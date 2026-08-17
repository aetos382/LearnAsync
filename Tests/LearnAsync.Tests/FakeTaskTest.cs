using System;
using System.Runtime.CompilerServices;
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

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 同期的に完了するFakeTaskをawaitする()
    {
        var resultHolder = new ResultHolder();

        var fakeTask = DoAsync(
            (Input: 42, Output: resultHolder),
            static state =>
            {
                state.Output.SetResult(state.Input);
                return Task.CompletedTask;
            });

        await fakeTask;

        Assert.AreEqual(42, resultHolder.Result);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 同期的に完了するFakeTaskを複数回awaitする()
    {
        var fakeTask = DoAsync(static () => Task.CompletedTask);

        await fakeTask;
        await fakeTask;
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public void 完了済みのFakeTaskに登録した継続はその場で実行される()
    {
        var fakeTask = DoAsync(static () => Task.CompletedTask);

        var awaiter = fakeTask.GetAwaiter();

        Assert.IsTrue(awaiter.IsCompleted);

        var ran = false;

        ((ICriticalNotifyCompletion)awaiter).UnsafeOnCompleted(() => ran = true);

        Assert.IsTrue(ran);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 呼び出し元のAsyncLocalの値がasyncFakeTaskメソッドの中で見える()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var resultHolder = new ResultHolder();

        await ReadAsync();

        Assert.AreEqual(42, resultHolder.Result);

        async FakeTask ReadAsync()
        {
            resultHolder.SetResult(asyncLocal.Value);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task AsyncLocalへの書き込みはasyncFakeTaskメソッドの外に漏れない()
    {
        var asyncLocal = new AsyncLocal<int>();

        var fakeTask = SetAsync();

        Assert.AreEqual(0, asyncLocal.Value);

        await fakeTask;

        async FakeTask SetAsync()
        {
            asyncLocal.Value = 42;

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task AsyncLocalの値はawaitをまたいで保たれる()
    {
        var asyncLocal = new AsyncLocal<int>();

        var tcs = new TaskCompletionSource();

        var resultHolder = new ResultHolder();

        var fakeTask = SetAwaitReadAsync();

        Assert.AreEqual(0, asyncLocal.Value);

        tcs.SetResult();

        await fakeTask;

        Assert.AreEqual(42, resultHolder.Result);

        async FakeTask SetAwaitReadAsync()
        {
            asyncLocal.Value = 42;

            await tcs.Task.ConfigureAwait(false);

            resultHolder.SetResult(asyncLocal.Value);
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task OnCompletedで登録した継続はキャプチャしたExecutionContextの上で実行される()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var asyncLocal = new AsyncLocal<int>();

        var tcs = new TaskCompletionSource();

        var resultHolder = new ResultHolder();

        var fakeTask = DoAsync(
            (Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);
            });

        var awaiter = fakeTask.GetAwaiter();

        Assert.IsFalse(awaiter.IsCompleted);

        asyncLocal.Value = 42;

        ((INotifyCompletion)awaiter).OnCompleted(() => resultHolder.SetResult(asyncLocal.Value));

        asyncLocal.Value = 0;

        tcs.SetResult();

        await fakeTask;

        Assert.AreEqual(42, resultHolder.Result);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 例外で同期的に完了するタスクをawaitする()
    {
#pragma warning disable CA2201
        var fakeTask = DoAsync(static () => throw new Exception("Oops!"));
#pragma warning restore

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask, "Oops!").ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 非同期的に完了するFakeTaskをawaitする()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var resultHolder = new ResultHolder();

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(
            (Input: 42, Output: resultHolder, Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

                state.Output.SetResult(state.Input);
            });

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, resultHolder.Result);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 継続が例外を投げても後続の継続は実行されUnobservedContinuationExceptionで観測できる()
    {
        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(tcs.Task, static state => state);

        var awaiter = (ICriticalNotifyCompletion)fakeTask.GetAwaiter();

        Assert.IsFalse(((FakeTaskAwaiter)awaiter).IsCompleted);

#pragma warning disable CA2201
        var thrown = new Exception("Oops!");
#pragma warning restore

        var observed = new TaskCompletionSource<Exception>();

        var firstRan = false;
        var lastRan = false;

        void Handler(object? sender, UnobservedContinuationExceptionEventArgs e)
        {
            if (ReferenceEquals(e.Exception, thrown))
            {
                observed.SetResult(e.Exception);
            }
        }

        FakeTaskEvents.UnobservedContinuationException += Handler;

        try
        {
            awaiter.UnsafeOnCompleted(() => firstRan = true);
            awaiter.UnsafeOnCompleted(() => throw thrown);
            awaiter.UnsafeOnCompleted(() => lastRan = true);

            tcs.SetResult();

            await fakeTask;

            Assert.IsTrue(firstRan);
            Assert.IsTrue(lastRan);
            Assert.AreSame(thrown, await observed.Task.ConfigureAwait(false));
        }
        finally
        {
            FakeTaskEvents.UnobservedContinuationException -= Handler;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 待機中のGetResultは後から設定された例外を投げる()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(
            (Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

#pragma warning disable CA2201
                throw new Exception("Oops!");
#pragma warning restore
            });

        var awaiter = fakeTask.GetAwaiter();

        Assert.IsFalse(awaiter.IsCompleted);

        using var started = new ManualResetEventSlim(false);

        var getResult = Task.Run(
            () =>
            {
                started.Set();
                awaiter.GetResult();
            },
            testCancellationToken);

        started.Wait(testCancellationToken);

        await Task.Delay(100, testCancellationToken).ConfigureAwait(false);

        Assert.IsFalse(getResult.IsCompleted, "GetResult は完了までブロックするはず。");

        tcs.SetResult();

        await Assert.ThrowsAsync<Exception>(async () => await getResult.ConfigureAwait(false), "Oops!").ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 同期的に完了するFakeTaskを自前のステートマシンで回す()
    {
        var resultHolder = new ResultHolder();

        await RunStateMachine(42, resultHolder);

        Assert.AreEqual(42, resultHolder.Result);

        static FakeTask RunStateMachine(int input, ResultHolder output)
        {
            var stateMachine = new DoAsyncStateMachine<(int Input, ResultHolder Output)>
            {
                Builder = FakeTaskMethodBuilder.Create(),
                Parameters = new()
                {
                    State = (input, output),
                    Action = static state =>
                    {
                        state.Output.SetResult(state.Input);
                        return Task.CompletedTask;
                    }
                }
            };

            ref var builder = ref stateMachine.Builder;

            builder.Start(ref stateMachine);

            return builder.Task;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 例外で同期的に完了するFakeTaskを自前のステートマシンで回す()
    {
        var fakeTask = RunStateMachine();

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask, "Oops!").ConfigureAwait(false);

        static FakeTask RunStateMachine()
        {
            var stateMachine = new DoAsyncStateMachine<object?>
            {
                Builder = FakeTaskMethodBuilder.Create(),
                Parameters = new()
                {
                    State = null,
                    Action = static _ =>
                    {
#pragma warning disable CA2201
                        throw new Exception("Oops!");
#pragma warning restore
                    }
                }
            };

            ref var builder = ref stateMachine.Builder;

            builder.Start(ref stateMachine);

            return builder.Task;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 非同期的に完了するFakeTaskを自前のステートマシンで回す()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var resultHolder = new ResultHolder();

        var tcs = new TaskCompletionSource();

        var fakeTask = RunStateMachine(42, resultHolder, tcs.Task, testCancellationToken);

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, resultHolder.Result);

        static FakeTask RunStateMachine(int input, ResultHolder output, Task signal, CancellationToken cancellationToken)
        {
            var stateMachine = new DoAsyncStateMachine<(int Input, ResultHolder Output, Task Signal, CancellationToken CancellationToken)>
            {
                Builder = FakeTaskMethodBuilder.Create(),
                Parameters = new()
                {
                    State = (input, output, signal, cancellationToken),
                    Action = static async state =>
                    {
                        await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);
                        state.Output.SetResult(state.Input);
                    }
                }
            };

            ref var builder = ref stateMachine.Builder;

            builder.Start(ref stateMachine);

            return builder.Task;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 中断したステートマシンの状態は本体に書き戻される()
    {
        var tcs = new TaskCompletionSource();

        var stateMachine = new DoAsyncStateMachine<Task>
        {
            Builder = FakeTaskMethodBuilder.Create(),
            Parameters = new()
            {
                State = tcs.Task,
                Action = static state => state
            }
        };

        ref var builder = ref stateMachine.Builder;

        builder.Start(ref stateMachine);

        Assert.AreEqual(DoAsyncStateMachine<Task>.State.Stage1Completed, stateMachine.CurrentState);

        tcs.SetResult();

        await builder.Task;
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 例外で非同期的に完了するFakeTaskを自前のステートマシンで回す()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = RunStateMachine(tcs.Task, testCancellationToken);

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask, "Oops!").ConfigureAwait(false);
        await tcs.Task.ConfigureAwait(false);

        static FakeTask RunStateMachine(Task signal, CancellationToken cancellationToken)
        {
            var stateMachine = new DoAsyncStateMachine<(Task Signal, CancellationToken CancellationToken)>
            {
                Builder = FakeTaskMethodBuilder.Create(),
                Parameters = new()
                {
                    State = (signal, cancellationToken),
                    Action = static async state =>
                    {
                        await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

#pragma warning disable CA2201
                        throw new Exception("Oops!");
#pragma warning restore
                    }
                }
            };

            ref var builder = ref stateMachine.Builder;

            builder.Start(ref stateMachine);

            return builder.Task;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task AwaitOnCompletedで中断してもAsyncLocalの値は保たれる()
    {
        var asyncLocal = new AsyncLocal<int>();

        var tcs = new TaskCompletionSource();

        var resultHolder = new ResultHolder();

        asyncLocal.Value = 42;

        var stateMachine = new OnCompletedStateMachine<FlowingAwaiter>
        {
            Builder = FakeTaskMethodBuilder.Create(),
            Awaiter = new(tcs.Task),
            Read = () => asyncLocal.Value,
            Output = resultHolder
        };

        stateMachine.Builder.Start(ref stateMachine);

        var fakeTask = stateMachine.Builder.Task;

        asyncLocal.Value = 0;

        tcs.SetResult();

        await fakeTask;

        Assert.AreEqual(42, resultHolder.Result);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task AwaitOnCompletedに渡す継続はビルダーがExecutionContextで包まない()
    {
        var asyncLocal = new AsyncLocal<int>();

        var awaiter = new ManualAwaiter();

        var resultHolder = new ResultHolder();

        asyncLocal.Value = 42;

        var stateMachine = new OnCompletedStateMachine<ManualAwaiter>
        {
            Builder = FakeTaskMethodBuilder.Create(),
            Awaiter = awaiter,
            Read = () => asyncLocal.Value,
            Output = resultHolder
        };

        stateMachine.Builder.Start(ref stateMachine);

        var fakeTask = stateMachine.Builder.Task;

        asyncLocal.Value = 0;

        awaiter.Complete();

        await fakeTask;

        Assert.AreEqual(0, resultHolder.Result);
    }

    private struct OnCompletedStateMachine<TAwaiter> :
        IAsyncStateMachine
        where TAwaiter : INotifyCompletion
    {
        public FakeTaskMethodBuilder Builder;

        public TAwaiter Awaiter;

        public ResultHolder Output;

        public Func<int> Read;

        private int _stage;

        void IAsyncStateMachine.MoveNext()
        {
            try
            {
                switch (this._stage)
                {
                    case 0:
                        this._stage = 1;
                        this.Builder.AwaitOnCompleted(ref this.Awaiter, ref this);
                        break;

                    case 1:
                        this.Output.SetResult(this.Read());
                        this._stage = 2;
                        this.Builder.SetResult();
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
#pragma warning disable CA1031
            catch (Exception exception)
            {
                this.Builder.SetException(exception);
            }
#pragma warning restore
        }

        public readonly void SetStateMachine(
            IAsyncStateMachine stateMachine)
        {
            this.Builder.SetStateMachine(stateMachine);
        }
    }

    // INotifyCompletion だけを実装し、ExecutionContext を流すアウェイター。
    private sealed class FlowingAwaiter :
        INotifyCompletion
    {
        private readonly Task _task;

        public FlowingAwaiter(
            Task task)
        {
            this._task = task;
        }

        public void OnCompleted(
            Action continuation)
        {
            this._task.ConfigureAwait(false).GetAwaiter().OnCompleted(continuation);
        }
    }

    // ExecutionContext を流さないアウェイター。継続は Complete の呼び出し元で走る。
    private sealed class ManualAwaiter :
        INotifyCompletion
    {
        private Action? _continuation;

        public void OnCompleted(
            Action continuation)
        {
            this._continuation = continuation;
        }

        public void Complete()
        {
            var continuation = this._continuation;

            this._continuation = null;

            continuation?.Invoke();
        }
    }

    private struct DoAsyncStateMachine<TState> :
        IAsyncStateMachine
    {
        public DoAsyncStateMachine()
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

        public struct MethodParameters
        {
            public TState State;

            public Func<TState, Task> Action;
        }

        public MethodParameters Parameters;

        public State CurrentState { get; set; }

        private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _awaiter;

        void IAsyncStateMachine.MoveNext()
        {
            try
            {
                switch (this.CurrentState)
                {
                    case State.NotStarted:
                        Stage1(ref this);
                        break;

                    case State.Stage1Completed:
                        Stage2(ref this);
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
                SetException(ref this, exception);
            }
#pragma warning restore

            static void Stage1(ref DoAsyncStateMachine<TState> self)
            {
                var awaiter = self.Parameters.Action(self.Parameters.State).ConfigureAwait(false).GetAwaiter();

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

            static void Stage2(ref DoAsyncStateMachine<TState> self)
            {
                self._awaiter.GetResult();
                self.Builder.SetResult();
                self.CurrentState = State.Completed;
            }

            static void SetException(ref DoAsyncStateMachine<TState> self, Exception exception)
            {
                self.Builder.SetException(exception);
                self.CurrentState = State.Completed;
            }
        }

        public readonly void SetStateMachine(
            IAsyncStateMachine stateMachine)
        {
            this.Builder.SetStateMachine(stateMachine);
        }
    }

    private class ResultHolder
    {
        public int Result { get; private set; }

        public void SetResult(int value)
        {
            this.Result = value;
        }
    }

    private static async Task DelayedSignal(
        TaskCompletionSource tcs,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        tcs.TrySetResult();
    }

    private static async FakeTask DoAsync<TState>(TState state, Func<TState, Task> action)
    {
        await action(state).ConfigureAwait(false);
    }

    private static async FakeTask DoAsync(Func<Task> action)
    {
        await action().ConfigureAwait(false);
    }
}
