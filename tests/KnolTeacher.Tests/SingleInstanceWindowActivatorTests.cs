using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public class SingleInstanceWindowActivatorTests
{
    [Fact]
    public void FindExistingWindowHandle_ImmediateHandle_DoesNotDelayOrRefresh()
    {
        var candidate = new FakeCandidate(11, _ => new IntPtr(1234));
        var lookup = new FakeLookup(("primary", [candidate]));
        int delayCalls = 0;
        var activator = new SingleInstanceWindowActivator(lookup, _ => delayCalls++);

        IntPtr handle = activator.FindExistingWindowHandle(99, ["primary"]);

        Assert.Equal(new IntPtr(1234), handle);
        Assert.Equal(0, delayCalls);
        Assert.Equal(0, candidate.RefreshCount);
        Assert.Equal(1, candidate.DisposeCount);
    }

    [Fact]
    public void FindExistingWindowHandle_DeduplicatesSamePidAcrossExecutableNames()
    {
        var first = new FakeCandidate(11, _ => IntPtr.Zero);
        var duplicate = new FakeCandidate(11, _ => IntPtr.Zero);
        var lookup = new FakeLookup(
            ("first", [first]),
            ("alias", [duplicate]));
        int delayCalls = 0;
        var activator = new SingleInstanceWindowActivator(lookup, _ => delayCalls++);

        IntPtr handle = activator.FindExistingWindowHandle(
            99,
            ["first", "alias"],
            refreshAttempts: 1,
            retryDelayMs: 200);

        Assert.Equal(IntPtr.Zero, handle);
        Assert.Equal(1, delayCalls);
        Assert.Equal(1, first.RefreshCount);
        Assert.Equal(0, duplicate.RefreshCount);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, duplicate.DisposeCount);
    }

    [Fact]
    public void FindExistingWindowHandle_PollsAllCandidatesTogetherWithinGlobalRetryBudget()
    {
        var first = new FakeCandidate(11, _ => IntPtr.Zero);
        var second = new FakeCandidate(12, refreshCount =>
            refreshCount >= 2 ? new IntPtr(5678) : IntPtr.Zero);
        var lookup = new FakeLookup(("app", [first, second]));
        int delayCalls = 0;
        var activator = new SingleInstanceWindowActivator(lookup, _ => delayCalls++);

        IntPtr handle = activator.FindExistingWindowHandle(
            99,
            ["app"],
            refreshAttempts: 8,
            retryDelayMs: 200);

        Assert.Equal(new IntPtr(5678), handle);
        Assert.Equal(2, delayCalls);
        Assert.Equal(2, first.RefreshCount);
        Assert.Equal(2, second.RefreshCount);
    }

    [Fact]
    public void FindExistingWindowHandle_CandidateRefreshFailure_DoesNotBlockAnotherCandidate()
    {
        var failing = new FakeCandidate(11, _ => IntPtr.Zero) { ThrowOnRefresh = true };
        var healthy = new FakeCandidate(12, refreshCount =>
            refreshCount >= 1 ? new IntPtr(9012) : IntPtr.Zero);
        var lookup = new FakeLookup(("app", [failing, healthy]));
        var activator = new SingleInstanceWindowActivator(lookup, _ => { });

        IntPtr handle = activator.FindExistingWindowHandle(
            99,
            ["app"],
            refreshAttempts: 2,
            retryDelayMs: 0);

        Assert.Equal(new IntPtr(9012), handle);
        Assert.Equal(1, failing.RefreshCount);
        Assert.Equal(1, healthy.RefreshCount);
    }

    private sealed class FakeLookup(params (string Name, IWindowProcessCandidate[] Candidates)[] entries)
        : IWindowProcessLookup
    {
        private readonly Dictionary<string, IWindowProcessCandidate[]> _entries =
            entries.ToDictionary(entry => entry.Name, entry => entry.Candidates, StringComparer.OrdinalIgnoreCase);

        public IEnumerable<IWindowProcessCandidate> GetProcessesByName(string processName)
            => _entries.TryGetValue(processName, out IWindowProcessCandidate[]? candidates)
                ? candidates
                : [];
    }

    private sealed class FakeCandidate(int id, Func<int, IntPtr> handleFactory) : IWindowProcessCandidate
    {
        public bool ThrowOnRefresh { get; init; }
        public int RefreshCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int Id => id;
        public IntPtr MainWindowHandle => handleFactory(RefreshCount);

        public void Refresh()
        {
            RefreshCount++;
            if (ThrowOnRefresh)
            {
                throw new InvalidOperationException("simulated process exit");
            }
        }

        public void Dispose() => DisposeCount++;
    }
}
