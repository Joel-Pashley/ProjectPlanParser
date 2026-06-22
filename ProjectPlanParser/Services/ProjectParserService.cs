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
        public bool IncludeProjectProperties { get; set; } = true;
        public bool IncludeTasks { get; set; } = true;
        public bool IncludeResources { get; set; } = true;
        public bool IncludeAssignments { get; set; } = true;
        public bool IncludeCalendars { get; set; } = true;
        public bool IncludeTaskLinks { get; set; } = true;
        public bool IncludeTimephasedData { get; set; } = true;
        public bool IncludeBaselines { get; set; } = true;
        public bool IncludeExtendedAttributes { get; set; } = true;
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
            _logger.LogInformation(
                "Starting project parsing. Options: Properties={Props}, Tasks={Tasks}, Resources={Resources}, Assignments={Assignments}, Calendars={Calendars}, TaskLinks={Links}, Timephased={Timephased}, Baselines={Baselines}, ExtendedAttributes={Extended}",
                options.IncludeProjectProperties, options.IncludeTasks, options.IncludeResources, options.IncludeAssignments,
                options.IncludeCalendars, options.IncludeTaskLinks, options.IncludeTimephasedData, options.IncludeBaselines,
                options.IncludeExtendedAttributes);

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

            if (options.IncludeProjectProperties)
            {
                result.Properties = ExtractProperties(project);
            }

            if (options.IncludeResources)
            {
                ExtractResources(project, result, options);
            }

            if (options.IncludeAssignments)
            {
                ExtractAssignments(project, result, options);
            }

            if (options.IncludeCalendars)
            {
                ExtractCalendars(project, result);
            }

            if (options.IncludeTasks)
            {
                ExtractTasks(project, result, options);
            }

            if (options.IncludeTaskLinks)
            {
                ExtractTaskLinks(project, result);
            }

            _logger.LogInformation(
                "Project parsing completed successfully. Extracted {TaskCount} tasks, {ResourceCount} resources, {AssignmentCount} assignments, {CalendarCount} calendars, {LinkCount} links.",
                result.Tasks.Count, result.Resources.Count, result.Assignments.Count, result.Calendars.Count, result.TaskLinks.Count);
            return result;
        }

        private ProjectPropertiesDto ExtractProperties(Project project)
        {
            _logger.LogDebug("Extracting project properties...");
            return new ProjectPropertiesDto
            {
                Title = project.Get(Prj.Title),
                Author = project.Get(Prj.Author),
                Manager = project.Get(Prj.Manager),
                Company = project.Get(Prj.Company),
                Subject = project.Get(Prj.Subject),
                Category = project.Get(Prj.Category),
                Comments = project.Get(Prj.Comments),
                StartDate = Nz(project.Get(Prj.StartDate)),
                FinishDate = Nz(project.Get(Prj.FinishDate)),
                StatusDate = Nz(project.Get(Prj.StatusDate)),
                CurrentDate = Nz(project.Get(Prj.CurrentDate)),
                CreationDate = Nz(project.Get(Prj.CreationDate)),
                LastSaved = Nz(project.Get(Prj.LastSaved)),
                ScheduleFromStart = project.Get(Prj.ScheduleFromStart),
                CurrencyCode = project.Get(Prj.CurrencyCode),
                CurrencySymbol = project.Get(Prj.CurrencySymbol),
                MinutesPerDay = project.Get(Prj.MinutesPerDay),
                MinutesPerWeek = project.Get(Prj.MinutesPerWeek),
                DaysPerMonth = project.Get(Prj.DaysPerMonth)
            };
        }

        private void ExtractResources(Project project, ProjectDataDto result, ParseOptions options)
        {
            _logger.LogDebug("Extracting resources...");
            foreach (var resource in project.Resources)
            {
                if (resource.Get(Rsc.IsNull).Value) continue;

                var resourceDto = new ResourceDto
                {
                    Id = resource.Get(Rsc.Id),
                    UniqueId = resource.Get(Rsc.Uid),
                    Name = resource.Get(Rsc.Name),
                    Type = resource.Get(Rsc.Type).ToString(),
                    Initials = resource.Get(Rsc.Initials),
                    Group = resource.Get(Rsc.Group),
                    EmailAddress = resource.Get(Rsc.EMailAddress),
                    Code = resource.Get(Rsc.Code),
                    Notes = resource.Get(Rsc.NotesText),
                    IsGeneric = resource.Get(Rsc.IsGeneric),
                    IsCostResource = resource.Get(Rsc.IsCostResource),
                    MaterialLabel = resource.Get(Rsc.MaterialLabel),
                    MaxUnits = resource.Get(Rsc.MaxUnits),
                    PeakUnits = resource.Get(Rsc.PeakUnits),
                    StandardRate = (double)resource.Get(Rsc.StandardRate),
                    OvertimeRate = (double)resource.Get(Rsc.OvertimeRate),
                    CostPerUse = (double)resource.Get(Rsc.CostPerUse),
                    AccrueAt = resource.Get(Rsc.AccrueAt).ToString(),
                    Work = resource.Get(Rsc.Work).ToDouble(),
                    ActualWork = resource.Get(Rsc.ActualWork).ToDouble(),
                    RemainingWork = resource.Get(Rsc.RemainingWork).ToDouble(),
                    OvertimeWork = resource.Get(Rsc.OvertimeWork).ToDouble(),
                    Cost = (double)resource.Get(Rsc.Cost),
                    ActualCost = (double)resource.Get(Rsc.ActualCost),
                    RemainingCost = (double)resource.Get(Rsc.RemainingCost)
                };

                if (options.IncludeExtendedAttributes)
                {
                    PopulateExtendedAttributes(resource.ExtendedAttributes, resourceDto.ExtendedAttributes);
                }

                result.Resources.Add(resourceDto);
            }
        }

        private void ExtractAssignments(Project project, ProjectDataDto result, ParseOptions options)
        {
            _logger.LogDebug("Extracting assignments...");
            foreach (var assignment in project.ResourceAssignments)
            {
                var task = assignment.Get(Asn.Task);
                var resource = assignment.Get(Asn.Resource);

                var assignmentDto = new AssignmentDto
                {
                    UniqueId = assignment.Get(Asn.Uid),
                    TaskUniqueId = task != null ? task.Get(Tsk.Uid) : 0,
                    ResourceUniqueId = resource != null ? resource.Get(Rsc.Uid) : 0,
                    Units = assignment.Get(Asn.Units),
                    Start = Nz(assignment.Get(Asn.Start)),
                    Finish = Nz(assignment.Get(Asn.Finish)),
                    ActualStart = Nz(assignment.Get(Asn.ActualStart)),
                    ActualFinish = Nz(assignment.Get(Asn.ActualFinish)),
                    Work = assignment.Get(Asn.Work).ToDouble(),
                    ActualWork = assignment.Get(Asn.ActualWork).ToDouble(),
                    RemainingWork = assignment.Get(Asn.RemainingWork).ToDouble(),
                    OvertimeWork = assignment.Get(Asn.OvertimeWork).ToDouble(),
                    Cost = (double)assignment.Get(Asn.Cost),
                    ActualCost = (double)assignment.Get(Asn.ActualCost),
                    RemainingCost = (double)assignment.Get(Asn.RemainingCost),
                    PercentWorkComplete = assignment.Get(Asn.PercentWorkComplete),
                    Notes = assignment.Get(Asn.NotesText)
                };

                if (options.IncludeTimephasedData)
                {
                    var tpd = assignment.GetTimephasedData(
                        assignment.Get(Asn.Start), assignment.Get(Asn.Finish), TimephasedDataType.AssignmentWork);
                    foreach (var td in tpd)
                    {
                        assignmentDto.TimephasedData.Add(new TimephasedDataDto
                        {
                            Start = td.Start,
                            Finish = td.Finish,
                            Value = double.TryParse(td.Value, out var v) ? v : 0
                        });
                    }
                }

                result.Assignments.Add(assignmentDto);
            }
        }

        private void ExtractCalendars(Project project, ProjectDataDto result)
        {
            _logger.LogDebug("Extracting calendars...");
            foreach (var calendar in project.Calendars)
            {
                var calendarDto = new CalendarDto
                {
                    UniqueId = calendar.Uid,
                    Name = calendar.Name,
                    IsBaseCalendar = calendar.IsBaseCalendar,
                    BaseCalendarName = calendar.BaseCalendar?.Name
                };

                foreach (WeekDay weekDay in calendar.WeekDays)
                {
                    if (weekDay.DayType == DayType.Exception) continue;

                    var weekDayDto = new WeekDayDto
                    {
                        DayOfWeek = weekDay.DayType.ToString(),
                        IsWorking = weekDay.DayWorking
                    };

                    foreach (WorkingTime wt in weekDay.WorkingTimes)
                    {
                        weekDayDto.WorkingTimes.Add(new WorkingTimeDto
                        {
                            From = wt.From.ToString("HH:mm"),
                            To = wt.To.ToString("HH:mm")
                        });
                        weekDayDto.WorkingHours += (wt.To - wt.From).TotalHours;
                    }

                    calendarDto.WeekDays.Add(weekDayDto);
                }

                foreach (CalendarException ex in calendar.Exceptions)
                {
                    var exceptionDto = new CalendarExceptionDto
                    {
                        Name = ex.Name,
                        FromDate = ex.FromDate,
                        ToDate = ex.ToDate,
                        IsWorking = ex.DayWorking
                    };

                    foreach (WorkingTime wt in ex.WorkingTimes)
                    {
                        exceptionDto.WorkingTimes.Add(new WorkingTimeDto
                        {
                            From = wt.From.ToString("HH:mm"),
                            To = wt.To.ToString("HH:mm")
                        });
                    }

                    calendarDto.Exceptions.Add(exceptionDto);
                }

                result.Calendars.Add(calendarDto);
            }
        }

        private void ExtractTasks(Project project, ProjectDataDto result, ParseOptions options)
        {
            _logger.LogDebug("Extracting tasks...");
            foreach (var task in project.RootTask.SelectAllChildTasks())
            {
                var taskDto = new TaskDto
                {
                    Id = task.Get(Tsk.Id),
                    UniqueId = task.Get(Tsk.Uid),
                    Name = task.Get(Tsk.Name),
                    OutlineLevel = task.Get(Tsk.OutlineLevel),
                    OutlineNumber = task.Get(Tsk.OutlineNumber),
                    Wbs = task.Get(Tsk.WBS),
                    IsSummary = task.Get(Tsk.IsSummary),
                    Start = task.Get(Tsk.Start),
                    Finish = task.Get(Tsk.Finish),
                    ActualStart = Nz(task.Get(Tsk.ActualStart)),
                    ActualFinish = Nz(task.Get(Tsk.ActualFinish)),
                    EarlyStart = Nz(task.Get(Tsk.EarlyStart)),
                    EarlyFinish = Nz(task.Get(Tsk.EarlyFinish)),
                    LateStart = Nz(task.Get(Tsk.LateStart)),
                    LateFinish = Nz(task.Get(Tsk.LateFinish)),
                    Deadline = Nz(task.Get(Tsk.Deadline)),
                    Duration = task.Get(Tsk.Duration).ToString(),
                    ConstraintType = task.Get(Tsk.ConstraintType).ToString(),
                    ConstraintDate = Nz(task.Get(Tsk.ConstraintDate)),
                    FreeSlackHours = task.Get(Tsk.FreeSlackTimeSpan).TotalHours,
                    TotalSlackHours = task.Get(Tsk.TotalSlackTimeSpan).TotalHours,
                    Work = task.Get(Tsk.Work).ToDouble(),
                    ActualWork = task.Get(Tsk.ActualWork).ToDouble(),
                    RemainingWork = task.Get(Tsk.RemainingWork).ToDouble(),
                    Cost = (double)task.Get(Tsk.Cost),
                    ActualCost = (double)task.Get(Tsk.ActualCost),
                    RemainingCost = (double)task.Get(Tsk.RemainingCost),
                    FixedCost = (double)task.Get(Tsk.FixedCost),
                    PercentComplete = task.Get(Tsk.PercentComplete),
                    PercentWorkComplete = task.Get(Tsk.PercentWorkComplete),
                    IsMilestone = task.Get(Tsk.IsMilestone),
                    IsCritical = task.Get(Tsk.IsCritical),
                    IsActive = task.Get(Tsk.IsActive),
                    IsManual = task.Get(Tsk.IsManual),
                    Priority = task.Get(Tsk.Priority).ToString(),
                    Notes = task.Get(Tsk.NotesText)
                };

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

                if (options.IncludeExtendedAttributes)
                {
                    PopulateExtendedAttributes(task.ExtendedAttributes, taskDto.ExtendedAttributes);
                }

                result.Tasks.Add(taskDto);
            }
        }

        private void ExtractTaskLinks(Project project, ProjectDataDto result)
        {
            _logger.LogDebug("Extracting task links...");
            foreach (TaskLink link in project.TaskLinks)
            {
                result.TaskLinks.Add(new TaskLinkDto
                {
                    PredecessorUniqueId = link.PredTask != null ? link.PredTask.Get(Tsk.Uid) : 0,
                    SuccessorUniqueId = link.SuccTask != null ? link.SuccTask.Get(Tsk.Uid) : 0,
                    LinkType = link.LinkType.ToString(),
                    Lag = link.LinkLag.ToString()
                });
            }
        }

        private static void PopulateExtendedAttributes(
            ExtendedAttributeCollection source, List<ExtendedAttributeDto> target)
        {
            foreach (ExtendedAttribute ea in source)
            {
                // Each value property is only valid for its matching field type — reading e.g.
                // NumericValue on a Text field throws — so dispatch on the definition's type.
                var type = ea.AttributeDefinition?.CfType ?? CustomFieldType.Null;
                string? value = type switch
                {
                    CustomFieldType.Cost or CustomFieldType.Number => ea.NumericValue.ToString(),
                    CustomFieldType.Date or CustomFieldType.Start or CustomFieldType.Finish => ea.DateValue.ToString("o"),
                    CustomFieldType.Duration => ea.DurationValue.ToString(),
                    CustomFieldType.Flag => ea.FlagValue.ToString(),
                    _ => ea.TextValue
                };

                target.Add(new ExtendedAttributeDto
                {
                    FieldName = ea.AttributeDefinition?.FieldName ?? ea.FieldId,
                    Value = value
                });
            }
        }

        /// <summary>Normalizes Aspose "not-available" sentinel dates (Min/Max) to null.</summary>
        private static DateTime? Nz(DateTime value)
        {
            if (value == DateTime.MinValue || value == DateTime.MaxValue)
            {
                return null;
            }
            // Aspose.Tasks uses a fixed "NA" sentinel around the boundaries of its supported range.
            if (value.Year <= 1984 || value.Year >= 2149)
            {
                return null;
            }
            return value;
        }
    }
}
