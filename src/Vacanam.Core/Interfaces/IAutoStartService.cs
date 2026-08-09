namespace Vacanam.Core.Interfaces;

/// <summary>
/// Service interface for managing Windows startup auto-launch registration.
/// </summary>
public interface IAutoStartService
{
    /// <summary>Returns true if Vacanam is registered to run on Windows startup.</summary>
    bool IsAutoStartEnabled();

    /// <summary>Enables or disables Vacanam startup registration in Windows registry.</summary>
    void SetAutoStart(bool enable);
}
