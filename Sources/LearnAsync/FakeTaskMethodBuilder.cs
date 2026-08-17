using System;
using System.Runtime.CompilerServices;
using System.Threading;

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
        var previousExecutionContext = ExecutionContext.Capture();
        var previousSynchronizationContext = SynchronizationContext.Current;

        try
        {
            stateMachine.MoveNext();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);

            if (previousExecutionContext is not null)
            {
                ExecutionContext.Restore(previousExecutionContext);
            }
        }
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
        awaiter.OnCompleted(CreateContinuation(ref stateMachine));
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.UnsafeOnCompleted(CreateContinuation(ref stateMachine));
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

    private static Action CreateContinuation<TStateMachine>(
        ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        IAsyncStateMachine boxedStateMachine = stateMachine;

        var context = ExecutionContext.Capture();
        if (context is null)
        {
            return boxedStateMachine.MoveNext;
        }

        return () => ExecutionContext.Run(
            context,
            static state => ((IAsyncStateMachine)state!).MoveNext(),
            boxedStateMachine);
    }
}
