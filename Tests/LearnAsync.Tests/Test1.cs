using System;
using System.Threading;
using System.Threading.Tasks;

namespace LearnAsync.Tests;

[TestClass]
public sealed class Test1
{
    private readonly TestContext _testContext;

    public Test1(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    [TestMethod]
    public async Task TestMethod1()
    {
        await this.DoAsync(this._testContext.CancellationToken);
    }

    [TestMethod]
    public async Task TestMethod2()
    {
        var result = await this.GetIntAsync(this._testContext.CancellationToken);

        Assert.AreEqual(42, result);
    }

    private async FakeTask DoAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.Zero).ConfigureAwait(false);
    }

    private async FakeTask<int> GetIntAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.Zero).ConfigureAwait(false);

        return 42;
    }
}
