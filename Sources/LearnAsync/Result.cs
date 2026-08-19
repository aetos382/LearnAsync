using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

using JetBrains.Annotations;

namespace LearnAsync;

[Union]
internal readonly struct Result<T> :
    IUnion
{
    public Result(
        T value)
    {
        this._value = value;
        this._kind = Kind.Succeeded;
    }

    public Result(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        this._exception = exception;
        this._kind = Kind.Failed;
    }

    private enum Kind
    {
        None,
        Succeeded,
        Failed
    }

    private readonly Kind _kind;

    private readonly T? _value;

    private readonly Exception? _exception;

    public bool TryGetValue(
        out T? value)
    {
        if (this._kind is not Kind.Succeeded)
        {
            value = default;
            return false;
        }

        value = this._value;
        return true;
    }

    public bool TryGetValue(
        [MaybeNullWhen(false)] out Exception exception)
    {
        if (this._kind is not Kind.Failed)
        {
            exception = null;
            return false;
        }

        exception = this._exception!;
        return true;
    }

    public object? Value
    {
        [Pure]
        get
        {
            return this._kind switch
            {
                Kind.Succeeded => this._value,
                Kind.Failed => this._exception,
                _ => throw new InvalidOperationException()
            };
        }
    }

    public T GetValue()
    {
        if (this.TryGetValue(out Exception? exception))
        {
            ExceptionDispatchInfo.Throw(exception);
        }

        return this._value!;
    }
}
