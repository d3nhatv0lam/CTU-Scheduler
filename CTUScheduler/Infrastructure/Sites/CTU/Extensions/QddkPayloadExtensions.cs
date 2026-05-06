using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions
{
    public static class QddkPayloadExtensions
    {
        public static RegistrationInformation ToRegistrationInformation(this RawQddkPayload rawQddkPayload, string userKey, string userUnit)
        {
            try
            {
                int maxCreditPerSemester = 99;
                string period = string.Empty;
                List<GroupItem> groups = null!;

                Parallel.Invoke(
                    () => maxCreditPerSemester = GetMaxCreditPerSemester(rawQddkPayload),
                    () => period = GetPeriod(rawQddkPayload),
                    () => groups = GetGroups(rawQddkPayload)
                );

                if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(userUnit))
                {
                    return new RegistrationInformation(
                        rawQddkPayload.NamHoc,
                        rawQddkPayload.HocKy,
                        maxCreditPerSemester,
                        period,
                        groups,
                        null
                    );
                }

                string userGroup = FindGroupByUnit(groups, userUnit);
                var userPeriod = GetUserPeriod(rawQddkPayload, userKey, userGroup, groups);

                return new RegistrationInformation(
                    rawQddkPayload.NamHoc,
                    rawQddkPayload.HocKy,
                    maxCreditPerSemester,
                    period,
                    groups,
                    userPeriod
                );
            }
            catch
            {
                return null!;
            }
        }

        private static int GetMaxCreditPerSemester(RawQddkPayload rawQddkPayload)
        {
            int maxCreditPerSemester = 99;
            foreach (var quyDinh in rawQddkPayload.DanhSachQuyDinh)
            {
                foreach (var leftData in quyDinh.CotTrai)
                {
                    if (leftData.NoiDung.Contains("tín chỉ"))
                    {
                        foreach (var rightData in quyDinh.CotPhai)
                        {
                            if (int.TryParse(rightData.CacDiemLuuY.First(), out maxCreditPerSemester))
                                return maxCreditPerSemester;
                        }
                    }
                }
            }
            return maxCreditPerSemester;
        }


        private static string GetPeriod(RawQddkPayload rawQddkPayload)
        {
            foreach (var quyDinh in rawQddkPayload.DanhSachQuyDinh)
            {
                if (quyDinh.CotTrai.Any(leftData => leftData.NoiDung.Contains("Thời gian đăng ký")))
                {
                    return quyDinh.CotTrai.Last().NoiDung;
                }
            }
            return string.Empty;
        }

        private static List<GroupItem> GetGroups(RawQddkPayload rawQddkPayload)
        {
            List<GroupItem> groups = new List<GroupItem>();
            foreach (var quyDinh in rawQddkPayload.DanhSachQuyDinh)
            {
                if (quyDinh.CotPhai.Count > 0 && quyDinh.CotPhai.First().NoiDung.StartsWith("Nhóm"))
                {
                    foreach (var rightData in quyDinh.CotPhai)
                    {
                        var name = rightData.CacDiemLuuY.First();
                        var description = Regex.Replace(rightData.NoiDung, @"Nhóm \d+: ?", "");
                        groups.Add(new GroupItem(name, description));
                    }
                }
            }
            return groups;
        }

        private static string FindGroupByUnit(List<GroupItem> groups, string unit)
        {
            string unitNormalized = NormalizeString(unit);
            string group = string.Empty;
            foreach (var item in groups)
            {
                if (NormalizeString(item.Description).Contains(unitNormalized))
                {
                    try
                    {
                        Match match = Regex.Match(item.Name, @"\d+");
                        group = match.Value;
                        break;
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            return group;

            string NormalizeString(string input)
            {
                var sb = new StringBuilder(input.Length);

                foreach (var c in input.Normalize(NormalizationForm.FormD))
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (unicodeCategory != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                    {
                        sb.Append(char.ToLowerInvariant(c));
                    }
                }
                return sb.ToString();
            }
        }

        private static UserPeriodItem? GetUserPeriod(RawQddkPayload rawQddkPayload, string userKey, string group, List<GroupItem> groups)
        {
            if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(group))
                return null;

            userKey = Regex.Match(userKey, @"Khóa \d+").Value;
            int userKeyInt = int.Parse(Regex.Match(userKey, @"\d+").Value);

            int i = 0;
            while (i < rawQddkPayload.DanhSachThoiGianDangKy.Count)
            {
                IReadOnlyList<RawQddkThoiGianDangKyItem> firstRow = rawQddkPayload.DanhSachThoiGianDangKy[i];
                try
                {
                    var rowSpan = int.Parse(firstRow.First().RowSpan);

                    if (i == 0 && userKeyInt > int.Parse(Regex.Match(firstRow[1].TieuDe, @"\d+").Value)
                        || !firstRow[1].TieuDe.Contains(userKey))
                    {
                        i += rowSpan;
                        continue;
                    }

                    for (int j = i; j < i + rowSpan; j++)
                    {
                        var row = rawQddkPayload.DanhSachThoiGianDangKy[j];
                        var groupStr = row.Last().TieuDe;

                        // Parse groups in row: "1, 3" -> [1, 3]
                        var allowedGroups = groupStr.Split(',', ' ', ';')
                            .Select(s => Regex.Match(s, @"\d+").Value)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(int.Parse)
                            .ToList();

                        if (allowedGroups.Contains(int.Parse(group)))
                        {
                            var startDate = ParseDateTime(row[^3].TieuDe);
                            var endDate = ParseDateTime(row[^2].TieuDe);

                            // Get description for all allowed groups
                            var groupDescriptions = groups
                                .Where(g => allowedGroups.Any(id => g.Name.Contains(id.ToString())))
                                .Select(g => g.Description)
                                .Distinct()
                                .ToList();

                            // Clean and capitalize descriptions
                            var finalDescription = string.Join(". ", groupDescriptions
                                .Select(d => d.Trim())
                                .Select(d => string.IsNullOrEmpty(d) ? d : char.ToUpper(d[0]) + d[1..]));

                            return new UserPeriodItem(
                                userKey,
                                startDate,
                                endDate,
                                allowedGroups,
                                finalDescription
                            );
                        }
                    }
                    break;
                }
                catch
                {
                    i++;
                }
            }
            return null;
        }

        private static DateTime ParseDateTime(string input)
        {
            if (string.IsNullOrEmpty(input)) return DateTime.MinValue;

            // Xử lý định dạng: "07:30 ngày 06/04/2026"
            // Thay thế " ngày " thành " " để parse dễ hơn
            string normalized = input.Replace(" ngày ", " ").Trim();

            // Thử parse các định dạng phổ biến
            string[] formats = { "HH:mm dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy" };
            
            if (DateTime.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            
            if (DateTime.TryParse(normalized, out dt))
                return dt;

            return DateTime.MinValue;
        }
    }
}
