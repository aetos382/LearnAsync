using System;
using System.Collections.Generic;
using System.Text;

namespace LearnAsync;

internal sealed class FakeTaskState<T>
{
    private T? _result;

    private Exception? _exception;

    public void SetResult(T result)
    {

    }

    public void SetException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
    }
}
