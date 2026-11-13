using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;
using CTUScheduler.Benchmarks.Datasets;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Algorithms.Scoring;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args.Contains("--stats"))
        {
            PrintPruningStats();
            return;
        }

        if (args.Length > 0 && args.Contains("--sensitivity"))
        {
            PrintSensitivityAnalysis();
            return;
        }

        BenchmarkRunner.Run<TimetableGeneratorBenchmarks>(args: args);
    }

    public static void PrintPruningStats()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("\n=================================================================================================");
        Console.WriteLine("BẢNG 11a: ĐÁNH GIÁ HIỆU QUẢ CẮT TỈA KHÔNG GIAN TÌM KIẾM TRÊN DỮ LIỆU THỰC TẾ MYCTUS");
        Console.WriteLine("=================================================================================================");
        Console.WriteLine(string.Format("{0,-6} | {1,-15} | {2,-15} | {3,-14} | {4,-15} | {5,-14} | {6,-10}",
            "K.Bản", "Quy mô", "K.Gian L.Thuyết", "Số nút duyệt", "Số nhánh cắt", "TKB hợp lệ", "Tỉ lệ cắt"));
        Console.WriteLine(new string('-', 97));

        var pruningRule = new OverlapPruningRule();

        for (int k = 4; k <= 8; k++)
        {
            var sets = CtuRealCourseDataset.GetCourseSets(k);
            var orderedSets = sets.OrderBy(s => s.Count).ToList();

            long theoretical = 1;
            int totalCredits = 0;
            foreach (var s in orderedSets)
            {
                theoretical *= s.Count;
                totalCredits += s[0].Course.Credits;
            }

            long nodesVisited = 0;
            long branchesPruned = 0;
            int validCount = 0;

            var validSchedules = Combinatorics.CartesianProduct(
                orderedSets,
                isValidCandidate: (currentPath, candidate) =>
                {
                    nodesVisited++;
                    bool canContinue = pruningRule.CanContinue(currentPath, candidate);
                    if (!canContinue)
                    {
                        branchesPruned++;
                        return false;
                    }
                    return true;
                }
            );

            foreach (var _ in validSchedules)
            {
                validCount++;
            }

            double prunePercentage = (1.0 - ((double)nodesVisited / theoretical)) * 100.0;

            Console.WriteLine(string.Format("KB{0:D2}   | {1,-15} | {2,-15:N0} | {3,-14:N0} | {4,-15:N0} | {5,-14:N0} | {6,8:F1}%",
                k - 3, $"{k} môn ({totalCredits} TC)", theoretical, nodesVisited, branchesPruned, validCount, prunePercentage));
        }

        Console.WriteLine("=================================================================================================\n");
    }

    public static void PrintSensitivityAnalysis()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("\n=================================================================================================");
        Console.WriteLine("THỰC NGHIỆM ĐỘ NHẠY CỦA HÀM CHẤM ĐIỂM THEO CÁC HỒ SƠ TRỌNG SỐ (TẬP 5 HỌC PHẦN)");
        Console.WriteLine("=================================================================================================");

        var sets = CtuRealCourseDataset.GetCourseSets(5);
        var orderedSets = sets.OrderBy(s => s.Count).ToList();
        var pruningRule = new OverlapPruningRule();

        var validSchedules = Combinatorics.CartesianProduct(
            orderedSets,
            isValidCandidate: (currentPath, candidate) => pruningRule.CanContinue(currentPath, candidate)
        ).ToList();

        Console.WriteLine($"Số lượng thời khóa biểu hợp lệ sinh ra: {validSchedules.Count} phương án.");

        void EvaluateProfile(string profileName, IScheduleScorer[] scorers)
        {
            Console.WriteLine($"\n--- HỒ SƠ: {profileName} ---");
            var scoredList = validSchedules.Select(schedule =>
            {
                double total = 0;
                double sumW = 0;
                foreach (var sc in scorers)
                {
                    total += sc.CalculateScore(schedule) * sc.Weight;
                    sumW += sc.Weight;
                }
                double finalScore = sumW > 0 ? total / sumW : 0;
                return (Schedule: schedule, Score: finalScore);
            }).OrderByDescending(x => x.Score).ToList();

            var top1 = scoredList.First();
            var days = top1.Schedule.SelectMany(s => s.Section.ClassDays).Select(d => d.AttendingDay).Distinct().OrderBy(x => x).ToList();

            var compactScorer = new CompactDaysScorer();
            var morningScorer = new TimeOfDayScorer(TimeOfDay.Morning);
            var afternoonScorer = new TimeOfDayScorer(TimeOfDay.Afternoon);
            var gapsScorer = new MinimizeGapsScorer();
            var balanceScorer = new BalancedWorkloadScorer();

            Console.WriteLine($"Điểm tổng hợp Top-1: {top1.Score:F3}");
            Console.WriteLine($"Số ngày có lịch: {days.Count} ngày (Thứ: {string.Join(", ", days)})");
            Console.WriteLine($"Điểm f_compact: {compactScorer.CalculateScore(top1.Schedule):F3}");
            Console.WriteLine($"Điểm f_morning: {morningScorer.CalculateScore(top1.Schedule):F3}");
            Console.WriteLine($"Điểm f_afternoon: {afternoonScorer.CalculateScore(top1.Schedule):F3}");
            Console.WriteLine($"Điểm f_gaps: {gapsScorer.CalculateScore(top1.Schedule):F3}");
            Console.WriteLine($"Điểm f_balance: {balanceScorer.CalculateScore(top1.Schedule):F3}");
            Console.WriteLine("Các nhóm lớp trong phương án Top-1:");
            foreach (var s in top1.Schedule)
            {
                var timeInfo = string.Join("; ", s.Section.ClassDays.Select(d => $"Thứ {d.AttendingDay} tiết {d.StartPeriod}-{d.EndPeriod}"));
                Console.WriteLine($"  * {s.Course.Code} ({s.Course.Name_VN}) - Nhóm {s.Section.Group}: {timeInfo}");
            }
        }

        EvaluateProfile("Hồ sơ A (Ưu tiên gom ngày: w_compact = 1.0)", new IScheduleScorer[] { new CompactDaysScorer(1.0) });
        EvaluateProfile("Hồ sơ B (Ưu tiên học ca sáng: w_morning = 1.0)", new IScheduleScorer[] { new TimeOfDayScorer(TimeOfDay.Morning, 1.0) });
        EvaluateProfile("Hồ sơ C (Ưu tiên học ca chiều: w_afternoon = 1.0)", new IScheduleScorer[] { new TimeOfDayScorer(TimeOfDay.Afternoon, 1.0) });
        EvaluateProfile("Hồ sơ D (Ưu tiên liền mạch / giảm tiết trống: w_gaps = 1.0)", new IScheduleScorer[] { new MinimizeGapsScorer(1.0) });
        EvaluateProfile("Hồ sơ E (Ưu tiên cân bằng tải giữa các ngày: w_balance = 1.0)", new IScheduleScorer[] { new BalancedWorkloadScorer(1.0) });

        Console.WriteLine("\n=================================================================================================\n");
    }
}
