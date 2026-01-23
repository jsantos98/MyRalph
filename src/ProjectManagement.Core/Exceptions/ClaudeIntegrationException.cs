namespace ProjectManagement.Core.Exceptions;

/// <summary>
/// Thrown when Claude API or Claude Code integration fails
/// </summary>
public class ClaudeIntegrationException : ProjectManagementException
{
    public ClaudeIntegrationException(string message) : base(message)
    {
    }

    public ClaudeIntegrationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
