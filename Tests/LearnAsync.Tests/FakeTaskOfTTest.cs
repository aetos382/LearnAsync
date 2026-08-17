using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.Utilities;

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
    public async Task 同期的に完了するFakeTaskOfTをawaitする()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var fakeTask = DoAsync(
            asyncLocal,
            static state =>
            {
                return Task.FromResult(state.Value);
            });

        var result = await fakeTask;

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 同期的に完了するFakeTaskOfTを複数回awaitする()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var fakeTask = DoAsync(
            asyncLocal,
            static state =>
            {
                return Task.FromResult(state.Value);
            });

        var result1 = await fakeTask;
        var result2 = await fakeTask;

        Assert.AreEqual(42, result1);
        Assert.AreEqual(42, result2);
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 例外で同期的に完了するFakeTaskOfTをawaitする()
    {
        var fakeTask = DoAsync<int>(
            static () =>
            {
#pragma warning disable CA2201
                throw new Exception("Oops!");
#pragma warning restore
            });

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 非同期的に完了するFakeTaskOfTをawaitする()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(
            (Input: asyncLocal, Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

                return state.Input.Value;
            });

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        var result = await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 非同期的に完了するFakeTaskOfTを複数回awaitする()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var tcs = new TaskCompletionSource();

        var fakeTask = DoAsync(
            (Input: asyncLocal, Signal: tcs.Task, CancellationToken: testCancellationToken),
            static async state =>
            {
                await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);

                return state.Input.Value;
            });

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        var result1 = await fakeTask;
        var result2 = await fakeTask;

        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, result1);
        Assert.AreEqual(42, result2);
    }

    [TestMethod]
    [Timeout(10_000)]
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
    [Timeout(10_000)]
    public async Task 同期的に完了するFakeTaskOfTを自前のステートマシンで回す()
    {
        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var result = await RunStateMachine(asyncLocal);

        Assert.AreEqual(42, result);

        static FakeTask<int> RunStateMachine(AsyncLocal<int> input)
        {
            var stateMachine = new DoAsyncStateMachine<AsyncLocal<int>, int>
            {
                Builder = FakeTaskMethodBuilder<int>.Create(),
                Parameters = new()
                {
                    State = input,
                    Action = static state =>
                    {
                        return Task.FromResult(state.Value);
                    }
                }
            };

            ref var builder = ref stateMachine.Builder;

            builder.Start(ref stateMachine);

            return builder.Task;
        }
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 非同期的に完了するFakeTaskOfTを自前のステートマシンで回す()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var asyncLocal = new AsyncLocal<int>
        {
            Value = 42
        };

        var tcs = new TaskCompletionSource();

        var fakeTask = RunStateMachine(asyncLocal, tcs.Task, testCancellationToken);

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        var result = await fakeTask;
        await tcs.Task.ConfigureAwait(false);

        Assert.AreEqual(42, result);

        static FakeTask<int> RunStateMachine(AsyncLocal<int> input, Task signal, CancellationToken cancellationToken)
        {
            var stateMachine = new DoAsyncStateMachine<(AsyncLocal<int> Input, Task Signal, CancellationToken CancellationToken), int>
            {
                Builder = FakeTaskMethodBuilder<int>.Create(),
                Parameters = new()
                {
                    State = (input, signal, cancellationToken),
                    Action = static async state =>
                    {
                        await state.Signal.WaitAsync(state.CancellationToken).ConfigureAwait(false);
                        return state.Input.Value;
                    }
                }
            };

            ref var builder = ref stateMachine.Builder;

            builder.Start(ref stateMachine);

            return builder.Task;
        }
    }

    [TestMethod]
    [Timeout(10_000)]
    public async Task 例外で非同期的に完了するFakeTaskOfTを自前のステートマシンで回す()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        var tcs = new TaskCompletionSource();

        var fakeTask = RunStateMachine(tcs.Task, testCancellationToken);

        var task = DelayedSignal(tcs, TimeSpan.FromSeconds(3), testCancellationToken);

        await Assert.ThrowsAsync<Exception>(async () => await fakeTask).ConfigureAwait(false);

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

        public struct NethodParameters
        {
            public TState State;

            public Func<TState, Task<TResult>> Action;
        }

        public NethodParameters Parameters;

        public State CurrentState { get; set; }

        private ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter _awaiter;

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

            static void Stage1(DoAsyncStateMachine<TState, TResult> self)
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

            static void Stage2(DoAsyncStateMachine<TState, TResult> self)
            {
                var result = self._awaiter.GetResult();
                self.Builder.SetResult(result);
                self.CurrentState = State.Completed;
            }

            static void SetException(DoAsyncStateMachine<TState, TResult> self, Exception exception)
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
