using System;

namespace CTUScheduler.Core.Models.Timetable;

public record AvoidanceSlot(DayOfWeek? Day = null, TimeOfDay? Time = null);