using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LearnAsync;

internal readonly struct FakeTaskMethodBuilderCore<T>
{
    public FakeTaskState<T> State { get; }

    public FakeTaskMethodBuilderCore()
    {
        this.State = new();
    }

    public void SetStateMachine(
        IAsyncStateMachine stateMachine)
    {
        this.State.SetStateMachine(stateMachine);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        // awaiter.OnCompleted は ExecutionContext をキャプチャする責務を持つので、MethodBuilder ではキャプチャしない。
        // なお AsyncTaskMethodBuilder は AwaitOnCompleted でも ExecutionContext をキャプチャしている。
        awaiter.OnCompleted(this.CreateContinuation(ref stateMachine, false));
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        // awaiter.UnsafeOnCompleted は ExecutionContext をキャプチャしないので、MethodBuilder がキャプチャする。
        awaiter.UnsafeOnCompleted(this.CreateContinuation(ref stateMachine, true));
    }

    public void SetResult(
        T result)
    {
        this.State.SetResult(result);
    }

    public void SetException(
        Exception exception)
    {
        this.State.SetException(exception);
    }

    private Action CreateContinuation<TStateMachine>(
        ref TStateMachine stateMachine,
        bool flowExecutionContext)
        where TStateMachine : IAsyncStateMachine
    {
        if (this.State.StateMachine is not { } boxedStateMachine)
        {
            boxedStateMachine = stateMachine;
            boxedStateMachine.SetStateMachine(boxedStateMachine);
        }

        var context = flowExecutionContext ? ExecutionContext.Capture() : null;
        if (context is null)
        {
            return boxedStateMachine.MoveNext;
        }

        return () => ExecutionContext.Run(context, static state => ((IAsyncStateMachine)state!).MoveNext(), boxedStateMachine);
    }
}
