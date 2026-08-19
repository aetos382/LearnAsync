using System;
using System.Runtime.CompilerServices;
using System.Threading;

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

    public T GetResult()
    {
        return this.GetResult(CancellationToken.None);
    }

    // テスト用に GetResult をキャンセル可能にしたもの
    internal T GetResult(
        CancellationToken cancellationToken)
    {
        return this._core.GetResult(cancellationToken);
    }
}
