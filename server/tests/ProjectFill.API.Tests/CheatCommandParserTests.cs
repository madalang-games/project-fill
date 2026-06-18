using ProjectFill.API.Dev;
using ProjectFill.Application.Common;
using ProjectFill.Domain.Enums;
using Xunit;

namespace ProjectFill.API.Tests;

public sealed class CheatCommandParserTests
{
    [Fact]
    public void Parse_ValidGoldCommand_ReturnsDomainAndArgs()
    {
        var parsed = CheatCommandParser.Parse("/gold set 99999");

        Assert.Equal(CheatDomain.Gold, parsed.Domain);
        Assert.Equal("gold", parsed.DomainToken);
        Assert.Equal(new[] { "set", "99999" }, parsed.Args);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("gold set 1")]   // missing leading slash
    [InlineData("/")]            // missing domain
    [InlineData("/bogus set 1")] // unknown domain
    public void Parse_Malformed_ThrowsInvalidCommand(string raw)
    {
        var ex = Assert.Throws<GameApiException>(() => CheatCommandParser.Parse(raw));
        Assert.Equal(ErrorCodes.InvalidCommand, ex.Code);
    }

    [Fact]
    public void Catalog_CoversEveryDomain()
    {
        foreach (CheatDomain domain in System.Enum.GetValues(typeof(CheatDomain)))
            Assert.Contains(CheatCommandCatalog.All, s => s.Domain == domain);
    }

    [Fact]
    public void DocsPage_RendersCommandSyntax()
    {
        var html = CheatDocsPage.Render("dev", "p1");

        Assert.Contains("/gold add|red|set {amount}", html);
        Assert.Contains("GAME_ENV", html);
    }
}
