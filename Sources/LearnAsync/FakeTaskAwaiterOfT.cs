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
        get
        {
            return this._core.IsCompleted;
        }
    }

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

    // 完了までブロックするので Pure ではないが、結果を捨てるのは無意味。
    [MustUseReturnValue]
    public T GetResult()
    {
        return this._core.GetResult();
    }
}
