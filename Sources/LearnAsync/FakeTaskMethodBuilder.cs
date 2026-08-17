using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskMethodBuilder
{
    private readonly FakeTaskState _state = new();

    public static FakeTaskMethodBuilder Create()
    {
        return new();
    }

    public FakeTaskMethodBuilder()
    {
    }

    public FakeTask Task => new(this._state);

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
#pragma warning restore

    public void SetStateMachine(
        IAsyncStateMachine stateMachine)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        this._state.SetStateMachine(stateMachine);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.OnCompleted(this.CreateContinuation(ref stateMachine, false));
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.UnsafeOnCompleted(this.CreateContinuation(ref stateMachine, true));
    }

    public void SetResult()
    {
        this._state.SetResult();
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this._state.SetException(exception);
    }

    private Action CreateContinuation<TStateMachine>(
        ref TStateMachine stateMachine,
        bool flowExecutionContext)
        where TStateMachine : IAsyncStateMachine
    {
        if (this._state.StateMachine is not { } boxedStateMachine)
        {
            boxedStateMachine = stateMachine;
            boxedStateMachine.SetStateMachine(boxedStateMachine);
        }

        var context = flowExecutionContext ? ExecutionContext.Capture() : null;
        if (context is null)
        {
            return () => boxedStateMachine.MoveNext();
        }

        return () => ExecutionContext.Run(context, static state => ((IAsyncStateMachine)state!).MoveNext(), boxedStateMachine);
    }
}
