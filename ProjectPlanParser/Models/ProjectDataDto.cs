using System;
using System.Collections.Generic;

namespace ProjectPlanParser.Models
{
    public class ProjectDataDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public ProjectPropertiesDto? Properties { get; set; }
        public List<TaskDto> Tasks { get; set; } = new();
        public List<ResourceDto> Resources { get; set; } = new();
        public List<AssignmentDto> Assignments { get; set; } = new();
        public List<CalendarDto> Calendars { get; set; } = new();
        public List<TaskLinkDto> TaskLinks { get; set; } = new();
    }

    public class ProjectPropertiesDto
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Manager { get; set; }
        public string? Company { get; set; }
        public string? Subject { get; set; }
        public string? Category { get; set; }
        public string? Comments { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public DateTime? StatusDate { get; set; }
        public DateTime? CurrentDate { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? LastSaved { get; set; }
        public bool ScheduleFromStart { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CurrencySymbol { get; set; }
        public int MinutesPerDay { get; set; }
        public int MinutesPerWeek { get; set; }
        public int DaysPerMonth { get; set; }
    }

    public class TaskDto
    {
        public int Id { get; set; }
        public int UniqueId { get; set; }
        public string Name { get; set; } = string.Empty;

        // Hierarchy
        public int OutlineLevel { get; set; }
        public string? OutlineNumber { get; set; }
        public string? Wbs { get; set; }
        public bool IsSummary { get; set; }

        // Scheduling
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualFinish { get; set; }
        public DateTime? EarlyStart { get; set; }
        public DateTime? EarlyFinish { get; set; }
        public DateTime? LateStart { get; set; }
        public DateTime? LateFinish { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Duration { get; set; }
        public string? ConstraintType { get; set; }
        public DateTime? ConstraintDate { get; set; }
        public double FreeSlackHours { get; set; }
        public double TotalSlackHours { get; set; }

        // Work & cost
        public double Work { get; set; }
        public double ActualWork { get; set; }
        public double RemainingWork { get; set; }
        public double Cost { get; set; }
        public double ActualCost { get; set; }
        public double RemainingCost { get; set; }
        public double FixedCost { get; set; }

        // Progress & flags
        public double PercentComplete { get; set; }
        public double PercentWorkComplete { get; set; }
        public bool IsMilestone { get; set; }
        public bool IsCritical { get; set; }
        public bool IsActive { get; set; }
        public bool IsManual { get; set; }
        public string? Priority { get; set; }
        public string? Notes { get; set; }

        public List<TimephasedDataDto> TimephasedData { get; set; } = new();
        public List<TaskBaselineDto> Baselines { get; set; } = new();
        public List<ExtendedAttributeDto> ExtendedAttributes { get; set; } = new();
    }

    public class ResourceDto
    {
        public int Id { get; set; }
        public int UniqueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Initials { get; set; }
        public string? Group { get; set; }
        public string? EmailAddress { get; set; }
        public string? Code { get; set; }
        public string? Notes { get; set; }
        public bool IsGeneric { get; set; }
        public bool IsCostResource { get; set; }
        public string? MaterialLabel { get; set; }

        public double MaxUnits { get; set; }
        public double PeakUnits { get; set; }
        public double StandardRate { get; set; }
        public double OvertimeRate { get; set; }
        public double CostPerUse { get; set; }
        public string? AccrueAt { get; set; }

        public double Work { get; set; }
        public double ActualWork { get; set; }
        public double RemainingWork { get; set; }
        public double OvertimeWork { get; set; }
        public double Cost { get; set; }
        public double ActualCost { get; set; }
        public double RemainingCost { get; set; }

        public List<ExtendedAttributeDto> ExtendedAttributes { get; set; } = new();
    }

    public class AssignmentDto
    {
        public int UniqueId { get; set; }
        public int TaskUniqueId { get; set; }
        public int ResourceUniqueId { get; set; }
        public double Units { get; set; }

        public DateTime? Start { get; set; }
        public DateTime? Finish { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualFinish { get; set; }

        public double Work { get; set; }
        public double ActualWork { get; set; }
        public double RemainingWork { get; set; }
        public double OvertimeWork { get; set; }
        public double Cost { get; set; }
        public double ActualCost { get; set; }
        public double RemainingCost { get; set; }
        public double PercentWorkComplete { get; set; }
        public string? Notes { get; set; }

        public List<TimephasedDataDto> TimephasedData { get; set; } = new();
    }

    public class CalendarDto
    {
        public int UniqueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsBaseCalendar { get; set; }
        public string? BaseCalendarName { get; set; }
        public List<WeekDayDto> WeekDays { get; set; } = new();
        public List<CalendarExceptionDto> Exceptions { get; set; } = new();
    }

    public class WeekDayDto
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public bool IsWorking { get; set; }
        public double WorkingHours { get; set; }
        public List<WorkingTimeDto> WorkingTimes { get; set; } = new();
    }

    public class WorkingTimeDto
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }

    public class CalendarExceptionDto
    {
        public string? Name { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsWorking { get; set; }
        public List<WorkingTimeDto> WorkingTimes { get; set; } = new();
    }

    public class TaskLinkDto
    {
        public int PredecessorUniqueId { get; set; }
        public int SuccessorUniqueId { get; set; }
        public string LinkType { get; set; } = string.Empty;
        public string? Lag { get; set; }
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

    public class ExtendedAttributeDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
