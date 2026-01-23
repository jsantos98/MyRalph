namespace ProjectManagement.Core.Exceptions;

/// <summary>
/// Thrown when dependency-related operations fail
/// </summary>
public class DependencyException : ProjectManagementException
{
    public DependencyException(string message) : base(message)
    {
    }

    public DependencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
