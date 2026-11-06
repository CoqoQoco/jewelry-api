using jewelry.Model.Exceptions;
using jewelry.Model.ReleaseLog;
using Jewelry.Service.ReleaseLog;

namespace Jewelry.Service.Tests.Ticket;

public class ReleaseLogListRulesTests
{
    private static readonly List<string> NoTickets = new();
    private static readonly List<ReleaseLogManualStep> NoSteps = new();

    private static void Validate(List<string>? tickets = null, List<string>? closed = null, List<ReleaseLogManualStep>? steps = null, string? note = null) =>
        ReleaseLogRules.ValidateLists(tickets ?? NoTickets, closed ?? NoTickets, steps ?? NoSteps, note);

    [Theory]
    [InlineData("TK202611060005", true)]
    [InlineData("TK20261106000", false)]
    [InlineData("TK2026110600055", false)]
    [InlineData("tk202611060005", false)]
    [InlineData("TK20261106000A", false)]
    [InlineData("", false)]
    public void TicketNo_format(string value, bool expected) =>
        Assert.Equal(expected, ReleaseLogRules.IsValidTicketNo(value));

    [Fact]
    public void Valid_input_passes() =>
        Validate(new() { "TK202611060005" }, new() { "TK202611060004" },
            new() { new ReleaseLogManualStep { Desc = "run sql", Done = false } }, "ok");

    [Fact]
    public void Bad_ticket_no_throws()
    {
        Assert.Throws<HandleException>(() => Validate(tickets: new() { "XX1" }));
        Assert.Throws<HandleException>(() => Validate(closed: new() { "TK1" }));
    }

    [Fact]
    public void Ticket_count_limit_200()
    {
        List<string> Make(int n) => Enumerable.Range(0, n).Select(i => $"TK{i:D12}").ToList();
        Validate(tickets: Make(200));
        Assert.Throws<HandleException>(() => Validate(tickets: Make(201)));
        Assert.Throws<HandleException>(() => Validate(closed: Make(201)));
    }

    [Fact]
    public void ManualSteps_limits()
    {
        List<ReleaseLogManualStep> Make(int n) => Enumerable.Range(0, n).Select(_ => new ReleaseLogManualStep { Desc = "x" }).ToList();
        Validate(steps: Make(50));
        Assert.Throws<HandleException>(() => Validate(steps: Make(51)));
        Assert.Throws<HandleException>(() => Validate(steps: new() { new ReleaseLogManualStep { Desc = "  " } }));
        Validate(steps: new() { new ReleaseLogManualStep { Desc = new string('a', 500) } });
        Assert.Throws<HandleException>(() => Validate(steps: new() { new ReleaseLogManualStep { Desc = new string('a', 501) } }));
    }

    [Fact]
    public void Note_limit_1000()
    {
        Validate(note: new string('a', 1000));
        Assert.Throws<HandleException>(() => Validate(note: new string('a', 1001)));
    }
}
