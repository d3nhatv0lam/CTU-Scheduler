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
    public static class RawRegistrationExtension
    {
        public static RegistrationInformation ToRegistrationInformation(this DkmhQddkCrawlerPayload dkmhQddkCrawlerPayload,string userKey, string userUnit)
        {
            try
            {
                RegistrationInformation info = new RegistrationInformation();
                
                int maxCreditPerSemester = 99;
                string period = string.Empty;
                List<GroupItem> groups = null!;

                Parallel.Invoke(
                    () => maxCreditPerSemester = GetMaxCreditPerSemester(dkmhQddkCrawlerPayload),
                    () => period = GetPeriod(dkmhQddkCrawlerPayload),
                    () => groups = GetGroups(dkmhQddkCrawlerPayload)
                );

                info.AcademicYear = dkmhQddkCrawlerPayload.NamHoc;
                info.Semester = dkmhQddkCrawlerPayload.HocKy;
                info.MaxCreditPerSemester = maxCreditPerSemester;
                info.Period = period;
                info.Groups = groups;

                if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(userUnit))
                    return info;
                
                string userGroup = FindGroupByUnit(info.Groups, userUnit);
                info.UserPeriod = GetUserPeriod(dkmhQddkCrawlerPayload, userKey,userGroup);
                return info;
            }
            catch
            {
                return null!;
            }
        }

        private static int GetMaxCreditPerSemester(DkmhQddkCrawlerPayload dkmhQddkCrawlerPayload)
        {
            int maxCreditPerSemester = 99;
            foreach(var quyDinh in dkmhQddkCrawlerPayload.DanhSachQuyDinh)
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


        private static string GetPeriod(DkmhQddkCrawlerPayload dkmhQddkCrawlerPayload)
        {
            foreach (var quyDinh in dkmhQddkCrawlerPayload.DanhSachQuyDinh)
            {
                if (quyDinh.CotTrai.Any(leftData => leftData.NoiDung.Contains("Thời gian đăng ký"))) 
                {
                    return quyDinh.CotTrai.Last().NoiDung;
                }
            }
            return string.Empty;
        }

        private static List<GroupItem> GetGroups(DkmhQddkCrawlerPayload dkmhQddkCrawlerPayload)
        {
            List<GroupItem> groups = new List<GroupItem>();
            foreach (var quyDinh in dkmhQddkCrawlerPayload.DanhSachQuyDinh)
            {
                if (quyDinh.CotPhai.Count > 0 && quyDinh.CotPhai.First().NoiDung.StartsWith("Nhóm"))
                {
                    foreach(var rightData in quyDinh.CotPhai)
                    {
                        GroupItem group = new GroupItem();
                        group.Title = rightData.CacDiemLuuY.First();
                        group.Value = Regex.Replace(rightData.NoiDung, @"Nhóm \d+: ?", "");
                        groups.Add(group);
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
                if (NormalizeString(item.Value).Contains(unitNormalized))
                {
                    try 
                    {
                        Match match = Regex.Match(item.Title, @"\d+");
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

        private static List<PeriodItem> GetUserPeriod(DkmhQddkCrawlerPayload dkmhQddkCrawlerPayload, string userKey, string group)
        {
            List<PeriodItem> userPeriods = new List<PeriodItem>();
            // empty check
            if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(group))
                return userPeriods;

            userKey = Regex.Match(userKey, @"Khóa \d+").Value;
            int userKeyInt = int.Parse(Regex.Match(userKey, @"\d+").Value);

            int i = 0;
            while(i < dkmhQddkCrawlerPayload.DanhSachThoiGianDangKy.Count)
            {
                IReadOnlyList<RawThoiGianDangKyItem> firstRow = dkmhQddkCrawlerPayload.DanhSachThoiGianDangKy[i];
                try
                {
                    var rowSpan = int.Parse(firstRow.First().RowSpan);

                    if (i == 0 && userKeyInt > int.Parse(Regex.Match(firstRow[1].TieuDe, @"\d+").Value) 
                        || !firstRow[1].TieuDe.Contains(userKey))
                    {
                        i += rowSpan;
                        continue;
                    }

                    PeriodItem period = new PeriodItem() { Key = userKey};

                    for (int j = i; j < i + rowSpan; j++)
                    {
                        var row = dkmhQddkCrawlerPayload.DanhSachThoiGianDangKy[j];
                        if (row.Last().TieuDe.Contains(group))
                        {
                            period.StartDate = row[^3].TieuDe;
                            period.EndDate = row[^2].TieuDe;
                            period.Group = row[^1].TieuDe;
                        }
                    }
                    userPeriods.Add(period);
                    break;
                }
                catch
                {
                    i++;
                }
            }
            return userPeriods;
        }
    }
}
