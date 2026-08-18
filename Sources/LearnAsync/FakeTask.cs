using System;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace LearnAsync;

#pragma warning disable CA1815

[PublicAPI]
[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder))]
public readonly struct FakeTask
{
    private static readonly FakeTaskState<VoidTaskResult> CompletedState = FakeTaskState<VoidTaskResult>.FromResult(default);

    private readonly FakeTaskState<VoidTaskResult> _state;

    internal FakeTask(
        FakeTaskState<VoidTaskResult> state)
    {
        this._state = state;
    }

    public static FakeTask CompletedTask
    {
        [Pure]
        get
        {
            return new(CompletedState);
        }
    }

    [Pure]
    public static FakeTask<T> FromResult<T>(
        T result)
    {
        return new(FakeTaskState<T>.FromResult(result));
    }

    [Pure]
    public static FakeTask FromException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new(FakeTaskState<VoidTaskResult>.FromException(exception));
    }

    [Pure]
    public static FakeTask<T> FromException<T>(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new(FakeTaskState<T>.FromException(exception));
    }

    [Pure]
    public FakeTaskAwaiter GetAwaiter()
    {
        return new(this._state);
    }
}
