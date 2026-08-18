namespace LearnAsync;

internal enum FakeTaskStatus
{
    // 未完了。結果も例外も書かれていない。
    Pending,

    // いずれか 1 つのスレッドが完了させる権利を取り、結果を書いている途中。
    // 待機側はまだ結果を読んではいけない。
    Completing,

    // 結果または例外が書き終わり、公開された。以後この状態から変化しない。
    Completed
}
