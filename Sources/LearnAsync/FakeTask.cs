using System.Runtime.CompilerServices;

namespace LearnAsync;

#pragma warning disable CA1815

[AsyncMethodBuilder(typeof(FakeTaskMethodBuilder))]
public readonly struct FakeTask
{
    public FakeTaskAwaiter GetAwaiter()
    {
        return new();
    }
}
