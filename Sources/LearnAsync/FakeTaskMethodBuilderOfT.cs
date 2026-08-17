using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskMethodBuilder<T>
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

    public void SetResult(T result)
    {
        this.Task.SetResult(result);
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this.Task.SetException(exception);
    }
}
