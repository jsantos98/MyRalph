namespace ProjectManagement.Core.Exceptions;

/// <summary>
/// Thrown when Git operations fail
/// </summary>
public class GitOperationException : ProjectManagementException
{
    public string? GitCommand { get; }

    public GitOperationException(string message, string? gitCommand = null) : base(message)
    {
        GitCommand = gitCommand;
    }

    public GitOperationException(string message, Exception innerException, string? gitCommand = null)
        : base(message, innerException)
    {
        GitCommand = gitCommand;
    }
}
