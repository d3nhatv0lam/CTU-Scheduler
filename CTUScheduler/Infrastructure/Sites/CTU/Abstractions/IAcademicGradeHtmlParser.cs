// Created: 2026-11-28
// Implemented by AI with author's review.

using System;
using CTUScheduler.Core.Models.Academic.Grades;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IAcademicGradeHtmlParser
{
    GradeTranscript Parse(string html, DateTimeOffset fetchedAt);
}
