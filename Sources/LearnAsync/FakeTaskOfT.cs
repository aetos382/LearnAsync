using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder<>))]
public readonly struct FakeTask<T>
{
    private readonly FakeTaskState<T> _state;

    public FakeTask()
    {
        this._state = new();
    }

    public FakeTaskAwaiter<T> GetAwaiter()
    {
        return new(this._state);
    }

    internal void SetResult(T result)
    {
        this._state.SetResult(result);
    }

    internal void SetException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this._state.SetException(exception);
    }
}
