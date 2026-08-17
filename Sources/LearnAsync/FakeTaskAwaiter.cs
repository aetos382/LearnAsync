using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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
    }

    void ICriticalNotifyCompletion.UnsafeOnCompleted(
        Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);
    }

    public void GetResult()
    {
        this._state.Wait();
    }
}
