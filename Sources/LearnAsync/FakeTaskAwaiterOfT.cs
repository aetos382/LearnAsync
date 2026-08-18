using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

public readonly struct FakeTaskAwaiter<T> :
    ICriticalNotifyCompletion
{
    private readonly FakeTaskAwaiterCore<T> _core;

    internal FakeTaskAwaiter(
        FakeTaskState<T> state)
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

    public T GetResult()
    {
        return this._core.GetResult();
    }
}
