using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

public struct FakeTaskAwaiter<T> :
    ICriticalNotifyCompletion
{
    private bool _isCompleted;

    public bool IsCompleted => this._isCompleted;

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

    public T GetResult()
    {
        return default;
    }
}
