using System;
using System.Diagnostics.Tracing;

namespace LearnAsync.Tests;

internal sealed partial class FakeTaskEventListener :
    EventListener
{
    /// <inheritdoc />
    protected override void OnEventSourceCreated(
        EventSource eventSource)
    {
        ArgumentNullException.ThrowIfNull(eventSource);

        if (eventSource.Name != "LearnAsync.FakeTask")
        {
            return;
        }

        this.EnableEvents(eventSource, EventLevel.LogAlways);
    }

    /// <inheritdoc />
    protected override void OnEventWritten(
        EventWrittenEventArgs eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
    }
}
