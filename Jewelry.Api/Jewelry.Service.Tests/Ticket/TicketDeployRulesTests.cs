using jewelry.Model.Exceptions;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.ReleaseLog;
using Jewelry.Service.Ticket;

namespace Jewelry.Service.Tests.Ticket;

public class TicketDeployRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 3, 0, 0, DateTimeKind.Utc);

    private static TbtTicket NewTicket(string status = "none") => new() { DeployStatus = status };

    [Theory]
    [InlineData("none")]
    [InlineData("pending")]
    [InlineData("deployed")]
    [InlineData(" Pending ")]
    public void Whitelist_accepts_valid_status(string status)
    {
        var ticket = NewTicket();
        TicketDeployRules.Apply(ticket, status, null, null, Now);
        Assert.Equal(status.Trim().ToLowerInvariant(), ticket.DeployStatus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("done")]
    [InlineData("deploy")]
    public void Whitelist_rejects_invalid_status(string status)
    {
        Assert.Throws<HandleException>(() => TicketDeployRules.Apply(NewTicket(), status, null, null, Now));
    }

    [Fact]
    public void All_null_fails_validation()
    {
        Assert.Throws<HandleException>(() => TicketDeployRules.Apply(NewTicket(), null, null, null, Now));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("none")]
    public void Cannot_go_back_from_deployed(string target)
    {
        var ticket = NewTicket("deployed");
        ticket.DeployDate = Now;
        Assert.Throws<HandleException>(() => TicketDeployRules.Apply(ticket, target, null, null, Now));
        Assert.Equal("deployed", ticket.DeployStatus);
    }

    [Fact]
    public void Deployed_sets_deploy_date()
    {
        var ticket = NewTicket("pending");
        var changed = TicketDeployRules.Apply(ticket, "deployed", null, null, Now);
        Assert.True(changed);
        Assert.Equal(Now, ticket.DeployDate);
    }

    [Fact]
    public void Same_deployed_again_keeps_deploy_date()
    {
        var first = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var ticket = NewTicket("deployed");
        ticket.DeployDate = first;
        var changed = TicketDeployRules.Apply(ticket, "deployed", null, null, Now);
        Assert.False(changed);
        Assert.Equal(first, ticket.DeployDate);
    }

    [Fact]
    public void Null_fields_are_not_touched()
    {
        var ticket = NewTicket("pending");
        ticket.CommitUi = "abc1234";
        ticket.CommitApi = "def5678";
        TicketDeployRules.Apply(ticket, null, null, "1234567", Now);
        Assert.Equal("pending", ticket.DeployStatus);
        Assert.Equal("abc1234", ticket.CommitUi);
        Assert.Equal("1234567", ticket.CommitApi);
    }

    [Fact]
    public void Invalid_commit_rejected_and_nothing_changed()
    {
        var ticket = NewTicket();
        Assert.Throws<HandleException>(() => TicketDeployRules.Apply(ticket, "pending", "xyz", null, Now));
        Assert.Equal("none", ticket.DeployStatus);
    }

    [Fact]
    public void Empty_commit_clears_value()
    {
        var ticket = NewTicket();
        ticket.CommitUi = "abc1234";
        TicketDeployRules.Apply(ticket, null, "", null, Now);
        Assert.Null(ticket.CommitUi);
    }

    [Theory]
    [InlineData("2026.11.06.1530", true)]
    [InlineData("2026.11.06.153", false)]
    [InlineData("1.0.0", false)]
    [InlineData("2026.11.06.1530x", false)]
    [InlineData(null, false)]
    public void AppVersion_validator(string? value, bool expected)
    {
        Assert.Equal(expected, ReleaseLogRules.IsValidAppVersion(value));
    }

    [Theory]
    [InlineData("abc1234", true)]
    [InlineData("ABCDEF0123456789abcdef0123456789abcdef01", true)]
    [InlineData("abc123", false)]
    [InlineData("abc12345678901234567890123456789012345678", false)]
    [InlineData("ghijklm", false)]
    [InlineData(null, false)]
    public void Sha_validator(string? value, bool expected)
    {
        Assert.Equal(expected, ReleaseLogRules.IsValidSha(value));
    }

    [Fact]
    public void ReleaseLog_optional_validation_throws_on_bad_values()
    {
        ReleaseLogRules.ValidateOptional(null, null, null);
        ReleaseLogRules.ValidateOptional("2026.11.06.1530", "abc1234", "abc1234");
        Assert.Throws<HandleException>(() => ReleaseLogRules.ValidateOptional("1.0.0", null, null));
        Assert.Throws<HandleException>(() => ReleaseLogRules.ValidateOptional(null, "zz", null));
        Assert.Throws<HandleException>(() => ReleaseLogRules.ValidateOptional(null, null, "zz"));
    }
}
