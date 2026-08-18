using System;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace LearnAsync;

#pragma warning disable CA1815

[PublicAPI]
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

    public bool IsCompleted
    {
        [Pure]
        get => this._core.IsCompleted;
    }

    /// <inheritdoc/>
    void INotifyCompletion.OnCompleted(
        Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        this._core.OnCompleted(continuation);
    }

    /// <inheritdoc/>
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
