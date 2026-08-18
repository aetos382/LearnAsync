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
        // Start は非同期メソッドの同期部分（最初の await まで）を呼び出し元のスレッド上でそのまま実行する。
        // その中で AsyncLocal<T> に書き込まれると、呼び出し元スレッドの ExecutionContext に記録され、非同期メソッドの外で見えてしまう。
        // それを避けるために元に戻している。
        // なお、ExecutionContext.SuppressFlow の影響下で実行された場合は元に戻らないが、それは自己責任。await を跨いで SuppressFlow するんじゃない。

        // ExecutionContext.Run を使わないのは、ステート マシンをここでボックス化しないため。
        // Run は状態を object で受けるのでボックス化が必要になり、MoveNext がボックスのコピーを進めてしまって
        // 呼び出し元のステート マシンに状態が書き戻されない。

        // SynchronizationContext については FakeTask にとって本質ではないため触らない。
        var previousExecutionContext = ExecutionContext.Capture();

        try
        {
            stateMachine.MoveNext();
        }
        finally
        {
            // フロー抑止下では Capture が null を返す。抑止しているなら復元しないのが呼び出し元の意図。
            if (previousExecutionContext is not null)
            {
                ExecutionContext.Restore(previousExecutionContext);
            }
        }
    }
}
