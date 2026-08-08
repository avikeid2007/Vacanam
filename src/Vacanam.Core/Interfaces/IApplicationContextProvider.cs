namespace Vacanam.Core.Interfaces;

/// <summary>
/// High-level facade that provides the current application context,
/// used by the LLM to select context-aware prompts.
/// </summary>
public interface IApplicationContextProvider
{
    /// <summary>Returns the context of the current foreground application.</summary>
    Models.ApplicationContext GetContext();
}
