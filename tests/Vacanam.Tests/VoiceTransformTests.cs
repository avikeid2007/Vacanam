using Vacanam.Core.Models;
using Vacanam.LLM.Processing;
using Xunit;

namespace Vacanam.Tests;

public class VoiceTransformTests
{
    [Fact]
    public void BuildTransformPrompt_WithSelectedText_BuildsInstructionAndSourceTextChatML()
    {
        var settings = new AiSettings();
        var context = ApplicationContext.Unknown;
        string instruction = "Make this more professional";
        string selectedText = "hey boss, can't make it today, cya tomorrow";

        string prompt = LlmTextProcessor.BuildTransformPrompt(instruction, selectedText, settings, context, out string systemPrompt);

        Assert.Contains("<|im_start|>system", prompt);
        Assert.Contains("expert desktop AI assistant", systemPrompt);
        Assert.Contains("Instruction: Make this more professional", prompt);
        Assert.Contains("hey boss, can't make it today, cya tomorrow", prompt);
        Assert.EndsWith("<|im_start|>assistant\n", prompt);
    }

    [Fact]
    public void BuildTransformPrompt_WithoutSelectedText_BuildsDirectGenerationPrompt()
    {
        var settings = new AiSettings();
        var context = ApplicationContext.Unknown;
        string promptQuery = "Draft a polite follow-up email about invoice 1042";

        string prompt = LlmTextProcessor.BuildTransformPrompt(promptQuery, null, settings, context, out string systemPrompt);

        Assert.Contains("<|im_start|>system", prompt);
        Assert.Contains("direct, concise voice AI assistant", systemPrompt);
        Assert.Contains("<|im_start|>user\nDraft a polite follow-up email about invoice 1042\n<|im_end|>", prompt);
        Assert.DoesNotContain("Reference Text:", prompt);
        Assert.EndsWith("<|im_start|>assistant\n", prompt);
    }

    [Fact]
    public void BuildTransformPrompt_IntegratesActiveContextProfile()
    {
        var settings = new AiSettings
        {
            EnableContextProfiles = true,
            ContextProfiles = AiSettings.DefaultContextProfiles()
        };
        var context = new ApplicationContext(
            WindowHandle: 12345,
            ProcessId: 1001,
            ProcessName: "code",
            WindowTitle: "Program.cs - Visual Studio Code"
        );

        string prompt = LlmTextProcessor.BuildTransformPrompt(
            "Refactor this to LINQ",
            "foreach(var item in items) { if (item > 0) result.Add(item); }",
            settings,
            context,
            out string systemPrompt);

        Assert.Contains("TARGET CONTEXT", prompt);
        Assert.Contains("camelCase", systemPrompt);
        Assert.Contains("Reference Text:", prompt);
    }

    [Theory]
    [InlineData("Transformed text: Here is the rewritten content.", "Here is the rewritten content.")]
    [InlineData("Transformed: Successfully updated.", "Successfully updated.")]
    [InlineData("Answer: The answer is 42.", "The answer is 42.")]
    [InlineData("Reply: Thank you for your email.", "Thank you for your email.")]
    [InlineData("Response: We have received your request.", "We have received your request.")]
    public void SanitizeLlmOutput_StripsTransformPrefixes(string input, string expected)
    {
        string sanitized = LlmTextProcessor.SanitizeLlmOutput(input);
        Assert.Equal(expected, sanitized);
    }

    [Fact]
    public void HotkeySettings_Defaults_IncludeShiftSpaceForAiTransform()
    {
        var settings = new HotkeySettings();

        Assert.True(settings.EnableAiTransformHotkey);
        Assert.Equal(4, settings.AiTransformModifiers); // 4 = Shift
        Assert.Equal(0x20, settings.AiTransformVirtualKey); // 0x20 = Space
    }

    [Fact]
    public void GeneralSettings_Defaults_IncludeOnboardingFlags()
    {
        var general = new GeneralSettings();

        Assert.False(general.HasCompletedOnboarding);
        Assert.True(general.ShowLaunchBannerOnStartup);
    }

    [Fact]
    public void IGlobalHotkeyService_ContractIncludesSuppressHoldDetection()
    {
        var testService = new TestHotkeyService();
        testService.SuppressHoldDetection(true);
        Assert.True(testService.Suppressed);
        testService.SuppressHoldDetection(false);
        Assert.False(testService.Suppressed);
    }

#pragma warning disable CS0067
    private sealed class TestHotkeyService : Vacanam.Core.Interfaces.IGlobalHotkeyService
    {
        public bool Suppressed { get; private set; }
        public event EventHandler? HotkeyPressed;
        public event EventHandler? HotkeyReleased;
        public event EventHandler? AiTransformHotkeyPressed;
        public event EventHandler? AiTransformHotkeyReleased;
        public bool IsRegistered => true;
        public bool IsAiTransformRegistered => true;
        public bool Register(nint windowHandle) => true;
        public void Unregister() { }
        public bool UpdateRegistration(nint windowHandle, int modifiers, int virtualKey) => true;
        public bool UpdateAiTransformRegistration(nint windowHandle, int modifiers, int virtualKey) => true;
        public void SuppressHoldDetection(bool suppress) => Suppressed = suppress;
        public void Dispose() { }
    }
#pragma warning restore CS0067
}
