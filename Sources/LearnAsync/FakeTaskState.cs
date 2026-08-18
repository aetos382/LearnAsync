using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

using JetBrains.Annotations;

namespace LearnAsync;

internal sealed class FakeTaskState<T>
{
    [Pure]
    internal static FakeTaskState<T> FromResult(
        T result)
    {
        var state = new FakeTaskState<T>();

        state.SetResult(result);

        return state;
    }

    [Pure]
    internal static FakeTaskState<T> FromException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var state = new FakeTaskState<T>();

        state.SetException(exception);

        return state;
    }

    private readonly Lock _lock = new();

    private bool _isCompleted;

    private T? _result;

    private ExceptionDispatchInfo? _edi;

    private IAsyncStateMachine? _stateMachine;

    private readonly Queue<Action> _continuations = new();

    // 最初の中断でヒープに移されたステート マシンの箱。ビルダーは readonly struct で
    // コピーされてしまうので、コピーをまたいで共有されるここに置く。
    internal IAsyncStateMachine? StateMachine
    {
        [Pure]
        get
        {
            return Volatile.Read(ref this._stateMachine);
        }
    }

    internal void SetStateMachine(
        IAsyncStateMachine stateMachine)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        var original = Interlocked.CompareExchange(ref this._stateMachine, stateMachine, null);

        if (original is not null && !ReferenceEquals(original, stateMachine))
        {
            throw new InvalidOperationException("state machine already set.");
        }
    }

    public bool IsCompleted
    {
        [Pure]
        get
        {
            return Volatile.Read(ref this._isCompleted);
        }
    }

    internal void AddContinuationAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (this._lock)
        {
            if (!this._isCompleted)
            {
                this._continuations.Enqueue(action);
                return;
            }
        }

        action();
    }

    public void SetResult(T result)
    {
        lock (this._lock)
        {
            if (this._isCompleted)
            {
                throw new InvalidOperationException("task already completed.");
            }

            this._result = result;

            Volatile.Write(ref this._isCompleted, true);
        }

        this.RunContinuations();
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (this._lock)
        {
            if (this._isCompleted)
            {
                throw new InvalidOperationException("task already completed.");
            }

            this._edi = ExceptionDispatchInfo.Capture(exception);

            Volatile.Write(ref this._isCompleted, true);
        }

        this.RunContinuations();
    }

    // 完了までブロックするので Pure ではないが、結果を捨てるのは無意味。
    [MustUseReturnValue]
    public T GetResult()
    {
        // ここで false を観測しても AddContinuationAction が完了を再チェックするので、
        // 待機に入ったまま取り残されることはない。
        if (!Volatile.Read(ref this._isCompleted))
        {
            using var e = new ManualResetEventSlim(false);

            this.AddContinuationAction(e.Set);

            e.Wait();
        }

        this._edi?.Throw();

        return this._result!;
    }

    private void RunContinuations()
    {
        // 完了フラグを立てた後は AddContinuationAction が Enqueue しないので、
        // ここで一度キューを空にすれば継続を取りこぼすことはない。
        // 継続の実行はロックの外で行う（任意のユーザー コードを抱えたまま
        // 他のスレッドをブロックしないため）。
        Action[] actions;

        lock (this._lock)
        {
            actions = this._continuations.ToArray();
            this._continuations.Clear();
        }

        foreach (var action in actions)
        {
            try
            {
                action();
            }
#pragma warning disable CA1031
            catch (Exception exception)
            {
                FakeTaskEvents.OnUnobservedContinuationException(exception);
            }
#pragma warning restore
        }
    }
}
