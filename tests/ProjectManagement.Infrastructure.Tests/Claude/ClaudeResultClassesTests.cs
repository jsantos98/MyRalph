using ProjectManagement.Infrastructure.Claude;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Claude;

public class ClaudeResultClassesTests
{
    [Fact]
    public void ClaudeRefinementResult_Constructor_SetsDefaults()
    {
        // Arrange & Act
        var result = new ClaudeRefinementResult();

        // Assert
        Assert.NotNull(result.DeveloperStories);
        Assert.Empty(result.DeveloperStories);
        Assert.NotNull(result.Dependencies);
        Assert.Empty(result.Dependencies);
        Assert.Null(result.Analysis);
    }

    [Fact]
    public void ClaudeRefinementResult_CanSetProperties()
    {
        // Arrange & Act
        var result = new ClaudeRefinementResult
        {
            Analysis = "Test analysis"
        };
        result.DeveloperStories.Add(new DeveloperStoryGeneration
        {
            Title = "Story 1",
            Description = "D1",
            Instructions = "I1"
        });
        result.Dependencies.Add(new StoryDependency
        {
            DependentStoryIndex = 1,
            RequiredStoryIndex = 0
        });

        // Assert
        Assert.Equal("Test analysis", result.Analysis);
        Assert.Single(result.DeveloperStories);
        Assert.Single(result.Dependencies);
    }

    [Fact]
    public void DeveloperStoryGeneration_CanSetProperties()
    {
        // Arrange & Act
        var story = new DeveloperStoryGeneration
        {
            Title = "Test Story",
            Description = "Test Description",
            Instructions = "Test Instructions",
            StoryType = 0
        };

        // Assert
        Assert.Equal("Test Story", story.Title);
        Assert.Equal("Test Description", story.Description);
        Assert.Equal("Test Instructions", story.Instructions);
        Assert.Equal(0, story.StoryType);
    }

    [Fact]
    public void DeveloperStoryGeneration_AllStoryTypes_Work()
    {
        // Arrange & Act
        var implementation = new DeveloperStoryGeneration { Title = "Impl", Description = "D", Instructions = "I", StoryType = 0 };
        var unitTests = new DeveloperStoryGeneration { Title = "Unit", Description = "D", Instructions = "I", StoryType = 1 };
        var featureTests = new DeveloperStoryGeneration { Title = "Feature", Description = "D", Instructions = "I", StoryType = 2 };
        var documentation = new DeveloperStoryGeneration { Title = "Doc", Description = "D", Instructions = "I", StoryType = 3 };

        // Assert
        Assert.Equal(0, implementation.StoryType);
        Assert.Equal(1, unitTests.StoryType);
        Assert.Equal(2, featureTests.StoryType);
        Assert.Equal(3, documentation.StoryType);
    }

    [Fact]
    public void StoryDependency_Constructor_SetsDefaults()
    {
        // Arrange & Act
        var dependency = new StoryDependency();

        // Assert
        Assert.Equal(0, dependency.DependentStoryIndex);
        Assert.Equal(0, dependency.RequiredStoryIndex);
        Assert.Null(dependency.Description);
    }

    [Fact]
    public void StoryDependency_CanSetProperties()
    {
        // Arrange & Act
        var dependency = new StoryDependency
        {
            DependentStoryIndex = 2,
            RequiredStoryIndex = 1,
            Description = "Must complete tests first"
        };

        // Assert
        Assert.Equal(2, dependency.DependentStoryIndex);
        Assert.Equal(1, dependency.RequiredStoryIndex);
        Assert.Equal("Must complete tests first", dependency.Description);
    }

    [Fact]
    public void ClaudeRefinementResult_WithMultipleStories_HandlesCorrectly()
    {
        // Arrange & Act
        var result = new ClaudeRefinementResult();
        result.DeveloperStories.AddRange(new[]
        {
            new DeveloperStoryGeneration { Title = "Story 1", Description = "D1", Instructions = "I1", StoryType = 0 },
            new DeveloperStoryGeneration { Title = "Story 2", Description = "D2", Instructions = "I2", StoryType = 1 },
            new DeveloperStoryGeneration { Title = "Story 3", Description = "D3", Instructions = "I3", StoryType = 2 }
        });
        result.Dependencies.AddRange(new[]
        {
            new StoryDependency { DependentStoryIndex = 1, RequiredStoryIndex = 0 },
            new StoryDependency { DependentStoryIndex = 2, RequiredStoryIndex = 0 }
        });

        // Assert
        Assert.Equal(3, result.DeveloperStories.Count);
        Assert.Equal(2, result.Dependencies.Count);
    }

    [Fact]
    public void DeveloperStoryGeneration_WithEmptyStrings_Works()
    {
        // Arrange & Act
        var story = new DeveloperStoryGeneration
        {
            Title = "",
            Description = "",
            Instructions = ""
        };

        // Assert - properties can be set to empty strings
        Assert.Equal("", story.Title);
        Assert.Equal("", story.Description);
        Assert.Equal("", story.Instructions);
    }

    [Fact]
    public void StoryDependency_WithEmptyDescription_Works()
    {
        // Arrange & Act
        var dependency = new StoryDependency
        {
            DependentStoryIndex = 1,
            RequiredStoryIndex = 0,
            Description = ""
        };

        // Assert - empty description is allowed
        Assert.Equal("", dependency.Description);
    }
}
