using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LearnAsync;

#pragma warning disable CA1815

public struct FakeTaskAwaiter :
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

    public void GetResult()
    {
    }
}
