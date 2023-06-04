namespace p1eXu5.FSharp.Ports.Tests.Tasks;

public static class TestValueTaskFactory
{
    public static ValueTask<int> SimpleValueTaskWithReturn(int value)
        => ValueTask.FromResult(value);
}
