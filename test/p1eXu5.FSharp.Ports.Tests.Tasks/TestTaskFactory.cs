namespace p1eXu5.FSharp.Ports.Tests.Tasks;

public static class TestTaskFactory
{
    public static Task<int> SimpleTaskWithReturn(int value)
        => Task.FromResult(value);
}
