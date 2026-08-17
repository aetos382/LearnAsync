using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LearnAsync.Tests;

[TestClass]
public sealed class FakeTaskOfTTest
{
    private readonly TestContext _testContext;

    public FakeTaskOfTTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 戻り値のある非同期メソッドを呼ぶ()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var tcs = new TaskCompletionSource();

        var result = await this.GetIntAsync(asyncLocal, this._testContext.CancellationToken);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 戻り値のある非同期メソッド_自前ステートマシン版_を呼ぶ()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var result = await this.GetIntAsyncWithCustomStateMachine(asyncLocal, this._testContext.CancellationToken);
        Assert.AreEqual(42, result);
    }

    private async FakeTask<int> GetIntAsync(
    AsyncLocal<int> asyncLocal,
    CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);

        return asyncLocal.Value;
    }

    private FakeTask<int> GetIntAsyncWithCustomStateMachine(
        AsyncLocal<int> asyncLocal,
        CancellationToken cancellationToken = default)
    {
        var stateMachine = new GetIntAsyncCustomStateMachine();

        stateMachine.Builder = FakeTaskMethodBuilder<int>.Create();
        ref var builder = ref stateMachine.Builder;

        builder.Start(ref stateMachine);

        return builder.Task;
    }

    private struct GetIntAsyncCustomStateMachine :
    IAsyncStateMachine
    {
        public FakeTaskMethodBuilder<int> Builder;

        public void MoveNext()
        {
        }

        public void SetStateMachine(
            IAsyncStateMachine stateMachine)
        {
            ArgumentNullException.ThrowIfNull(stateMachine);
        }
    }
}
