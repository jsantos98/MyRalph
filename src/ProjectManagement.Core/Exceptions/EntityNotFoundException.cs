namespace ProjectManagement.Core.Exceptions;

/// <summary>
/// Thrown when an entity cannot be found
/// </summary>
public class EntityNotFoundException : ProjectManagementException
{
    public Type EntityType { get; }
    public object Id { get; }

    public EntityNotFoundException(Type entityType, object id)
        : base($"{entityType.Name} with ID '{id}' was not found.")
    {
        EntityType = entityType;
        Id = id;
    }

    public EntityNotFoundException(string message, Type entityType, object id)
        : base(message)
    {
        EntityType = entityType;
        Id = id;
    }
}
