using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace LearnAsync;

internal sealed class FakeTaskState
{
    private bool _gate;

    public bool IsCompleted { get; private set; }

    private ExceptionDispatchInfo? _edi;

    private readonly ConcurrentQueue<Action> _completedActions = new();

    internal void AddContinuationAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        this._completedActions.Enqueue(action);
    }

    public void SetResult()
    {
        if (Interlocked.Exchange(ref this._gate, true))
        {
            throw new InvalidOperationException("task already completed.");
        }

        this.IsCompleted = true;

        this.WakeWaiters();
    }

    public void SetException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (Interlocked.Exchange(ref this._gate, true))
        {
            throw new InvalidOperationException("task already completed.");
        }

        Debug.Assert(this._edi is null);

        this._edi = ExceptionDispatchInfo.Capture(exception);

        this.IsCompleted = true;

        this.WakeWaiters();
    }

    public void GetResult()
    {
        this._edi?.Throw();

        if (!this.IsCompleted)
        {
            using var e = new ManualResetEventSlim(false);

            this.AddContinuationAction(e.Set);

            e.Wait();
        }
    }

    private void WakeWaiters()
    {
        while (this._completedActions.TryDequeue(out var action))
        {
            action();
        }
    }
}
