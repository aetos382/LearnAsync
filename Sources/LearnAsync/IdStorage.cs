using System.Threading;

namespace LearnAsync;

internal static class IdStorage
{
    private static int _sharedId;

    public static int NewId()
    {
        int newId;

        // 0 は未割り当てを示すので、上限を超えてオーバーフローした場合に 0 にならないようにする
        do
        {
            newId = Interlocked.Increment(ref _sharedId);
        }
        while (newId == 0);

        return newId;
    }
}
