using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CTUScheduler.Core.Helpers;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions;

public static partial class QddkPayloadExtensions
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"Khóa \d+")]
    private static partial Regex KhoaRegex();

    public static RegistrationInformation ToRegistrationInformation(this RawQddkPayload rawQddkPayload,
        string userKey, string userUnit, ILogger? logger = null)
    {
        logger?.LogDebug("Starting to parse registration information for Academic Year {Year}, Semester {Semester}",
            rawQddkPayload.NamHoc, rawQddkPayload.HocKy);

        try
        {
            int? maxCreditPerSemester = GetMaxCreditPerSemester(rawQddkPayload, logger);
            string? period = GetPeriod(rawQddkPayload);
            List<GroupItem> groups = GetGroups(rawQddkPayload);

            if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(userUnit))
            {
                logger?.LogWarning("User key or unit is empty. Skipping user-specific period parsing.");
                return new RegistrationInformation(
                    rawQddkPayload.NamHoc == 0 ? null : rawQddkPayload.NamHoc,
                    rawQddkPayload.HocKy,
                    maxCreditPerSemester,
                    period,
                    groups,
                    null
                );
            }

            string userGroup = FindGroupByUnit(groups, userUnit);
            if (string.IsNullOrEmpty(userGroup))
            {
                logger?.LogWarning("Could not find a matching registration group for unit: {Unit}", userUnit);
            }

            var userPeriod = GetUserPeriod(rawQddkPayload, userKey, userGroup, groups, logger);

            return new RegistrationInformation(
                rawQddkPayload.NamHoc == 0 ? null : rawQddkPayload.NamHoc,
                rawQddkPayload.HocKy,
                maxCreditPerSemester,
                period,
                groups,
                userPeriod
            );
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Critical error while parsing RegistrationInformation.");
            return new RegistrationInformation(
                rawQddkPayload.NamHoc == 0 ? null : rawQddkPayload.NamHoc,
                rawQddkPayload.HocKy,
                null,
                null,
                new List<GroupItem>(),
                null
            );
        }
    }

    private static int? GetMaxCreditPerSemester(RawQddkPayload rawQddkPayload, ILogger? logger)
    {
        var quyDinh = rawQddkPayload.DanhSachQuyDinh
            .FirstOrDefault(q => q.CotTrai.Any(l => l.NoiDung.Contains("tín chỉ")));

        if (quyDinh == null)
        {
            logger?.LogWarning("Could not find Max Credit node in registration rules.");
            return null;
        }

        var rightData = quyDinh.CotPhai.FirstOrDefault();
        var valStr = rightData?.CacDiemLuuY.FirstOrDefault();

        if (int.TryParse(valStr, out var result)) return result;

        logger?.LogWarning("Failed to parse Max Credit value: {Value}", valStr);
        return null;
    }

    private static string? GetPeriod(RawQddkPayload rawQddkPayload)
    {
        var quyDinh = rawQddkPayload.DanhSachQuyDinh
            .LastOrDefault(q => q.CotTrai.Any(l => l.NoiDung.Contains("Thời gian đăng ký")));

        return quyDinh?.CotTrai.LastOrDefault()?.NoiDung;
    }

    private static List<GroupItem> GetGroups(RawQddkPayload rawQddkPayload)
    {
        var groups = new List<GroupItem>();
        foreach (var quyDinh in rawQddkPayload.DanhSachQuyDinh)
        {
            var firstRight = quyDinh.CotPhai.FirstOrDefault();
            if (firstRight != null && firstRight.NoiDung.StartsWith("Nhóm"))
            {
                foreach (var rightData in quyDinh.CotPhai)
                {
                    var name = rightData.CacDiemLuuY.FirstOrDefault() ?? string.Empty;
                    var description = Regex.Replace(rightData.NoiDung, @"Nhóm \d+: ?", "");
                    groups.Add(new GroupItem(name, description));
                }
            }
        }

        return groups;
    }

    private static string FindGroupByUnit(List<GroupItem> groups, string unit)
    {
        string unitNormalized = StringHelper.NormalizeString(unit);
        foreach (var item in groups)
        {
            if (StringHelper.NormalizeString(item.Description).Contains(unitNormalized))
            {
                var match = NumberRegex().Match(item.Name);
                if (match.Success) return match.Value;
            }
        }

        return string.Empty;
    }

    private static UserPeriodItem? GetUserPeriod(RawQddkPayload rawQddkPayload, string userKey, string group,
        List<GroupItem> groups, ILogger? logger)
    {
        if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(group))
            return null;

        // Kiểm tra userKey đã truyêền có đúng dạng "Khóa {number}"
        var matchKhoa = KhoaRegex().Match(userKey);
        if (!matchKhoa.Success) return null;

        // Thử lấy số ra từ matchKhoa
        var matchYear = NumberRegex().Match(matchKhoa.Value);
        if (!matchYear.Success || !int.TryParse(matchYear.Value, out int studentCohort)) return null;

        int rowIndex = 0;
        var scheduleTable = rawQddkPayload.DanhSachThoiGianDangKy;

        while (rowIndex < scheduleTable.Count)
        {
            // blockHeaderRow: Dòng đầu tiên của một cụm (Chứa thông tin Khóa và RowSpan)
            var blockHeaderRow = scheduleTable[rowIndex];

            // cellRowSpan: Ô chứa giá trị RowSpan (thường là ô đầu tiên của dòng đầu cụm)
            var cellRowSpan = blockHeaderRow.FirstOrDefault();
            if (cellRowSpan is null || !int.TryParse(cellRowSpan.RowSpan, out int rowsInBlock))
            {
                rowIndex++;
                continue;
            }

            try
            {
                // cellCohortTitle: Ô chứa tiêu đề Khóa (Ví dụ: "Khóa 48 trở về trước")
                var cellCohortTitle = blockHeaderRow.Count > 1 ? blockHeaderRow[1] : null;
                if (cellCohortTitle == null)
                {
                    rowIndex += rowsInBlock;
                    continue;
                }

                // khóa của sinh viên trên hệ thống dkmh
                var titleText = cellCohortTitle.TieuDe;
                var titleYearMatch = NumberRegex().Match(titleText);

                // 2. Kiểm tra xem sinh viên có thuộc Khóa của cụm này không
                bool isCohortMatched;
                if (titleYearMatch.Success && int.TryParse(titleYearMatch.Value, out int cohortFromTitle))
                {
                    if (titleText.Contains("trở về trước") || titleText.Contains("trở xuống"))
                        isCohortMatched = studentCohort <= cohortFromTitle;
                    else if (titleText.Contains("trở về sau") || titleText.Contains("trở lên"))
                        isCohortMatched = studentCohort >= cohortFromTitle;
                    else
                        isCohortMatched = studentCohort == cohortFromTitle;
                }
                else
                {
                    isCohortMatched = titleText.Contains($"Khóa {studentCohort}");
                }

                if (!isCohortMatched)
                {
                    // Nếu không đúng Khóa, nhảy qua toàn bộ số dòng của cụm này
                    rowIndex += rowsInBlock;
                    continue;
                }

                // 3. Nếu đúng Khóa, quét các dòng con bên trong cụm để tìm đúng Nhóm (Group)
                for (int subRowIndex = rowIndex; subRowIndex < rowIndex + rowsInBlock; subRowIndex++)
                {
                    var groupRow = scheduleTable[subRowIndex];
                    var groupCellText = groupRow.LastOrDefault()?.TieuDe ?? string.Empty;

                    var allowedGroups = NumberRegex().Matches(groupCellText)
                        .Select(m => int.Parse(m.Value))
                        .ToList();

                    // Kiểm tra xem nhóm của sinh viên có nằm trong danh sách nhóm của dòng này không
                    if (int.TryParse(group, out int studentGroup) && allowedGroups.Contains(studentGroup))
                    {
                        // Lấy ngày bắt đầu và kết thúc (thường nằm ở 2 ô cuối trước ô nhóm)
                        var startDateStr = groupRow.Count >= 3 ? groupRow[^3].TieuDe : string.Empty;
                        var endDateStr = groupRow.Count >= 2 ? groupRow[^2].TieuDe : string.Empty;

                        var startDate = ParseDateTime(startDateStr);
                        var endDate = ParseDateTime(endDateStr);

                        if (startDate == null || endDate == null)
                        {
                            logger?.LogWarning(
                                "Failed to parse registration dates for group {Group}. Start: {Start}, End: {End}",
                                group, startDateStr, endDateStr);
                        }

                        var groupDescriptions = groups
                            .Where(g => allowedGroups.Any(id => g.Name.Contains(id.ToString())))
                            .Select(g => g.Description)
                            .Distinct()
                            .ToList();

                        var finalDescription = string.Join(". ", groupDescriptions
                            .Select(d => d.Trim())
                            .Select(d => string.IsNullOrEmpty(d) ? d : char.ToUpper(d[0]) + d[1..]));

                        return new UserPeriodItem(
                            $"Khóa {studentCohort}",
                            startDate,
                            endDate,
                            allowedGroups,
                            finalDescription
                        );
                    }
                }

                rowIndex += rowsInBlock;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Lỗi khi xử lý cụm dòng tại vị trí {Index}", rowIndex);
                rowIndex++;
            }
        }

        return null;
    }

    private static DateTime? ParseDateTime(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        // Xử lý định dạng: "07:30 ngày 06/04/2026"
        // Thay thế " ngày " thành " " để parse dễ hơn
        string normalized = input.Replace(" ngày ", " ").Trim();

        // Thử parse các định dạng phổ biến
        string[] formats = { "HH:mm dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy" };

        if (DateTime.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var dt))
            return dt;

        if (DateTime.TryParse(normalized, out dt))
            return dt;

        return null;
    }
}