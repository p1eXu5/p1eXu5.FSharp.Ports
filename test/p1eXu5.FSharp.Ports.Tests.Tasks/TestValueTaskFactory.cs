namespace p1eXu5.FSharp.Ports.Tests.Tasks;

public static class TestValueTaskFactory
{
    public static ValueTask<int> SimpleValueTaskWithReturn(int value)
        => ValueTask.FromResult(value);

    public static ValueTask<int> ValueTaskFromException(int _)
        => ValueTask.FromException<int>(new NotImplementedException());

    public static ValueTask<int> SimpleValueTaskWithException(int value)
    {
        throw new NotImplementedException();
    }

    public static ValueTask DoValueTask()
    {
        return ValueTask.CompletedTask;
    }
}
