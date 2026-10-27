using NUnit.Framework;
using OpenPad.Core.Engine;
using OpenPad.Core.Models;

namespace OpenPad.Tests;

[TestFixture]
public class CliRunnerTests
{
    [Test]
    public void Parse_EmptyArgs_ReturnsDefaultOptions()
    {
        var options = CliRunner.Parse(Array.Empty<string>());

        Assert.That(options.ArmImmediately, Is.False);
        Assert.That(options.StartInTray, Is.False);
        Assert.That(options.ProfilePath, Is.Null);
        Assert.That(options.CurveOverride, Is.Null);
        Assert.That(options.ShowHelp, Is.False);
    }

    [Test]
    public void Parse_HelpAndVersionFlags_SetsBooleans()
    {
        var optHelp = CliRunner.Parse(new[] { "--help" });
        Assert.That(optHelp.ShowHelp, Is.True);

        var optVer = CliRunner.Parse(new[] { "-v" });
        Assert.That(optVer.ShowVersion, Is.True);
    }

    [Test]
    public void Parse_ArmAndTray_SetsAutomationFlags()
    {
        var options = CliRunner.Parse(new[] { "--arm", "--tray" });

        Assert.That(options.ArmImmediately, Is.True);
        Assert.That(options.StartInTray, Is.True);
    }

    [Test]
    public void Parse_ProfileAndCurveOverride_ExtractsValues()
    {
        var options = CliRunner.Parse(new[] { "-p", "CustomDoom.json", "-c", "Aggressive" });

        Assert.That(options.ProfilePath, Is.EqualTo("CustomDoom.json"));
        Assert.That(options.CurveOverride, Is.EqualTo(StickCurveType.Aggressive));
    }

    [Test]
    public void GetHelpText_ContainsAllFlagsAndVersion()
    {
        string text = CliRunner.GetHelpText();

        Assert.That(text, Does.Contain("--profile"));
        Assert.That(text, Does.Contain("--arm"));
        Assert.That(text, Does.Contain("--tray"));
        Assert.That(text, Does.Contain("OpenPad"));
    }
}
