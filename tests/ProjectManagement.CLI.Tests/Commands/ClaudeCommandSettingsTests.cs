using ProjectManagement.CLI.Commands;
using Spectre.Console.Cli;
using Xunit;

namespace ProjectManagement.CLI.Tests.Commands;

/// <summary>
/// Tests for ClaudeCommandSettings class
/// </summary>
public class ClaudeCommandSettingsTests
{
    // Test settings class that inherits from ClaudeCommandSettings
    private class TestSettings : ClaudeCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        public required int Id { get; set; }
    }

    [Fact]
    public void ApiKey_WhenSetDirectly_ReturnsSetValue()
    {
        // Arrange
        var settings = new TestSettings
        {
            Id = 1,
            ApiKey = "test-api-key"
        };

        // Act
        var result = settings.GetApiKey();

        // Assert
        Assert.Equal("test-api-key", result);
    }

    [Fact]
    public void ApiKey_WhenNotSetAndNoEnvVar_ReturnsNull()
    {
        // Arrange
        // Clear the environment variable
        Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", null);

        var settings = new TestSettings
        {
            Id = 1
        };

        // Act
        var result = settings.GetApiKey();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ApiKey_WhenNotSetButEnvVarExists_ReturnsEnvVarValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", "env-api-key");

        var settings = new TestSettings
        {
            Id = 1
        };

        try
        {
            // Act
            var result = settings.GetApiKey();

            // Assert
            Assert.Equal("env-api-key", result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", null);
        }
    }

    [Fact]
    public void ApiKey_CommandLineTakesPrecedenceOverEnvVar()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", "env-api-key");

        var settings = new TestSettings
        {
            Id = 1,
            ApiKey = "cli-api-key"
        };

        try
        {
            // Act
            var result = settings.GetApiKey();

            // Assert
            Assert.Equal("cli-api-key", result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", null);
        }
    }

    [Fact]
    public void BaseUrl_WhenSetDirectly_ReturnsSetValue()
    {
        // Arrange
        var settings = new TestSettings
        {
            Id = 1,
            BaseUrl = "https://api.example.com"
        };

        // Act
        var result = settings.GetBaseUrl();

        // Assert
        Assert.Equal("https://api.example.com", result);
    }

    [Fact]
    public void BaseUrl_WhenNotSetAndNoEnvVar_ReturnsNull()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", null);

        var settings = new TestSettings
        {
            Id = 1
        };

        // Act
        var result = settings.GetBaseUrl();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void BaseUrl_WhenNotSetButEnvVarExists_ReturnsEnvVarValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", "https://env.example.com");

        var settings = new TestSettings
        {
            Id = 1
        };

        try
        {
            // Act
            var result = settings.GetBaseUrl();

            // Assert
            Assert.Equal("https://env.example.com", result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", null);
        }
    }

    [Fact]
    public void BaseUrl_CommandLineTakesPrecedenceOverEnvVar()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", "https://env.example.com");

        var settings = new TestSettings
        {
            Id = 1,
            BaseUrl = "https://cli.example.com"
        };

        try
        {
            // Act
            var result = settings.GetBaseUrl();

            // Assert
            Assert.Equal("https://cli.example.com", result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", null);
        }
    }

    [Fact]
    public void Timeout_WhenSetDirectly_ReturnsSetValue()
    {
        // Arrange
        var settings = new TestSettings
        {
            Id = 1,
            Timeout = 5000
        };

        // Act
        var result = settings.GetTimeout();

        // Assert
        Assert.Equal(5000, result);
    }

    [Fact]
    public void Timeout_WhenNotSetAndNoEnvVar_ReturnsNull()
    {
        // Arrange
        Environment.SetEnvironmentVariable("API_TIMEOUT_MS", null);

        var settings = new TestSettings
        {
            Id = 1
        };

        // Act
        var result = settings.GetTimeout();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Timeout_WhenNotSetButEnvVarExists_ReturnsEnvVarValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("API_TIMEOUT_MS", "10000");

        var settings = new TestSettings
        {
            Id = 1
        };

        try
        {
            // Act
            var result = settings.GetTimeout();

            // Assert
            Assert.Equal(10000, result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("API_TIMEOUT_MS", null);
        }
    }

    [Fact]
    public void Timeout_CommandLineTakesPrecedenceOverEnvVar()
    {
        // Arrange
        Environment.SetEnvironmentVariable("API_TIMEOUT_MS", "10000");

        var settings = new TestSettings
        {
            Id = 1,
            Timeout = 5000
        };

        try
        {
            // Act
            var result = settings.GetTimeout();

            // Assert
            Assert.Equal(5000, result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("API_TIMEOUT_MS", null);
        }
    }

    [Fact]
    public void Timeout_WhenEnvVarIsInvalid_ReturnsNull()
    {
        // Arrange
        Environment.SetEnvironmentVariable("API_TIMEOUT_MS", "invalid");

        var settings = new TestSettings
        {
            Id = 1
        };

        try
        {
            // Act
            var result = settings.GetTimeout();

            // Assert
            Assert.Null(result);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("API_TIMEOUT_MS", null);
        }
    }

    [Fact]
    public void Model_WhenSetDirectly_ReturnsSetValue()
    {
        // Arrange
        var settings = new TestSettings
        {
            Id = 1,
            Model = "GLM-4.7"
        };

        // Act
        var result = settings.GetModel();

        // Assert
        Assert.Equal("GLM-4.7", result);
    }

    [Fact]
    public void Model_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var settings = new TestSettings
        {
            Id = 1
        };

        // Act
        var result = settings.GetModel();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AllOptions_CanBeSetTogether()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", "env-api-key");
        Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", "https://env.example.com");
        Environment.SetEnvironmentVariable("API_TIMEOUT_MS", "10000");

        var settings = new TestSettings
        {
            Id = 1,
            ApiKey = "cli-api-key",
            BaseUrl = "https://cli.example.com",
            Timeout = 5000,
            Model = "GLM-4.7"
        };

        try
        {
            // Act
            var apiKey = settings.GetApiKey();
            var baseUrl = settings.GetBaseUrl();
            var timeout = settings.GetTimeout();
            var model = settings.GetModel();

            // Assert - CLI values should take precedence
            Assert.Equal("cli-api-key", apiKey);
            Assert.Equal("https://cli.example.com", baseUrl);
            Assert.Equal(5000, timeout);
            Assert.Equal("GLM-4.7", model);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", null);
            Environment.SetEnvironmentVariable("API_TIMEOUT_MS", null);
        }
    }

    [Fact]
    public void AllOptions_OnlyEnvVarsSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", "env-api-key");
        Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", "https://env.example.com");
        Environment.SetEnvironmentVariable("API_TIMEOUT_MS", "10000");

        var settings = new TestSettings
        {
            Id = 1,
            Model = "GLM-4.7"
        };

        try
        {
            // Act
            var apiKey = settings.GetApiKey();
            var baseUrl = settings.GetBaseUrl();
            var timeout = settings.GetTimeout();
            var model = settings.GetModel();

            // Assert
            Assert.Equal("env-api-key", apiKey);
            Assert.Equal("https://env.example.com", baseUrl);
            Assert.Equal(10000, timeout);
            Assert.Equal("GLM-4.7", model);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", null);
            Environment.SetEnvironmentVariable("API_TIMEOUT_MS", null);
        }
    }
}
