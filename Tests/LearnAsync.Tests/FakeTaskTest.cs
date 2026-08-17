using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LearnAsync.Tests;

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

    private class ResultHolder
    {
        public int Result;
    }

    [TestMethod]
    public async Task 戻り値のない非同期メソッドを呼ぶ()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var resultHolder = new ResultHolder();

        var tcs = new TaskCompletionSource();

        var task = this.DoAsync(asyncLocal, resultHolder, tcs.Task, this._testContext.CancellationToken);

        tcs.SetResult();

        await tcs.Task.ConfigureAwait(false);
        await task;

        Assert.AreEqual(42, resultHolder.Result);
    }

    [TestMethod]
    public async Task 戻り値のない非同期メソッド_自前ステートマシン版_を呼ぶ()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var resultHolder = new ResultHolder();

        var tcs = new TaskCompletionSource();

        var task = this.DoAsyncWithCustomStateMachine(asyncLocal, resultHolder, tcs.Task, this._testContext.CancellationToken);

        tcs.SetResult();

        await tcs.Task.ConfigureAwait(false);
        await task;

        Assert.AreEqual(42, resultHolder.Result);
    }

    [TestMethod]
    public async Task 戻り値のある非同期メソッドを呼ぶ()
    {
        var result = await this.GetIntAsync(this._testContext.CancellationToken);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public async Task 戻り値のある非同期メソッド_自前ステートマシン版_を呼ぶ()
    {
        var result = await this.GetIntAsyncWithCustomStateMachine(this._testContext.CancellationToken);
        Assert.AreEqual(42, result);
    }

#pragma warning disable CA1822
    private async Task WaitAsync(
        Task signal,
        CancellationToken cancellationToken)
    {
        await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore

    private async FakeTask DoAsync(
        AsyncLocal<int> asyncLocal,
        ResultHolder resultHolder,
        Task signal,
        CancellationToken cancellationToken = default)
    {
        await this.WaitAsync(signal, cancellationToken).ConfigureAwait(false);

        resultHolder.Result = asyncLocal.Value;
    }

    private FakeTask DoAsyncWithCustomStateMachine(
        AsyncLocal<int> asyncLocal,
        ResultHolder resultHolder,
        Task task,
        CancellationToken cancellationToken = default)
    {
        var stateMachine = new DoAsyncCustomStateMachine
        {
            Builder = FakeTaskMethodBuilder.Create(),
            CurrentState = DoAsyncCustomStateMachine.State.NotStarted,
            Parameters = new()
            {
                This = this,
                AsyncLocal = asyncLocal,
                ResultHolder = resultHolder,
                Task = task,
                CancellationToken = cancellationToken
            }
        };

        ref var builder = ref stateMachine.Builder;

        builder.Start(ref stateMachine);

        return builder.Task;
    }

    private async FakeTask<int> GetIntAsync(
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.Zero).ConfigureAwait(false);

        return 42;
    }

    private FakeTask<int> GetIntAsyncWithCustomStateMachine(
        CancellationToken cancellationToken = default)
    {
        var stateMachine = new GetIntAsyncCustomStateMachine();

        stateMachine.Builder = FakeTaskMethodBuilder<int>.Create();
        ref var builder = ref stateMachine.Builder;

        builder.Start(ref stateMachine);

        return builder.Task;
    }

    private struct DoAsyncCustomStateMachine :
        IAsyncStateMachine
    {
        public DoAsyncCustomStateMachine()
        {
            this.CurrentState = State.NotStarted;
        }

        public FakeTaskMethodBuilder Builder;

        public enum State
        {
            NotStarted,
            Stage1Completed,
            Completed
        }

        public struct NethodParameters
        {
            public FakeTaskTest This;
            public AsyncLocal<int> AsyncLocal;
            public ResultHolder ResultHolder;
            public Task Task;
            public CancellationToken CancellationToken;
        }

        public NethodParameters Parameters;

        public State CurrentState { get; set; }

        private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _awaiter;

        void IAsyncStateMachine.MoveNext()
        {
            var self = this;

            try
            {
                switch (this.CurrentState)
                {
                    case State.NotStarted:
                        Stage1();
                        break;

                    case State.Stage1Completed:
                        Stage2();
                        break;

                    case State.Completed:
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
#pragma warning disable CA1031
            catch (Exception exception)
            {
                SetException(exception);
            }
#pragma warning restore

            void Stage1()
            {
                var awaiter = self.Parameters.This.WaitAsync(self.Parameters.Task, self.Parameters.CancellationToken).ConfigureAwait(false).GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    awaiter.GetResult();
                    self.Builder.SetResult();
                    self.CurrentState = State.Completed;
                }
                else
                {
                    self.CurrentState = State.Stage1Completed;
                    self._awaiter = awaiter;
                    self.Builder.AwaitUnsafeOnCompleted(ref awaiter, ref self);
                }
            }

            void Stage2()
            {
                self._awaiter.GetResult();

                self.Parameters.ResultHolder.Result = self.Parameters.AsyncLocal.Value;

                self.Builder.SetResult();
                self.CurrentState = State.Completed;
            }

            void SetException(Exception exception)
            {
                self.Builder.SetException(exception);
                self.CurrentState = State.Completed;
            }
        }

        public void SetStateMachine(
            IAsyncStateMachine stateMachine)
        {
            ArgumentNullException.ThrowIfNull(stateMachine);
        }
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
