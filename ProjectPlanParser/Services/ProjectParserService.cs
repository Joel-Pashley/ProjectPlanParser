using Aspose.Tasks;
using ProjectPlanParser.Models;
namespace ProjectPlanParser.Services
{
    public interface IProjectParserService
    {
        ProjectDataDto ParseProject(Stream mppStream, ParseOptions? options = null);
    }

    public class ParseOptions
    {
        public bool IncludeTasks { get; set; } = true;
        public bool IncludeResources { get; set; } = true;
        public bool IncludeAssignments { get; set; } = true;
        public bool IncludeCalendars { get; set; } = true;
        public bool IncludeTimephasedData { get; set; } = true;
        public bool IncludeBaselines { get; set; } = true;
    }

    public class ProjectParserService : IProjectParserService
    {
        private readonly ILogger<ProjectParserService> _logger;

        public ProjectParserService(ILogger<ProjectParserService> logger)
        {
            _logger = logger;
        }

        public ProjectDataDto ParseProject(Stream mppStream, ParseOptions? options = null)
        {
            options ??= new ParseOptions();
            _logger.LogInformation("Starting project parsing. Options: Tasks={Tasks}, Resources={Resources}, Timephased={Timephased}", 
                options.IncludeTasks, options.IncludeResources, options.IncludeTimephasedData);
            
            // For large files, Aspose.Tasks LoadOptions can be used to optimize
            var loadOptions = new LoadOptions
            {
                // We can set specific options here if needed for large files
            };

            var project = new Project(mppStream, loadOptions);
            var result = new ProjectDataDto
            {
                ProjectName = project.Get(Prj.Name) ?? "Unnamed Project"
            };
            
            _logger.LogDebug("Project Name: {ProjectName}", result.ProjectName);

            // Extract Resources
            if (options.IncludeResources)
            {
                _logger.LogDebug("Extracting resources...");
                foreach (var resource in project.Resources)
                {
                    if (resource.Get(Rsc.IsNull).Value) continue;

                    result.Resources.Add(new ResourceDto
                    {
                        Id = resource.Get(Rsc.Id),
                        UniqueId = resource.Get(Rsc.Uid),
                        Name = resource.Get(Rsc.Name),
                        Type = resource.Get(Rsc.Type).ToString()
                    });
                }
            }

            // Extract Assignments
            if (options.IncludeAssignments)
            {
                _logger.LogDebug("Extracting assignments...");
                foreach (var assignment in project.ResourceAssignments)
                {
                    var task = assignment.Get(Asn.Task);
                    var resource = assignment.Get(Asn.Resource);
                    
                    result.Assignments.Add(new AssignmentDto
                    {
                        TaskUniqueId = task != null ? task.Get(Tsk.Uid) : 0,
                        ResourceUniqueId = resource != null ? resource.Get(Rsc.Uid) : 0,
                        Units = assignment.Get(Asn.Units),
                        Work = assignment.Get(Asn.Work).ToDouble(),
                        Cost = (double)assignment.Get(Asn.Cost)
                    });
                }
            }

            // Extract Calendars
            if (options.IncludeCalendars)
            {
                _logger.LogDebug("Extracting calendars...");
                foreach (var calendar in project.Calendars)
                {
                    result.Calendars.Add(new CalendarDto
                    {
                        Name = calendar.Name,
                        IsBaseCalendar = calendar.IsBaseCalendar
                    });
                }
            }

            // Extract Tasks
            if (options.IncludeTasks)
            {
                _logger.LogDebug("Extracting tasks...");
                foreach (var task in project.RootTask.SelectAllChildTasks())
                {
                    var taskDto = new TaskDto
                    {
                        Id = task.Get(Tsk.Id),
                        UniqueId = task.Get(Tsk.Uid),
                        Name = task.Get(Tsk.Name),
                        Start = task.Get(Tsk.Start),
                        Finish = task.Get(Tsk.Finish),
                        IsMilestone = task.Get(Tsk.IsMilestone),
                        PercentComplete = task.Get(Tsk.PercentComplete)
                    };

                    // Extract Task Baselines
                    if (options.IncludeBaselines)
                    {
                        foreach (var baseline in task.Baselines)
                        {
                            taskDto.Baselines.Add(new TaskBaselineDto
                            {
                                BaselineNumber = (int)baseline.BaselineNumber,
                                Start = baseline.Start,
                                Finish = baseline.Finish,
                                Cost = (double)baseline.Cost,
                                Work = baseline.Work.ToDouble()
                            });
                        }
                    }

                    // Timephased Data (Work)
                    if (options.IncludeTimephasedData)
                    {
                        var tpd = task.GetTimephasedData(task.Get(Tsk.Start), task.Get(Tsk.Finish), TimephasedDataType.TaskWork);
                        foreach (var td in tpd)
                        {
                            taskDto.TimephasedData.Add(new TimephasedDataDto
                            {
                                Start = td.Start,
                                Finish = td.Finish,
                                Value = double.TryParse(td.Value, out var v) ? v : 0
                            });
                        }
                    }

                    result.Tasks.Add(taskDto);
                }
            }

            _logger.LogInformation("Project parsing completed successfully. Extracted {TaskCount} tasks and {ResourceCount} resources.", result.Tasks.Count, result.Resources.Count);
            return result;
        }
    }
}
