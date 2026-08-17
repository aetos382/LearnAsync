using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder))]
public readonly struct FakeTask
{
    private readonly FakeTaskState _state;

    public FakeTask()
    {
        this._state = new();
    }

    public FakeTaskAwaiter GetAwaiter()
    {
        return new();
    }

    internal void SetResult()
    {
        this._state.SetResult();
    }

    internal void SetException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this._state.SetException(exception);
    }
}
