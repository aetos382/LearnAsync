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
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var resultHolder = new ResultHolder();

        var fakeTask = DoAsync(
            (Input: asyncLocal, Output: resultHolder),
            static state =>
            {
                state.Output.SetResult(state.Input.Value);
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
    public async Task 継続はキャプチャされたExecutionContextの上で実行される()
    {
        var asyncLocal = new AsyncLocal<int>();

        var resultHolder = new ResultHolder();

        var tcs = new TaskCompletionSource();

        var fakeTask = default(FakeTask);

        // async FakeTask メソッドの開始を別スレッドに隔離する。
        // FakeTaskMethodBuilder.Start は ExecutionContext を復元しないため、
        // メソッド内での AsyncLocal への書き込みが呼び出し元に漏れる。
        // 隔離しないと、継続を実行する側 (このスレッド) の ExecutionContext が汚染され、
        // ExecutionContext がフローしたかどうかを判定できなくなる。
        var starter = new Thread(() =>
        {
            fakeTask = SetAndReadAsyncLocalAsync(asyncLocal, resultHolder, tcs.Task);
        });

        starter.Start();
        starter.Join();

        Assert.AreEqual(0, asyncLocal.Value, "継続を実行するスレッドの AsyncLocal には値が入っていない。");

        // 継続 (ステートマシンの MoveNext) はこのスレッドの上で実行される。
        // 中断時にキャプチャした ExecutionContext を復元して実行しなければ、
        // 中断前に書き込んだ AsyncLocal の値が失われる。
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

        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var resultHolder = new ResultHolder();

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(
            (Input: asyncLocal, Output: resultHolder, Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

                state.Output.SetResult(state.Input.Value);
            });

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, resultHolder.Result);
    }

    [TestMethod]
    [Timeout(10_000, CooperativeCancellation = true)]
    public async Task 同期的に完了するFakeTaskを自前のステートマシンで回す()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var resultHolder = new ResultHolder();

        await RunStateMachine(asyncLocal, resultHolder);

        Assert.AreEqual(42, resultHolder.Result);

        static FakeTask RunStateMachine(AsyncLocal<int> input, ResultHolder output)
        {
            var stateMachine = new DoAsyncStateMachine<(AsyncLocal<int> Input, ResultHolder Output)>
            {
                Builder = FakeTaskMethodBuilder.Create(),
                Parameters = new()
                {
                    State = (input, output),
                    Action = static state =>
                    {
                        state.Output.SetResult(state.Input.Value);
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

        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var resultHolder = new ResultHolder();

        var tcs = new TaskCompletionSource();

        var fakeTask = RunStateMachine(asyncLocal, resultHolder, tcs.Task, testCancellationToken);

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, resultHolder.Result);

        static FakeTask RunStateMachine(AsyncLocal<int> input, ResultHolder output, Task signal, CancellationToken cancellationToken)
        {
            var stateMachine = new DoAsyncStateMachine<(AsyncLocal<int> Input, ResultHolder Output, Task Signal, CancellationToken CancellationToken)>
            {
                Builder = FakeTaskMethodBuilder.Create(),
                Parameters = new()
                {
                    State = (input, output, signal, cancellationToken),
                    Action = static async state =>
                    {
                        await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);
                        state.Output.SetResult(state.Input.Value);
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

        readonly void IAsyncStateMachine.MoveNext()
        {
            try
            {
                switch (this.CurrentState)
                {
                    case State.NotStarted:
                        Stage1(this);
                        break;

                    case State.Stage1Completed:
                        Stage2(this);
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
                SetException(this, exception);
            }
#pragma warning restore

            static void Stage1(DoAsyncStateMachine<TState> self)
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

            static void Stage2(DoAsyncStateMachine<TState> self)
            {
                self._awaiter.GetResult();
                self.Builder.SetResult();
                self.CurrentState = State.Completed;
            }

            static void SetException(DoAsyncStateMachine<TState> self, Exception exception)
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

    // await をまたいで AsyncLocal の値が保たれるかを見るための async FakeTask メソッド。
    // 中断と再開を扱うのは FakeTaskMethodBuilder なので、ExecutionContext をフローさせる
    // 責務も FakeTaskMethodBuilder にある。
    // (AsyncLocal の読み書きを async ラムダの中で行うと、そのラムダの中断は
    //  AsyncTaskMethodBuilder が扱うことになり、FakeTaskMethodBuilder の検証にならない)
    private static async FakeTask SetAndReadAsyncLocalAsync(
        AsyncLocal<int> asyncLocal,
        ResultHolder output,
        Task signal)
    {
        asyncLocal.Value = 42;

        await signal.ConfigureAwait(false);

        output.SetResult(asyncLocal.Value);
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
