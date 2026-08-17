using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LearnAsync;

#pragma warning disable CA1815

public struct FakeTaskMethodBuilder<T>
{
#pragma warning disable CA1000
    public static FakeTaskMethodBuilder<T> Create()
    {
        return new();
    }
#pragma warning restore

    public FakeTaskMethodBuilder()
    {
    }

    public FakeTask<T> Task { get; } = new();

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
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        ArgumentNullException.ThrowIfNull(awaiter);
        ArgumentNullException.ThrowIfNull(stateMachine);
    }

    public void SetResult(T result)
    {
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
    }
}
