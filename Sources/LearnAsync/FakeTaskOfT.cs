using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder<>))]
public struct FakeTask<T>
{
    public FakeTaskAwaiter<T> GetAwaiter()
    {
        return default;
    }
}
