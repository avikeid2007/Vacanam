using Vacanam.LLM.Processing;
using Xunit;

namespace Vacanam.Tests;

public class LlmTextProcessorTests
{
    [Fact]
    public void SanitizesOutput_PreservesMultipleLines()
    {
        string raw = "First line of dictation.\nSecond line of dictation.\nThird line of dictation.";
        string result = LlmTextProcessor.SanitizeLlmOutput(raw);
        Assert.Equal("First line of dictation.\nSecond line of dictation.\nThird line of dictation.", result);
    }

    [Fact]
    public void SanitizesOutput_DoesNotTruncateOnInternalNote()
    {
        string raw = "Please note: we will meet at five o'clock tomorrow.";
        string result = LlmTextProcessor.SanitizeLlmOutput(raw);
        Assert.Equal("Please note: we will meet at five o'clock tomorrow.", result);
    }

    [Fact]
    public void SanitizesOutput_StripsTrailingNoteCommentary()
    {
        string raw = "The meeting is rescheduled.\nNote: I corrected the spelling of rescheduled.";
        string result = LlmTextProcessor.SanitizeLlmOutput(raw);
        Assert.Equal("The meeting is rescheduled.", result);
    }

    [Fact]
    public void SanitizesOutput_StripsTrailingExplanationCommentary()
    {
        string raw = "Ready to proceed.\nExplanation: removed filler words from speech.";
        string result = LlmTextProcessor.SanitizeLlmOutput(raw);
        Assert.Equal("Ready to proceed.", result);
    }

    [Theory]
    [InlineData("Cleaned text: Hello world.", "Hello world.")]
    [InlineData("Output: Hello world.", "Hello world.")]
    [InlineData("Result: Hello world.", "Hello world.")]
    public void SanitizesOutput_StripsCommonPrefixes(string input, string expected)
    {
        string result = LlmTextProcessor.SanitizeLlmOutput(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"Quoted text.\"", "Quoted text.")]
    [InlineData("'Single quoted text.'", "Single quoted text.")]
    public void SanitizesOutput_StripsSurroundingQuotes(string input, string expected)
    {
        string result = LlmTextProcessor.SanitizeLlmOutput(input);
        Assert.Equal(expected, result);
    }
}
