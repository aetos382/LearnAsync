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
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 同期的に完了するFakeTaskOfTをawaitする()
    {
        var fakeTask = DoAsync(
            42,
            static state =>
            {
                return Task.FromResult(state);
            });

        var result = await fakeTask;

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 同期的に完了するFakeTaskOfTを複数回awaitする()
    {
        var fakeTask = DoAsync(
            42,
            static state =>
            {
                return Task.FromResult(state);
            });

        var result1 = await fakeTask;
        var result2 = await fakeTask;

        Assert.AreEqual(42, result1);
        Assert.AreEqual(42, result2);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public void 完了済みのFakeTaskOfTに登録した継続はその場で実行される()
    {
        var fakeTask = DoAsync(static () => Task.FromResult(42));

        var awaiter = fakeTask.GetAwaiter();

        Assert.IsTrue(awaiter.IsCompleted);

        var ran = false;

        ((ICriticalNotifyCompletion)awaiter).UnsafeOnCompleted(() => ran = true);

        Assert.IsTrue(ran);
        Assert.AreEqual(42, awaiter.GetResult());
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 呼び出し元のAsyncLocalの値がasyncFakeTaskOfTメソッドの中で見える()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var result = await ReadAsync();

        Assert.AreEqual(42, result);

        async FakeTask<int> ReadAsync()
        {
            var value = asyncLocal.Value;

            await Task.CompletedTask.ConfigureAwait(false);

            return value;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task AsyncLocalへの書き込みはasyncFakeTaskOfTメソッドの外に漏れない()
    {
        var asyncLocal = new AsyncLocal<int>();

        var fakeTask = SetAsync();

        Assert.AreEqual(0, asyncLocal.Value);

        Assert.AreEqual(42, await fakeTask);

        async FakeTask<int> SetAsync()
        {
            asyncLocal.Value = 42;

            await Task.CompletedTask.ConfigureAwait(false);

            return asyncLocal.Value;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task AsyncLocalの値はawaitをまたいで保たれる()
    {
        var asyncLocal = new AsyncLocal<int>();

        var tcs = new TaskCompletionSource();

        var fakeTask = SetAwaitReadAsync();

        Assert.AreEqual(0, asyncLocal.Value);

        tcs.SetResult();

        var result = await fakeTask;

        Assert.AreEqual(42, result);

        async FakeTask<int> SetAwaitReadAsync()
        {
            asyncLocal.Value = 42;

            await tcs.Task.ConfigureAwait(false);

            return asyncLocal.Value;
        }
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task OnCompletedで登録した継続はキャプチャしたExecutionContextの上で実行される()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var asyncLocal = new AsyncLocal<int>();

        var tcs = new TaskCompletionSource();

        var observed = new TaskCompletionSource<int>();

        var fakeTask = DoAsync(
            (Input: 42, Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

                return state.Input;
            });

        var awaiter = fakeTask.GetAwaiter();

        Assert.IsFalse(awaiter.IsCompleted);

        asyncLocal.Value = 42;

        ((INotifyCompletion)awaiter).OnCompleted(() => observed.SetResult(asyncLocal.Value));

        asyncLocal.Value = 0;

        tcs.SetResult();

        Assert.AreEqual(42, await fakeTask);
        Assert.AreEqual(42, await observed.Task.ConfigureAwait(false));
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 例外で同期的に完了するFakeTaskOfTをawaitする()
    {
        var fakeTask = DoAsync<int>(
            static () =>
            {
#pragma warning disable CA2201
                throw new Exception("Oops!");
#pragma warning restore
            });

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask, "Oops!").ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 非同期的に完了するFakeTaskOfTをawaitする()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(
            (Input: 42, Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

                return state.Input;
            });

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        var result = await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 非同期的に完了するFakeTaskOfTを複数回awaitする()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(
            (Input: 42, Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

                return state.Input;
            });

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        var result1 = await fakeTask;
        var result2 = await fakeTask;

        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, result1);
        Assert.AreEqual(42, result2);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 例外で非同期的に完了するFakeTaskOfTをawaitする()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync<(Task Signal, CancellationToken CancellationToken), int>(
            (tcs.Task, testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

#pragma warning disable CA2201
                throw new Exception("Oops!");
#pragma warning restore
            });

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask, "Oops!").ConfigureAwait(false);

        await tcs.Task.ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 同期的に完了するFakeTaskOfTを自前のステートマシンで回す()
    {
        var result = await RunStateMachine(42);

        Assert.AreEqual(42, result);

        static FakeTask<int> RunStateMachine(int input)
        {
            var stateMachine = new DoAsyncStateMachine<int, int>
            {
                Builder = FakeTaskMethodBuilder<int>.Create(),
                Parameters = new()
                {
                    State = input,
                    Action = static state =>
                    {
                        return Task.FromResult(state);
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
    public async Task 非同期的に完了するFakeTaskOfTを自前のステートマシンで回す()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = RunStateMachine(42, tcs.Task, testCancellationToken);

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        var result = await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, result);

        static FakeTask<int> RunStateMachine(int input, Task signal, CancellationToken cancellationToken)
        {
            var stateMachine = new DoAsyncStateMachine<(int Input, Task Signal, CancellationToken CancellationToken), int>
            {
                Builder = FakeTaskMethodBuilder<int>.Create(),
                Parameters = new()
                {
                    State = (input, signal, cancellationToken),
                    Action = static async state =>
                    {
                        await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);
                        return state.Input;
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
        var tcs = new TaskCompletionSource<int>();

        var stateMachine = new DoAsyncStateMachine<Task<int>, int>
        {
            Builder = FakeTaskMethodBuilder<int>.Create(),
            Parameters = new()
            {
                State = tcs.Task,
                Action = static state => state
            }
        };

        ref var builder = ref stateMachine.Builder;

        builder.Start(ref stateMachine);

        Assert.AreEqual(DoAsyncStateMachine<Task<int>, int>.State.Stage1Completed, stateMachine.CurrentState);

        tcs.SetResult(42);

        Assert.AreEqual(42, await builder.Task);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 例外で非同期的に完了するFakeTaskOfTを自前のステートマシンで回す()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = RunStateMachine(tcs.Task, testCancellationToken);

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask, "Oops!").ConfigureAwait(false);

        await tcs.Task.ConfigureAwait(false);

        static FakeTask<int> RunStateMachine(Task signal, CancellationToken cancellationToken)
        {
            var stateMachine = new DoAsyncStateMachine<(Task Signal, CancellationToken CancellationToken), int>
            {
                Builder = FakeTaskMethodBuilder<int>.Create(),
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

    private struct DoAsyncStateMachine<TState, TResult> :
        IAsyncStateMachine
    {
        public DoAsyncStateMachine()
        {
            this.CurrentState = State.NotStarted;
        }

        public FakeTaskMethodBuilder<TResult> Builder;

        public enum State
        {
            NotStarted,
            Stage1Completed,
            Completed
        }

        public struct MethodParameters
        {
            public TState State;

            public Func<TState, Task<TResult>> Action;
        }

        public MethodParameters Parameters;

        public State CurrentState { get; set; }

        private ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter _awaiter;

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

            static void Stage1(ref DoAsyncStateMachine<TState, TResult> self)
            {
                var awaiter = self.Parameters.Action(self.Parameters.State).ConfigureAwait(false).GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    var result = awaiter.GetResult();
                    self.Builder.SetResult(result);
                    self.CurrentState = State.Completed;
                }
                else
                {
                    self.CurrentState = State.Stage1Completed;
                    self._awaiter = awaiter;
                    self.Builder.AwaitUnsafeOnCompleted(ref awaiter, ref self);
                }
            }

            static void Stage2(ref DoAsyncStateMachine<TState, TResult> self)
            {
                var result = self._awaiter.GetResult();
                self.Builder.SetResult(result);
                self.CurrentState = State.Completed;
            }

            static void SetException(ref DoAsyncStateMachine<TState, TResult> self, Exception exception)
            {
                self.Builder.SetException(exception);
                self.CurrentState = State.Completed;
            }
        }

        public readonly void SetStateMachine(
            IAsyncStateMachine stateMachine)
        {
            ArgumentNullException.ThrowIfNull(stateMachine);
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

    private static async FakeTask<TResult> DoAsync<TState, TResult>(TState state, Func<TState, Task<TResult>> action)
    {
        return await action(state).ConfigureAwait(false);
    }

    private static async FakeTask<TResult> DoAsync<TResult>(Func<Task<TResult>> action)
    {
        return await action().ConfigureAwait(false);
    }
}
