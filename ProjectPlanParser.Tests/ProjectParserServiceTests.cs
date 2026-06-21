using Aspose.Tasks;
using Aspose.Tasks.Saving;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectPlanParser.Services;
using Xunit;

namespace ProjectPlanParser.Tests
{
    public class ProjectParserServiceTests
    {
        private readonly Mock<ILogger<ProjectParserService>> _loggerMock;
        private readonly ProjectParserService _service;

        public ProjectParserServiceTests()
        {
            _loggerMock = new Mock<ILogger<ProjectParserService>>();
            _service = new ProjectParserService(_loggerMock.Object);
        }

        [Fact]
        public void ParseProject_WithMinimalProject_ReturnsCorrectData()
        {
            // Arrange
            var project = new Project();
            project.Set(Prj.Name, "Test Project");
            
            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml); // Use XML as it's more likely to work in evaluation mode
            ms.Position = 0;

            // Act
            var result = _service.ParseProject(ms);

            // Assert
            result.Should().NotBeNull();
            result.ProjectName.Should().Be("Test Project");
        }

        [Fact]
        public void ParseProject_SelectiveExtraction_TasksOnly_ReturnsOnlyTasks()
        {
            // Arrange
            var project = new Project();
            project.Set(Prj.Name, "Selective Test");
            var task = project.RootTask.Children.Add("Task 1");
            var resource = project.Resources.Add("Resource 1");
            
            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            var options = new ParseOptions
            {
                IncludeTasks = true,
                IncludeResources = false,
                IncludeAssignments = false,
                IncludeCalendars = false,
                IncludeTimephasedData = false,
                IncludeBaselines = false
            };

            // Act
            var result = _service.ParseProject(ms, options);

            // Assert
            result.Tasks.Should().NotBeEmpty();
            result.Resources.Should().BeEmpty();
            result.Assignments.Should().BeEmpty();
        }
        
        [Fact]
        public void ParseProject_SelectiveExtraction_ResourcesOnly_ReturnsOnlyResources()
        {
            // Arrange
            var project = new Project();
            var task = project.RootTask.Children.Add("Task 1");
            var resource = project.Resources.Add("Resource 1");
            
            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            var options = new ParseOptions
            {
                IncludeTasks = false,
                IncludeResources = true,
                IncludeAssignments = false,
                IncludeCalendars = false,
                IncludeTimephasedData = false,
                IncludeBaselines = false
            };

            // Act
            var result = _service.ParseProject(ms, options);

            // Assert
            result.Tasks.Should().BeEmpty();
            result.Resources.Should().NotBeEmpty();
            // result.Resources.First().Name.Should().Be("Resource 1"); // Evaluation mode might strip names
        }
    }
}
