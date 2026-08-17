using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskMethodBuilder
{
    public static FakeTaskMethodBuilder Create()
    {
        return new();
    }

    public FakeTaskMethodBuilder()
    {
    }

    public FakeTask Task { get; } = new();

#pragma warning disable CA1822
    public void Start<TStateMachine>(
        ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        stateMachine.MoveNext();
    }

    public void SetStateMachine(
        IAsyncStateMachine stateMachine)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        ArgumentNullException.ThrowIfNull(awaiter);
        ArgumentNullException.ThrowIfNull(stateMachine);

        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        ArgumentNullException.ThrowIfNull(awaiter);
        ArgumentNullException.ThrowIfNull(stateMachine);

        awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
    }
#pragma warning restore

    public void SetResult()
    {
        this.Task.SetResult();
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this.Task.SetException(exception);
    }
}
