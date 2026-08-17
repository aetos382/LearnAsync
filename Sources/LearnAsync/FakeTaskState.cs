using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace LearnAsync;

internal sealed class FakeTaskState
{
    private bool _isCompleted;

    public bool IsCompleted => Volatile.Read(ref this._isCompleted);

    private Exception? _exception;

    public void SetResult()
    {
        if (Interlocked.Exchange(ref this._isCompleted, true))
        {
            throw new InvalidOperationException("task already completed.");
        }
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (Interlocked.Exchange(ref this._isCompleted, true))
        {
            throw new InvalidOperationException("task already completed.");
        }

        this._exception = exception;
    }

    public void GetResult()
    {
        if (this._exception is { } exception)
        {
            ExceptionDispatchInfo.Throw(exception);
        }
    }
}
