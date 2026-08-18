using System;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace LearnAsync;

#pragma warning disable CA1815

[PublicAPI]
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

    [MustUseReturnValue]
    public T GetResult()
    {
        return this._core.GetResult();
    }
}
