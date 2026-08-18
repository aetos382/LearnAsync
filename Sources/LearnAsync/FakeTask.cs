using System;
using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder))]
public readonly struct FakeTask
{
    private readonly FakeTaskState<VoidTaskResult> _state;

    public FakeTask()
    {
        this._state = new();
    }

    internal FakeTask(
        FakeTaskState<VoidTaskResult> state)
    {
        this._state = state;
    }

    public FakeTaskAwaiter GetAwaiter()
    {
        return new(this._state);
    }
}
