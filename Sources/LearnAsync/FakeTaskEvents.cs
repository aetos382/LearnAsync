using System;

namespace LearnAsync;

public static class FakeTaskEvents
{
    public static event EventHandler<UnobservedContinuationExceptionEventArgs>? UnobservedContinuationException;

    internal static void OnUnobservedContinuationException(
        Exception exception)
    {
        var handler = UnobservedContinuationException;

        if (handler is null)
        {
            return;
        }

        try
        {
            handler(null, new(exception));
        }
#pragma warning disable CA1031
        catch
        {
            // 通知の失敗で完了処理そのものを壊さない。
        }
#pragma warning restore
    }
}
