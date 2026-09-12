using System.Diagnostics;

namespace KnolTeacher.Desktop.Services;

public interface IWindowProcessCandidate : IDisposable
{
    int Id { get; }
    IntPtr MainWindowHandle { get; }
    void Refresh();
}

public interface IWindowProcessLookup
{
    IEnumerable<IWindowProcessCandidate> GetProcessesByName(string processName);
}

public sealed class SingleInstanceWindowActivator
{
    private static readonly string[] ExecutableAliases = ["놀티쳐", "KnolTeacher.Desktop"];

    private readonly IWindowProcessLookup _processLookup;
    private readonly Action<int> _delay;

    public SingleInstanceWindowActivator()
        : this(new SystemWindowProcessLookup(), Thread.Sleep)
    {
    }

    public SingleInstanceWindowActivator(IWindowProcessLookup processLookup, Action<int> delay)
    {
        _processLookup = processLookup ?? throw new ArgumentNullException(nameof(processLookup));
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
    }

    public IntPtr FindExistingWindowHandle(
        int currentProcessId,
        string currentProcessName,
        int refreshAttempts = 8,
        int retryDelayMs = 200)
    {
        if (string.IsNullOrWhiteSpace(currentProcessName))
        {
            throw new ArgumentException("Current process name is required.", nameof(currentProcessName));
        }

        return FindExistingWindowHandle(
            currentProcessId,
            [currentProcessName, .. ExecutableAliases],
            refreshAttempts,
            retryDelayMs);
    }

    public IntPtr FindExistingWindowHandle(
        int currentProcessId,
        IEnumerable<string> processNames,
        int refreshAttempts = 8,
        int retryDelayMs = 200)
    {
        ArgumentNullException.ThrowIfNull(processNames);
        if (currentProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentProcessId));
        }
        if (refreshAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshAttempts));
        }
        if (retryDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelayMs));
        }

        List<IWindowProcessCandidate> candidates = CollectUniqueCandidates(currentProcessId, processNames);
        if (candidates.Count == 0)
        {
            return IntPtr.Zero;
        }

        try
        {
            IntPtr handle = FindFirstWindowHandle(candidates);
            if (handle != IntPtr.Zero)
            {
                return handle;
            }

            for (int attempt = 0; attempt < refreshAttempts; attempt++)
            {
                _delay(retryDelayMs);

                foreach (IWindowProcessCandidate candidate in candidates)
                {
                    try
                    {
                        candidate.Refresh();
                    }
                    catch
                    {
                        // A candidate can exit while the secondary instance is looking for its window.
                        // Keep checking the remaining candidates instead of aborting activation entirely.
                    }
                }

                handle = FindFirstWindowHandle(candidates);
                if (handle != IntPtr.Zero)
                {
                    return handle;
                }
            }

            return IntPtr.Zero;
        }
        finally
        {
            foreach (IWindowProcessCandidate candidate in candidates)
            {
                try
                {
                    candidate.Dispose();
                }
                catch
                {
                }
            }
        }
    }

    private List<IWindowProcessCandidate> CollectUniqueCandidates(
        int currentProcessId,
        IEnumerable<string> processNames)
    {
        var candidatesById = new Dictionary<int, IWindowProcessCandidate>();
        var queriedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? processName in processNames)
        {
            if (string.IsNullOrWhiteSpace(processName) || !queriedNames.Add(processName))
            {
                continue;
            }

            IEnumerable<IWindowProcessCandidate> queriedCandidates;
            try
            {
                queriedCandidates = _processLookup.GetProcessesByName(processName).ToArray();
            }
            catch
            {
                continue;
            }

            foreach (IWindowProcessCandidate candidate in queriedCandidates)
            {
                int candidateId;
                try
                {
                    candidateId = candidate.Id;
                }
                catch
                {
                    TryDispose(candidate);
                    continue;
                }

                if (candidateId == currentProcessId || candidatesById.ContainsKey(candidateId))
                {
                    TryDispose(candidate);
                    continue;
                }

                candidatesById.Add(candidateId, candidate);
            }
        }

        return candidatesById.Values.ToList();
    }

    private static IntPtr FindFirstWindowHandle(IEnumerable<IWindowProcessCandidate> candidates)
    {
        foreach (IWindowProcessCandidate candidate in candidates)
        {
            try
            {
                IntPtr handle = candidate.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    return handle;
                }
            }
            catch
            {
            }
        }

        return IntPtr.Zero;
    }

    private static void TryDispose(IWindowProcessCandidate candidate)
    {
        try
        {
            candidate.Dispose();
        }
        catch
        {
        }
    }

    private sealed class SystemWindowProcessLookup : IWindowProcessLookup
    {
        public IEnumerable<IWindowProcessCandidate> GetProcessesByName(string processName)
            => Process.GetProcessesByName(processName)
                .Select(process => new SystemWindowProcessCandidate(process))
                .ToArray();
    }

    private sealed class SystemWindowProcessCandidate(Process process) : IWindowProcessCandidate
    {
        public int Id => process.Id;
        public IntPtr MainWindowHandle => process.MainWindowHandle;
        public void Refresh() => process.Refresh();
        public void Dispose() => process.Dispose();
    }
}
