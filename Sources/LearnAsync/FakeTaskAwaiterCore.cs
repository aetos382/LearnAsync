using System;
using System.Threading;

using JetBrains.Annotations;

namespace LearnAsync;

internal readonly struct FakeTaskAwaiterCore<T>
{
    private readonly FakeTaskState<T> _state;

    internal FakeTaskAwaiterCore(
        FakeTaskState<T> state)
    {
        this._state = state;
    }

    public bool IsCompleted
    {
        [Pure]
        get => this._state.IsCompleted;
    }

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

    public T GetResult(
        CancellationToken cancellationToken)
    {
        return this._state.GetResult(cancellationToken);
    }
}
