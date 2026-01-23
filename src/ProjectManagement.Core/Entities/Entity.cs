namespace ProjectManagement.Core.Entities;

/// <summary>
/// Base entity class with common properties
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public int Id { get; protected set; }
}
