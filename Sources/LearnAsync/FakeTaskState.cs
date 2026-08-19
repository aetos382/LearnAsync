using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    private volatile FakeTaskStatus _status;

    private Result<T>? _result;

    private IAsyncStateMachine? _stateMachine;

    private readonly Queue<Action> _continuations = [];

    internal IAsyncStateMachine? StateMachine
    {
        [Pure]
        get => Volatile.Read(ref this._stateMachine);
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
        get => this._status == FakeTaskStatus.Completed;
    }

    internal void AddContinuationAction(
        Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (this._lock)
        {
            if (this._status != FakeTaskStatus.Completed)
            {
                this._continuations.Enqueue(action);
                return;
            }
        }

        action();
    }

    public void SetResult(T result)
    {
        this.Complete(result);
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this.Complete(exception);
    }

    public T GetResult(
        CancellationToken cancellationToken)
    {
        if (!this.IsCompleted)
        {
            using var e = new ManualResetEventSlim(false);

            this.AddContinuationAction(e.Set);

            e.Wait(cancellationToken);
        }

        return this._result!.Value.GetValue();
    }

    private void Complete(
        Result<T> result)
    {
        this.ReserveCompletion();

        this._result = result;

        this.CompleteAndRunContinuations();
    }

    // 完了準備に入る。SetResult と SetException のどちらか一方しか成功させない。
    private void ReserveCompletion()
    {
        var original = Interlocked.CompareExchange(
            ref this._status, FakeTaskStatus.Completing, FakeTaskStatus.Pending);

        if (original != FakeTaskStatus.Pending)
        {
            throw new InvalidOperationException("task already completed.");
        }
    }

    // 完了状態にして継続を発火させる。
    // IsCompleted = true が外部から観測できるようになる。
    private void CompleteAndRunContinuations()
    {
        lock (this._lock)
        {
            // volatile なので _status を書くのに lock が要るわけではない。
            // AddContinuationAction の実行中に CompleteAndRunContinuations が割り込まないための lock。
            this._status = FakeTaskStatus.Completed;
        }

        while (this._continuations.TryDequeue(out var action))
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
