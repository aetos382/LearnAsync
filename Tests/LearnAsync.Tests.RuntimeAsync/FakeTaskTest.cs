using System;
using System.Threading;
using System.Threading.Tasks;

namespace LearnAsync.Tests.RuntimeAsync;

[TestClass]
public sealed class FakeTaskTest
{
    private readonly TestContext _testContext;

    public FakeTaskTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }
}
