using Vacanam.Speech.Punctuation;
using Xunit;

namespace Vacanam.Tests;

public class SmartPunctuationProcessorTests
{
    private readonly SmartPunctuationProcessor _processor = new();

    [Fact]
    public void FormatsBasicVerbalPunctuationCorrectly()
    {
        string input = "hello comma world period";
        string result = _processor.Format(input);
        Assert.Equal("Hello, world.", result);
    }

    [Fact]
    public void FormatsLineBreaksAndParagraphs()
    {
        string input = "first line new line second line new paragraph third line";
        string result = _processor.Format(input);
        Assert.Equal("First line\nSecond line\n\nThird line", result);
    }

    [Fact]
    public void FormatsQuestionAndExclamationMarksWithAutoCapitalization()
    {
        string input = "how are you question mark I am doing great exclamation mark";
        string result = _processor.Format(input);
        Assert.Equal("How are you? I am doing great!", result);
    }

    [Fact]
    public void FormatsParenthesesAndQuotes()
    {
        string input = "this is open paren an example close paren in open quote test mode close quote period";
        string result = _processor.Format(input);
        Assert.Equal("This is (an example) in \"test mode\".", result);
    }

    [Fact]
    public void FormatsEmojisProperly()
    {
        string input = "great work thumbs up rocket emoji fire emoji";
        string result = _processor.Format(input);
        Assert.Equal("Great work 👍 🚀 🔥", result);
    }
}
