namespace ProjectManagement.Core.Enums;

/// <summary>
/// Represents the type of a DeveloperStory
/// </summary>
public enum DeveloperStoryType
{
    /// <summary>
    /// Main implementation work
    /// </summary>
    Implementation = 0,

    /// <summary>
    /// Unit test development
    /// </summary>
    UnitTests = 1,

    /// <summary>
    /// Feature/integration test development
    /// </summary>
    FeatureTests = 2,

    /// <summary>
    /// Documentation updates
    /// </summary>
    Documentation = 3
}
