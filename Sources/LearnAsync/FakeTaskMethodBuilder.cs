using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskMethodBuilder
{
    private readonly FakeTaskMethodBuilderCore<VoidTaskResult> _core;

    public static FakeTaskMethodBuilder Create()
    {
        return new();
    }

    public FakeTaskMethodBuilder()
    {
        this._core = new();
    }

    public FakeTask Task => new(this._core.State);

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
