using System;
using System.Threading;

namespace KnolTeacher.Desktop.Services;

/// <summary>
/// Owns the named mutex used to enforce one effective KnolTeacher desktop instance.
/// Only the process that created the mutex owns it and may release it.
/// </summary>
public sealed class SingleInstanceLease : IDisposable
{
    private Mutex? _mutex;

    private SingleInstanceLease(Mutex mutex, bool isPrimaryInstance)
    {
        _mutex = mutex;
        IsPrimaryInstance = isPrimaryInstance;
    }

    public bool IsPrimaryInstance { get; }

    public static SingleInstanceLease Acquire(string mutexName)
    {
        if (string.IsNullOrWhiteSpace(mutexName))
        {
            throw new ArgumentException("Mutex name must not be blank.", nameof(mutexName));
        }

        var mutex = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);
        return new SingleInstanceLease(mutex, createdNew);
    }

    public void Dispose()
    {
        var mutex = Interlocked.Exchange(ref _mutex, null);
        if (mutex is null)
        {
            return;
        }

        try
        {
            if (IsPrimaryInstance)
            {
                mutex.ReleaseMutex();
            }
        }
        finally
        {
            mutex.Dispose();
        }
    }
}
