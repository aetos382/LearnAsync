using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskAwaiter :
    ICriticalNotifyCompletion
{
    private readonly FakeTaskState _state;

    internal FakeTaskAwaiter(
        FakeTaskState state)
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
        if (executionContext is null)
        {
            this._state.AddContinuationAction(continuation);
            return;
        }

        this._state.AddContinuationAction(
            () => ExecutionContext.Run(executionContext, static state => ((Action)state!)(), continuation));
    }

    void ICriticalNotifyCompletion.UnsafeOnCompleted(
        Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        this._state.AddContinuationAction(continuation);
    }

    public void GetResult()
    {
        this._state.GetResult();
    }
}
