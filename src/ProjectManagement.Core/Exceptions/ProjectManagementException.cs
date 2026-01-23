namespace ProjectManagement.Core.Exceptions;

/// <summary>
/// Base exception for all project management related exceptions
/// </summary>
public abstract class ProjectManagementException : Exception
{
    public ProjectManagementException(string message) : base(message)
    {
    }

    public ProjectManagementException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
