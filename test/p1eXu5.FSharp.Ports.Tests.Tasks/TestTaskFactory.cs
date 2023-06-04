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

    public static Task DoTaskAsync()
    {
        return Task.CompletedTask;
    }

    public static Task<ISyncDisposable> CreateSyncDisposableAsync()
        => Task.FromResult<ISyncDisposable>(new SyncDisposable());

    public static Task<IAsynchronousDisposable> CreateAsynchronousDisposableAsync()
        => Task.FromResult<IAsynchronousDisposable>(new AsynchronousDisposable());
}


public interface ISyncDisposable : IDisposable
{ 
    Task DoTaskAsync();
}

public interface IAsynchronousDisposable : IAsyncDisposable
{ 
    Task DoTaskAsync();
}


public sealed class SyncDisposable : ISyncDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }

    public Task DoTaskAsync()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(SyncDisposable));
        }

        return Task.CompletedTask;
    }
}


public sealed class AsynchronousDisposable : IAsynchronousDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return new();
    }

    public Task DoTaskAsync()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(SyncDisposable));
        }

        return Task.CompletedTask;
    }
}