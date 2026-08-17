using System;

namespace LearnAsync;

public sealed class UnobservedContinuationExceptionEventArgs :
    EventArgs
{
    internal UnobservedContinuationExceptionEventArgs(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this.Exception = exception;
    }

    public Exception Exception { get; }
}
