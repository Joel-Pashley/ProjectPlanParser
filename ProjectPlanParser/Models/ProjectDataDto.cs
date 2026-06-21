using System;
using System.Collections.Generic;

namespace ProjectPlanParser.Models
{
    public class ProjectDataDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<TaskDto> Tasks { get; set; } = new();
        public List<ResourceDto> Resources { get; set; } = new();
        public List<AssignmentDto> Assignments { get; set; } = new();
        public List<CalendarDto> Calendars { get; set; } = new();
    }

    public class TaskDto
    {
        public int Id { get; set; }
        public int UniqueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public bool IsMilestone { get; set; }
        public bool IsDeliverable { get; set; }
        public double PercentComplete { get; set; }
        public List<TimephasedDataDto> TimephasedData { get; set; } = new();
        public List<TaskBaselineDto> Baselines { get; set; } = new();
    }

    public class ResourceDto
    {
        public int Id { get; set; }
        public int UniqueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class AssignmentDto
    {
        public int TaskUniqueId { get; set; }
        public int ResourceUniqueId { get; set; }
        public double Units { get; set; }
        public double Work { get; set; }
        public double Cost { get; set; }
    }

    public class CalendarDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsBaseCalendar { get; set; }
    }

    public class TaskBaselineDto
    {
        public int BaselineNumber { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public double Cost { get; set; }
        public double Work { get; set; }
    }

    public class TimephasedDataDto
    {
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public double Value { get; set; }
    }
}
