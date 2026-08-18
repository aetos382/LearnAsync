using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskAwaiter :
    ICriticalNotifyCompletion
{
    private readonly FakeTaskAwaiterCore<VoidTaskResult> _core;

    internal FakeTaskAwaiter(
        FakeTaskState<VoidTaskResult> state)
    {
        ArgumentNullException.ThrowIfNull(state);

        this._core = new(state);
    }

    public bool IsCompleted => this._core.IsCompleted;

    void INotifyCompletion.OnCompleted(
        Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        this._core.OnCompleted(continuation);
    }

    void ICriticalNotifyCompletion.UnsafeOnCompleted(
        Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        this._core.UnsafeOnCompleted(continuation);
    }

    public void GetResult()
    {
        _ = this._core.GetResult();
    }
}
