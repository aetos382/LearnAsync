using System.Diagnostics.Tracing;

namespace LearnAsync;

[EventSource(Name = "LearnAsync.FakeTask")]
internal sealed class FakeTaskEventSource :
    EventSource
{
    public static readonly FakeTaskEventSource Log = new();

    public static class EventIds
    {
        public const int Foo = 1;
    }

    public static class Tasks
    {
        public const EventTask Foo = (EventTask)1;
    }

    public static class Keywords
    {
        public const EventKeywords Foo = (EventKeywords)100;
    }
}
