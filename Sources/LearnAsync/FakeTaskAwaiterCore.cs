using System;
using System.Threading;

namespace LearnAsync;

internal readonly struct FakeTaskAwaiterCore<T>
{
    private readonly FakeTaskState<T> _state;

    internal FakeTaskAwaiterCore(
        FakeTaskState<T> state)
    {
        this._state = state;
    }

    public bool IsCompleted => this._state.IsCompleted;

    public void OnCompleted(
        Action continuation)
    {
        var executionContext = ExecutionContext.Capture();
        if (executionContext is null)
        {
            this._state.AddContinuationAction(continuation);
            return;
        }

        this._state.AddContinuationAction(
            () => ExecutionContext.Run(executionContext, static state => ((Action)state!)(), continuation));
    }

    public void UnsafeOnCompleted(
        Action continuation)
    {
        this._state.AddContinuationAction(continuation);
    }

    public T GetResult()
    {
        return this._state.GetResult();
    }
}
