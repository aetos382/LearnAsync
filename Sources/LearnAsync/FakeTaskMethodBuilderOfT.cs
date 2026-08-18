using System;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace LearnAsync;

#pragma warning disable CA1815

// メンバーはコンパイラが生成したステート マシンから呼ばれるので、
// 未使用と見なされないように型ごと PublicAPI としてマークする。
[PublicAPI]
public readonly struct FakeTaskMethodBuilder<T>
{
    private readonly FakeTaskMethodBuilderCore<T> _core;

#pragma warning disable CA1000
    [Pure]
    public static FakeTaskMethodBuilder<T> Create()
    {
        return new();
    }
#pragma warning restore

    public FakeTaskMethodBuilder()
    {
        this._core = new();
    }

    public FakeTask<T> Task
    {
        [Pure]
        get => new(this._core.State);
    }

    public void Start<TStateMachine>(
        ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        FakeTaskMethodBuilderCore.Start(ref stateMachine);
    }

    public void SetStateMachine(
        IAsyncStateMachine stateMachine)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        this._core.SetStateMachine(stateMachine);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        this._core.AwaitOnCompleted(ref awaiter, ref stateMachine);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        this._core.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }

    public void SetResult(
        T result)
    {
        this._core.SetResult(result);
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this._core.SetException(exception);
    }
}
