using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vacanam.Core.Models;
using Vacanam.Speech.Commands;
using Xunit;

namespace Vacanam.Tests;

public class VoiceCommandProcessorTests
{
    private readonly AppSettings _settings;
    private readonly VoiceCommandProcessor _processor;

    public VoiceCommandProcessorTests()
    {
        _settings = new AppSettings
        {
            VoiceCommands = new VoiceCommandsSettings
            {
                Enabled = true,
                EnableSmartPunctuation = true,
                CustomSnippets =
                [
                    new("insert signature", "Best regards,\n[Your Name]"),
                    new("insert date", "{DATE}")
                ]
            }
        };

        var options = Options.Create(_settings);
        var logger = NullLogger<VoiceCommandProcessor>.Instance;
        _processor = new VoiceCommandProcessor(options, logger);
    }

    [Theory]
    [InlineData("select all.", "Select All")]
    [InlineData("undo that", "Undo")]
    [InlineData("copy that", "Copy")]
    [InlineData("paste that", "Paste")]
    [InlineData("save document", "Save Document")]
    [InlineData("press enter", "Press Enter")]
    [InlineData("press escape", "Press Escape")]
    public async Task DetectsBuiltInActionCommands(string input, string expectedCommandName)
    {
        var result = await _processor.ProcessAsync(input, ApplicationContext.Unknown);
        Assert.True(result.WasCommand);
        Assert.Equal(expectedCommandName, result.CommandName);
        Assert.Null(result.ProcessedText); // Action commands do not inject text
    }

    [Fact]
    public async Task ExpandsCustomSnippetMacros()
    {
        var result = await _processor.ProcessAsync("insert signature", ApplicationContext.Unknown);
        Assert.True(result.WasCommand);
        Assert.Equal("Snippet: insert signature", result.CommandName);
        Assert.Equal("Best regards,\n[Your Name]", result.ProcessedText);
    }

    [Fact]
    public async Task ExpandsDynamicDateMacro()
    {
        var result = await _processor.ProcessAsync("insert date", ApplicationContext.Unknown);
        Assert.True(result.WasCommand);
        Assert.Equal("Snippet: insert date", result.CommandName);
        Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), result.ProcessedText);
    }

    [Fact]
    public async Task IgnoresRegularDictation()
    {
        var result = await _processor.ProcessAsync("This is just regular dictation about our product.", ApplicationContext.Unknown);
        Assert.False(result.WasCommand);
    }
}
