using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder<>))]
public readonly struct FakeTask<T>
{
    private readonly FakeTaskState<T> _state;

    internal FakeTask(
        FakeTaskState<T> state)
    {
        this._state = state;
    }

    public FakeTaskAwaiter<T> GetAwaiter()
    {
        return new(this._state);
    }
}
