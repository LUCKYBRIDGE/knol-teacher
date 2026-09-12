using System;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public class SingleInstanceLeaseTests
{
    [Fact]
    public void Acquire_FirstLeaseIsPrimary_SecondLeaseIsSecondary()
    {
        string mutexName = NewMutexName();

        using var first = SingleInstanceLease.Acquire(mutexName);
        using var second = SingleInstanceLease.Acquire(mutexName);

        Assert.True(first.IsPrimaryInstance);
        Assert.False(second.IsPrimaryInstance);
    }

    [Fact]
    public void Dispose_SecondaryLease_DoesNotReleasePrimaryOwnership()
    {
        string mutexName = NewMutexName();
        using var primary = SingleInstanceLease.Acquire(mutexName);
        var secondary = SingleInstanceLease.Acquire(mutexName);

        secondary.Dispose();

        using var third = SingleInstanceLease.Acquire(mutexName);
        Assert.True(primary.IsPrimaryInstance);
        Assert.False(third.IsPrimaryInstance);
    }

    [Fact]
    public void Dispose_PrimaryLease_ReleasesNameForNextInstance()
    {
        string mutexName = NewMutexName();
        var first = SingleInstanceLease.Acquire(mutexName);
        Assert.True(first.IsPrimaryInstance);

        first.Dispose();

        using var next = SingleInstanceLease.Acquire(mutexName);
        Assert.True(next.IsPrimaryInstance);
    }

    [Fact]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        string mutexName = NewMutexName();
        var lease = SingleInstanceLease.Acquire(mutexName);

        lease.Dispose();
        lease.Dispose();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Acquire_BlankName_Throws(string mutexName)
        => Assert.Throws<ArgumentException>(() => SingleInstanceLease.Acquire(mutexName));

    private static string NewMutexName()
        => $"KnolTeacher.Tests.{Guid.NewGuid():N}";
}
