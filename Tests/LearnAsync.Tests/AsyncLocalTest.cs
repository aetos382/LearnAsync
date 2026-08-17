using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LearnAsync.Tests;

[TestClass]
public class AsyncLocalTest
{
    private readonly TestContext _testContext;

    public AsyncLocalTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private class ThreadParameters
    {
        public AsyncLocalTest This;

        public ThreadLocal<int> ThreadLocal;

        public AsyncLocal<int> AsyncLocal;

        public TaskCompletionSource<int> TaskCompletionSource;
    }

    [TestMethod]
    public async Task AsyncLocalTest1()
    {
        var testContext = this._testContext;

        this.PrintThreadId("Before thread");

        var threadLocal = new ThreadLocal<int>
        {
            Value = 1
        };

        var asyncLocal = new AsyncLocal<int>
        {
            Value = 2
        };

        var tcs = new TaskCompletionSource<int>();

        var parameters = new ThreadParameters
        {
            This = this,
            ThreadLocal = threadLocal,
            AsyncLocal = asyncLocal,
            TaskCompletionSource = tcs
        };

        var thread = new Thread(static state =>
        {
            var parameters = (ThreadParameters)state!;

            parameters.This.PrintThreadId("In thread");

            var result = (parameters.ThreadLocal.Value + parameters.AsyncLocal.Value) * 2;

            parameters.TaskCompletionSource.SetResult(result);
        });

        thread.Start(parameters);

        var result = await tcs.Task.ConfigureAwait(false);

        this.PrintThreadId("After thread");

        Assert.AreEqual(4, result);
        Assert.AreEqual(0, threadLocal.Value);
        Assert.AreEqual(2, asyncLocal.Value);
    }

    [TestMethod]
    public async Task AsyncLocalTest2()
    {
        var testContext = this._testContext;

        this.PrintThreadId("Before thread");

        var threadLocal = new ThreadLocal<int>
        {
            Value = 1
        };

        var asyncLocal = new AsyncLocal<int>
        {
            Value = 2
        };

        var tcs = new TaskCompletionSource<int>();

        var parameters = new ThreadParameters
        {
            This = this,
            ThreadLocal = threadLocal,
            AsyncLocal = asyncLocal,
            TaskCompletionSource = tcs
        };

        ThreadPool.UnsafeQueueUserWorkItem(static state =>
        {
            var parameters = (ThreadParameters)state!;

            parameters.This.PrintThreadId("In thread");

            var result = (parameters.ThreadLocal.Value + parameters.AsyncLocal.Value) * 2;

            parameters.TaskCompletionSource.SetResult(result);
        },
        parameters);

        var result = await tcs.Task.ConfigureAwait(false);

        this.PrintThreadId("After thread");

        Assert.AreEqual(0, result);
        Assert.AreEqual(0, threadLocal.Value);
        Assert.AreEqual(2, asyncLocal.Value);
    }

    private void PrintThreadId(string? label = null)
    {
        var builder = new StringBuilder();

        builder.Append(CultureInfo.InvariantCulture, $"Thread ID: {Environment.CurrentManagedThreadId}.");

        if (label is { Length: > 0 })
        {
            builder.Append(CultureInfo.InvariantCulture, $" ({label})");
        }

        var message = builder.ToString();

        this._testContext.WriteLine(message);
    }
}
