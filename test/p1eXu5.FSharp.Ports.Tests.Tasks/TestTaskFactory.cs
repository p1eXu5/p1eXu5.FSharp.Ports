namespace p1eXu5.FSharp.Ports.Tests.Tasks;

public static class TestTaskFactory
{
    public static Task<int> SimpleTaskWithReturn(int value)
        => Task.FromResult(value);

    public static Task<int> TaskFromException(int _)
        => Task.FromException<int>(new NotImplementedException());

    public static Task<int> SimpleTaskWithException(int value)
    {
        throw new NotImplementedException();
    }
}
