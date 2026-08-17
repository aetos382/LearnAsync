using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskAwaiter<T> :
    ICriticalNotifyCompletion
{
    private readonly FakeTaskState<T> _state;

    internal FakeTaskAwaiter(
        FakeTaskState<T> state)
    {
        ArgumentNullException.ThrowIfNull(state);

        this._state = state;
    }

    public bool IsCompleted => this._state.IsCompleted;

    void INotifyCompletion.OnCompleted(
        Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        var executionContext = ExecutionContext.Capture();
        if (executionContext is not null)
        {
            continuation = () => ExecutionContext.Run(executionContext, static state => ((Action)state!)(), continuation);
        }

        this._state.AddContinuationAction(continuation);
    }

    void ICriticalNotifyCompletion.UnsafeOnCompleted(
        Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        this._state.AddContinuationAction(continuation);
    }

    public T GetResult()
    {
        return this._state.GetResult();
    }
}
