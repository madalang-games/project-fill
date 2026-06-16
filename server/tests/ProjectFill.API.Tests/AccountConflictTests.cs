using ProjectFill.Contracts.Account;
using Xunit;

namespace ProjectFill.API.Tests;

public sealed class AccountConflictTests
{
    [Fact]
    public void ResolveConflictRequest_LocalSelection_IsValid()
    {
        var req = new ResolveConflictRequest { ConflictToken = "abc123", Selection = "local" };
        Assert.Equal("local", req.Selection);
        Assert.Equal("abc123", req.ConflictToken);
    }

    [Fact]
    public void ResolveConflictRequest_CloudSelection_IsValid()
    {
        var req = new ResolveConflictRequest { ConflictToken = "def456", Selection = "cloud" };
        Assert.Equal("cloud", req.Selection);
    }

    [Fact]
    public void SaveSnapshotDto_DefaultsAreZero()
    {
        var snap = new SaveSnapshotDto();
        Assert.Equal(0, snap.MaxStageId);
        Assert.Equal(0L, snap.Gold);
        Assert.Equal(0, snap.TotalClearedStages);
        Assert.Equal(0, snap.TotalItems);
    }

    [Fact]
    public void LinkAccountResponse_ConflictTrue_HasSaveData()
    {
        var resp = new LinkAccountResponse
        {
            Success = false,
            Conflict = true,
            LocalSave = new SaveSnapshotDto { MaxStageId = 50, Gold = 1000, TotalClearedStages = 100, TotalItems = 3 },
            CloudSave = new SaveSnapshotDto { MaxStageId = 20, Gold = 500,  TotalClearedStages = 40,  TotalItems = 1 },
            ConflictToken = "tok123"
        };
        Assert.True(resp.Conflict);
        Assert.Equal(50, resp.LocalSave!.MaxStageId);
        Assert.Equal(20, resp.CloudSave!.MaxStageId);
    }

    [Fact]
    public void LinkAccountRequest_GuestRefreshToken_DefaultsEmpty()
    {
        var req = new LinkAccountRequest();
        Assert.Equal(string.Empty, req.Provider);
        Assert.Equal(string.Empty, req.IdToken);
        Assert.Null(req.GuestRefreshToken);
    }
}
