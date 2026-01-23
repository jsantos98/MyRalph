using ProjectManagement.Core.Enums;

namespace ProjectManagement.Core.Exceptions;

/// <summary>
/// Thrown when an invalid state transition is attempted
/// </summary>
public class InvalidStateTransitionException : ProjectManagementException
{
    public Type? EntityType { get; }
    public object? CurrentState { get; }
    public object? TargetState { get; }

    public InvalidStateTransitionException(Enum currentState, Enum targetState, Type? entityType = null)
        : base($"Invalid state transition from '{currentState}' to '{targetState}'" +
               (entityType != null ? $" for {entityType.Name}" : ""))
    {
        EntityType = entityType;
        CurrentState = currentState;
        TargetState = targetState;
    }

    public InvalidStateTransitionException(string message) : base(message)
    {
    }
}
