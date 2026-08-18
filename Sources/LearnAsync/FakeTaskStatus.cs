namespace LearnAsync;

internal enum FakeTaskStatus
{
    /// <summary>未完了</summary>
    Pending,

    /// <summary>いずれか 1 つのスレッドが結果を書き込むために予約している</summary>
    Completing,

    /// <summary>結果が確定している</summary>
    Completed
}
