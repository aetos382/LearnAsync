using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace LearnAsync;

#pragma warning disable CA1815

[PublicAPI]
[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder<>))]
public readonly struct FakeTask<T>
{
    private readonly FakeTaskState<T> _state;

    internal FakeTask(
        FakeTaskState<T> state)
    {
        this._state = state;
    }

    public int Id => this._state.Id;

    [Pure]
    public FakeTaskAwaiter<T> GetAwaiter()
    {
        return new(this._state);
    }
}
