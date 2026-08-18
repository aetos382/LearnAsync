using System.Runtime.CompilerServices;
using System.Threading;

namespace LearnAsync;

internal static class FakeTaskMethodBuilderCore
{
    // <T> を参照しないので、最適化のために FakeTaskMethodBuilderCore<T> から独立させる
    public static void Start<TStateMachine>(
        ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        var previousExecutionContext = ExecutionContext.Capture();

        try
        {
            stateMachine.MoveNext();
        }
        finally
        {
            if (previousExecutionContext is not null)
            {
                ExecutionContext.Restore(previousExecutionContext);
            }
        }
    }
}
