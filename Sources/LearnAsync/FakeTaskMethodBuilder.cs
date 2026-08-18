using System;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace LearnAsync;

#pragma warning disable CA1815

// メンバーはコンパイラが生成したステート マシンから呼ばれるので、
// 未使用と見なされないように型ごと PublicAPI としてマークする。
[PublicAPI]
public readonly struct FakeTaskMethodBuilder
{
    private readonly FakeTaskMethodBuilderCore<VoidTaskResult> _core;

    [Pure]
    public static FakeTaskMethodBuilder Create()
    {
        return new();
    }

    public FakeTaskMethodBuilder()
    {
        this._core = new();
    }

    public FakeTask Task
    {
        [Pure]
        get => new(this._core.State);
    }

#pragma warning disable CA1822
    public void Start<TStateMachine>(
        ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        FakeTaskMethodBuilderCore.Start(ref stateMachine);
    }
#pragma warning restore

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

    public void SetResult()
    {
        this._core.SetResult(default);
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this._core.SetException(exception);
    }
}
