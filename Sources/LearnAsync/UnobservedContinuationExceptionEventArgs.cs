using System;

using JetBrains.Annotations;

namespace LearnAsync;

[PublicAPI]
public sealed class UnobservedContinuationExceptionEventArgs :
    EventArgs
{
    internal UnobservedContinuationExceptionEventArgs(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this.Exception = exception;
    }

    public Exception Exception { [Pure] get; }
}
