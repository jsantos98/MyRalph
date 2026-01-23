using ProjectManagement.Core.Entities;
using Xunit;

namespace ProjectManagement.Core.Tests.Entities;

public class DeveloperStoryDependencyTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        // Arrange & Act
        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = 1,
            RequiredStoryId = 2
        };

        // Assert
        Assert.Equal(1, dep.DependentStoryId);
        Assert.Equal(2, dep.RequiredStoryId);
        Assert.Null(dep.Description);
        Assert.True(dep.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithDescription_SetsDescription()
    {
        // Arrange & Act
        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = 1,
            RequiredStoryId = 2,
            Description = "Must complete first"
        };

        // Assert
        Assert.Equal("Must complete first", dep.Description);
    }

    [Fact]
    public void Constructor_WithNavigationProperties_SetsNavigationProperties()
    {
        // Arrange
        var dependentStory = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = Core.Enums.DeveloperStoryType.Implementation,
            Title = "Dependent",
            Description = "Description",
            Instructions = "Instructions"
        };

        var requiredStory = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = Core.Enums.DeveloperStoryType.UnitTests,
            Title = "Required",
            Description = "Description",
            Instructions = "Instructions"
        };

        // Act
        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = 1,
            RequiredStoryId = 2,
            DependentStory = dependentStory,
            RequiredStory = requiredStory
        };

        // Assert
        Assert.Same(dependentStory, dep.DependentStory);
        Assert.Same(requiredStory, dep.RequiredStory);
    }
}
