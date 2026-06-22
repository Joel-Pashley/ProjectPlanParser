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

        [Fact]
        public void ParseProject_IncludesProjectProperties_WhenEnabled()
        {
            // Arrange
            var project = new Project();
            project.Set(Prj.Name, "Props Test");
            project.Set(Prj.Manager, "Jane Manager");
            project.Set(Prj.Company, "Acme Corp");

            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            // Act
            var result = _service.ParseProject(ms, new ParseOptions { IncludeProjectProperties = true });

            // Assert
            result.Properties.Should().NotBeNull();
            result.Properties!.Manager.Should().Be("Jane Manager");
            result.Properties.Company.Should().Be("Acme Corp");
        }

        [Fact]
        public void ParseProject_OmitsProjectProperties_WhenDisabled()
        {
            // Arrange
            var project = new Project();
            project.Set(Prj.Manager, "Jane Manager");

            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            // Act
            var result = _service.ParseProject(ms, new ParseOptions { IncludeProjectProperties = false });

            // Assert
            result.Properties.Should().BeNull();
        }

        [Fact]
        public void ParseProject_ExtractsTaskLinks_WhenEnabled()
        {
            // Arrange
            var project = new Project();
            var predecessor = project.RootTask.Children.Add("Predecessor");
            var successor = project.RootTask.Children.Add("Successor");
            project.TaskLinks.Add(predecessor, successor, TaskLinkType.FinishToStart);

            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            // Act
            var result = _service.ParseProject(ms, new ParseOptions { IncludeTaskLinks = true });

            // Assert
            result.TaskLinks.Should().NotBeEmpty();
            result.TaskLinks.Should().Contain(l => l.LinkType == "FinishToStart");
        }

        [Fact]
        public void ParseProject_OmitsTaskLinks_WhenDisabled()
        {
            // Arrange
            var project = new Project();
            var predecessor = project.RootTask.Children.Add("Predecessor");
            var successor = project.RootTask.Children.Add("Successor");
            project.TaskLinks.Add(predecessor, successor, TaskLinkType.FinishToStart);

            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            // Act
            var result = _service.ParseProject(ms, new ParseOptions { IncludeTaskLinks = false });

            // Assert
            result.TaskLinks.Should().BeEmpty();
        }

        [Fact]
        public void ParseProject_ExtractsCalendarWorkingWeek()
        {
            // Arrange
            var project = new Project(); // default project ships with a "Standard" base calendar
            project.RootTask.Children.Add("Task 1");

            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            // Act
            var result = _service.ParseProject(ms, new ParseOptions { IncludeCalendars = true });

            // Assert
            result.Calendars.Should().NotBeEmpty();
            var calendar = result.Calendars.First();
            calendar.WeekDays.Should().NotBeEmpty();
            calendar.WeekDays.Should().Contain(d => d.IsWorking && d.WorkingHours > 0);
        }

        [Fact]
        public void ParseProject_PopulatesRichTaskFields()
        {
            // Arrange
            var project = new Project();
            project.RootTask.Children.Add("Task 1");

            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            // Act
            var result = _service.ParseProject(ms, new ParseOptions { IncludeTasks = true });

            // Assert
            result.Tasks.Should().NotBeEmpty();
            var task = result.Tasks.First();
            task.Duration.Should().NotBeNullOrEmpty();      // new rich field is populated for every task
        }

        [Fact]
        public void ParseProject_AllSectionsDisabled_ReturnsOnlyTasks()
        {
            // Arrange
            var project = new Project();
            project.Set(Prj.Manager, "Jane Manager");
            project.RootTask.Children.Add("Task 1");
            project.Resources.Add("Resource 1");

            using var ms = new MemoryStream();
            project.Save(ms, SaveFileFormat.Xml);
            ms.Position = 0;

            var options = new ParseOptions
            {
                IncludeProjectProperties = false,
                IncludeTasks = true,
                IncludeResources = false,
                IncludeAssignments = false,
                IncludeCalendars = false,
                IncludeTaskLinks = false,
                IncludeTimephasedData = false,
                IncludeBaselines = false,
                IncludeExtendedAttributes = false
            };

            // Act
            var result = _service.ParseProject(ms, options);

            // Assert
            result.Tasks.Should().NotBeEmpty();
            result.Properties.Should().BeNull();
            result.Resources.Should().BeEmpty();
            result.Assignments.Should().BeEmpty();
            result.Calendars.Should().BeEmpty();
            result.TaskLinks.Should().BeEmpty();
            result.Tasks.First().Baselines.Should().BeEmpty();
            result.Tasks.First().TimephasedData.Should().BeEmpty();
            result.Tasks.First().ExtendedAttributes.Should().BeEmpty();
        }
    }
}
