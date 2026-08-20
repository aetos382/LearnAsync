namespace LearnAsync.Tests;

[TestClass]
public sealed class EventTest
{
    [TestMethod]
    public void Foo()
    {
        using var listener = new FakeTaskEventListener();
    }
}
