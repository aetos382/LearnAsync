using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder))]
public readonly struct FakeTask
{
    // 完了済みの状態は二度と変化せず、継続を登録してもその場で実行されるだけなので、共有しても安全。
    private static readonly FakeTaskState<VoidTaskResult> CompletedState = FakeTaskState<VoidTaskResult>.FromResult(default);

    private readonly FakeTaskState<VoidTaskResult> _state;

    internal FakeTask(
        FakeTaskState<VoidTaskResult> state)
    {
        this._state = state;
    }

    public static FakeTask CompletedTask => new(CompletedState);

    public static FakeTask<T> FromResult<T>(
        T result)
    {
        return new(FakeTaskState<T>.FromResult(result));
    }

    public static FakeTask FromException(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new(FakeTaskState<VoidTaskResult>.FromException(exception));
    }

    public static FakeTask<T> FromException<T>(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new(FakeTaskState<T>.FromException(exception));
    }

    public FakeTaskAwaiter GetAwaiter()
    {
        return new(this._state);
    }
}
