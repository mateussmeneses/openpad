using NUnit.Framework;
using OpenPad.Core.Engine;

namespace OpenPad.Tests;

[TestFixture]
public class ProcessSecurityGuardTests
{
    private ProcessSecurityGuard _guard = null!;

    [SetUp]
    public void SetUp()
    {
        _guard = new ProcessSecurityGuard();
    }

    [Test]
    public void EvaluateProcess_NormalGameProcess_AllowsInput()
    {
        var action = _guard.EvaluateProcess("Cyberpunk2077.exe");
        Assert.That(action, Is.EqualTo(SecurityGuardAction.Allow));
    }

    [Test]
    public void EvaluateProcess_TaskManagerOrUAC_TriggersDisarm()
    {
        var action1 = _guard.EvaluateProcess(@"C:\Windows\System32\Taskmgr.exe");
        var action2 = _guard.EvaluateProcess("consent.exe");

        Assert.That(action1, Is.EqualTo(SecurityGuardAction.TriggerEmergencyDisarm));
        Assert.That(action2, Is.EqualTo(SecurityGuardAction.TriggerEmergencyDisarm));
    }

    [Test]
    public void EvaluateProcess_WhenAutoDisarmFalse_ReturnsBlockInputs()
    {
        _guard.AutoDisarmOnBlacklist = false;

        var action = _guard.EvaluateProcess("Taskmgr.exe");

        Assert.That(action, Is.EqualTo(SecurityGuardAction.BlockInputs));
    }

    [Test]
    public void EvaluateProcess_WhitelistOverride_AllowsExecution()
    {
        _guard.AddWhitelist("taskmgr.exe");

        var action = _guard.EvaluateProcess("taskmgr.exe");

        Assert.That(action, Is.EqualTo(SecurityGuardAction.Allow));
    }

    [Test]
    public void DynamicBlacklist_AddAndRemove_WorksCorrectly()
    {
        _guard.AddBlacklist("MaliciousApp");
        Assert.That(_guard.IsBlacklisted("MaliciousApp.exe"), Is.True);

        bool removed = _guard.RemoveBlacklist("MaliciousApp");
        Assert.That(removed, Is.True);
        Assert.That(_guard.IsBlacklisted("MaliciousApp.exe"), Is.False);
    }
}
