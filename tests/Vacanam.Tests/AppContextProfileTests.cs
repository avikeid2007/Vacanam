using Vacanam.Core.Models;
using Vacanam.LLM.Processing;
using Xunit;

namespace Vacanam.Tests;

public class AppContextProfileTests
{
    [Fact]
    public void DefaultProfiles_ContainsExpectedCoreCategories()
    {
        var profiles = AiSettings.DefaultContextProfiles();

        Assert.NotEmpty(profiles);
        Assert.Contains(profiles, p => p.Id == "coding");
        Assert.Contains(profiles, p => p.Id == "chat");
        Assert.Contains(profiles, p => p.Id == "email");
        Assert.Contains(profiles, p => p.Id == "notes");
    }

    [Theory]
    [InlineData("code", "coding IDE or command-line terminal")]
    [InlineData("devenv", "coding IDE or command-line terminal")]
    [InlineData("windowsterminal", "coding IDE or command-line terminal")]
    [InlineData("slack", "chat or messaging application")]
    [InlineData("discord", "chat or messaging application")]
    [InlineData("outlook", "professional email or formal document")]
    [InlineData("winword", "professional email or formal document")]
    [InlineData("notion", "taking notes or drafting in markdown")]
    [InlineData("obsidian", "taking notes or drafting in markdown")]
    public void ResolveSystemPrompt_AppendsProfileInstruction_WhenProcessMatches(string processName, string expectedSnippet)
    {
        var aiSettings = new AiSettings
        {
            EnableContextProfiles = true,
            ContextProfiles = AiSettings.DefaultContextProfiles()
        };

        var context = new ApplicationContext(
            WindowHandle: 0x1234,
            ProcessId: 5678,
            ProcessName: processName,
            WindowTitle: "Work Window"
        );

        string prompt = LlmTextProcessor.ResolveSystemPrompt(aiSettings, context);

        Assert.Contains(expectedSnippet, prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSystemPrompt_DoesNotAppend_WhenProfileIsDisabled()
    {
        var profiles = AiSettings.DefaultContextProfiles();
        var codingProfile = profiles.First(p => p.Id == "coding");
        codingProfile.IsEnabled = false;

        var aiSettings = new AiSettings
        {
            EnableContextProfiles = true,
            ContextProfiles = profiles
        };

        var context = new ApplicationContext(0x1234, 5678, "code", "VS Code");

        string prompt = LlmTextProcessor.ResolveSystemPrompt(aiSettings, context);

        Assert.DoesNotContain("coding IDE or command-line terminal", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSystemPrompt_DoesNotAppend_WhenEnableContextProfilesIsFalse()
    {
        var aiSettings = new AiSettings
        {
            EnableContextProfiles = false,
            ContextProfiles = AiSettings.DefaultContextProfiles()
        };

        var context = new ApplicationContext(0x1234, 5678, "code", "VS Code");

        string prompt = LlmTextProcessor.ResolveSystemPrompt(aiSettings, context);

        Assert.DoesNotContain("coding IDE or command-line terminal", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSystemPrompt_MaintainsConservativeMode_WithContextProfile()
    {
        var aiSettings = new AiSettings
        {
            ConservativeMode = true,
            EnableContextProfiles = true,
            ContextProfiles = AiSettings.DefaultContextProfiles()
        };

        var context = new ApplicationContext(0x1234, 5678, "slack", "Slack #general");

        string prompt = LlmTextProcessor.ResolveSystemPrompt(aiSettings, context);

        Assert.Contains("CRITICAL: Do not rephrase", prompt);
        Assert.Contains("chat or messaging application", prompt);
    }

    [Fact]
    public void ResolveSystemPrompt_FallsBackGracefully_WhenContextIsUnknown()
    {
        var aiSettings = new AiSettings
        {
            EnableContextProfiles = true,
            ContextProfiles = AiSettings.DefaultContextProfiles()
        };

        string prompt = LlmTextProcessor.ResolveSystemPrompt(aiSettings, ApplicationContext.Unknown);

        Assert.DoesNotContain("TARGET CONTEXT:", prompt);
    }
}
