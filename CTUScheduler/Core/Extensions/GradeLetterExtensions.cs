using CTUScheduler.Core.Models.Academic.Grades;

namespace CTUScheduler.Core.Extensions;

public static class GradeLetterExtensions
{
    public static GradeLetter ParseGradeLetter(string? rawString)
    {
        if (string.IsNullOrEmpty(rawString))
        {
            return GradeLetter.Unknown;
        }

        return rawString.Trim().ToUpperInvariant() switch
        {
            "A" => GradeLetter.A,
            "B+" => GradeLetter.BPlus,
            "B" => GradeLetter.B,
            "C+" => GradeLetter.CPlus,
            "C" => GradeLetter.C,
            "D+" => GradeLetter.DPlus,
            "D" => GradeLetter.D,
            "F" => GradeLetter.F,
            "M" => GradeLetter.M,
            "I" => GradeLetter.I,
            _ => GradeLetter.Unknown
        };
    }

    public static bool IsStandardScoredGrade(this GradeLetter letter)
    {
        return letter is >= GradeLetter.A and <= GradeLetter.F;
    }
}
