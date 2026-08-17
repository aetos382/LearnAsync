using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace LearnAsync;

internal sealed class FakeTaskState<T>
{
    private readonly Lock _lock = new();

    private bool _isCompleted;

    private T? _result;

    private ExceptionDispatchInfo? _edi;

    private readonly Queue<Action> _continuations = new();

    public bool IsCompleted
    {
        get
        {
            lock (this._lock)
            {
                return this._isCompleted;
            }
        }
    }

    internal void AddContinuationAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (this._lock)
        {
            if (!this._isCompleted)
            {
                this._continuations.Enqueue(action);
                return;
            }
        }

        action();
    }

    public void SetResult(T result)
    {
        lock (this._lock)
        {
            if (this._isCompleted)
            {
                throw new InvalidOperationException("task already completed.");
            }

            this._result = result;
            this._isCompleted = true;
        }

        this.RunContinuations();
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (this._lock)
        {
            if (this._isCompleted)
            {
                throw new InvalidOperationException("task already completed.");
            }

            this._edi = ExceptionDispatchInfo.Capture(exception);
            this._isCompleted = true;
        }

        this.RunContinuations();
    }

    public T GetResult()
    {
        lock (this._lock)
        {
            this._edi?.Throw();

            if (this._isCompleted)
            {
                return this._result!;
            }
        }

        using var e = new ManualResetEventSlim(false);

        this.AddContinuationAction(e.Set);

        e.Wait();

        lock (this._lock)
        {
            return this._result!;
        }
    }

    private void RunContinuations()
    {
        while (true)
        {
            Action? action;

            lock (this._lock)
            {
                if (!this._continuations.TryDequeue(out action))
                {
                    return;
                }
            }

            action();
        }
    }
}
