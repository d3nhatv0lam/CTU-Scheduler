using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.Registration;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions
{
    public static class RawRegistrationExtension
    {
        public static RegistrationInformation ToRegistrationInformation(this RawRegistrationInformation rawRegistrationInformation,string userKey, string userUnit)
        {
            try
            {
                RegistrationInformation info = new RegistrationInformation();
                
                int maxCreditPerSemester = 99;
                string period = string.Empty;
                List<GroupItem> groups = null!;

                Parallel.Invoke(
                    () => maxCreditPerSemester = GetMaxCreditPerSemester(rawRegistrationInformation),
                    () => period = GetPeriod(rawRegistrationInformation),
                    () => groups = GetGroups(rawRegistrationInformation)
                );

                info.AcademicYear = rawRegistrationInformation.namhoc;
                info.Semester = rawRegistrationInformation.hocky;
                info.MaxCreditPerSemester = maxCreditPerSemester;
                info.Period = period;
                info.Groups = groups;

                if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(userUnit))
                    return info;
                
                string userGroup = FindGroupByUnit(info.Groups, userUnit);
                info.UserPeriod = GetUserPeriod(rawRegistrationInformation, userKey,userGroup);
                return info;
            }
            catch
            {
                return null!;
            }
        }

        private static int GetMaxCreditPerSemester(RawRegistrationInformation rawRegistrationInformation)
        {
            int maxCreditPerSemester = 99;
            foreach(var quyDinh in rawRegistrationInformation.quyDinh)
            {
                foreach (var leftData in quyDinh.leftData)
                {
                    if (leftData.value.Contains("tín chỉ"))
                    {
                        foreach (var rightData in quyDinh.rightData)
                        {
                            if (int.TryParse(rightData.important.First(), out maxCreditPerSemester))
                                return maxCreditPerSemester;
                        }
                    }
                }
            }
            return maxCreditPerSemester;
        }


        private static string GetPeriod(RawRegistrationInformation rawRegistrationInformation)
        {
            foreach (var quyDinh in rawRegistrationInformation.quyDinh)
            {
                if (quyDinh.leftData.Any(leftData => leftData.value.Contains("Thời gian đăng ký"))) 
                {
                    return quyDinh.leftData.Last().value;
                }
            }
            return string.Empty;
        }

        private static List<GroupItem> GetGroups(RawRegistrationInformation rawRegistrationInformation)
        {
            List<GroupItem> groups = new List<GroupItem>();
            foreach (var quyDinh in rawRegistrationInformation.quyDinh)
            {
                if (quyDinh.rightData.Count > 0 && quyDinh.rightData.First().value.StartsWith("Nhóm"))
                {
                    foreach(var rightData in quyDinh.rightData)
                    {
                        GroupItem group = new GroupItem();
                        group.Title = rightData.important.First();
                        group.Value = Regex.Replace(rightData.value, @"Nhóm \d+: ?", "");
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

        private static List<PeriodItem> GetUserPeriod(RawRegistrationInformation rawRegistrationInformation, string userKey, string group)
        {
            List<PeriodItem> userPeriods = new List<PeriodItem>();
            // empty check
            if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(group))
                return userPeriods;

            userKey = Regex.Match(userKey, @"Khóa \d+").Value;
            int userKeyInt = int.Parse(Regex.Match(userKey, @"\d+").Value);

            int i = 0;
            while(i < rawRegistrationInformation.thoiGianDangKy.Count)
            {
                List<RawThoiGianDangKyItem> firstRow = rawRegistrationInformation.thoiGianDangKy[i];
                try
                {
                    var rowSpan = int.Parse(firstRow.First().rowspan);

                    if (i == 0 && userKeyInt > int.Parse(Regex.Match(firstRow[1].title, @"\d+").Value) 
                        || !firstRow[1].title.Contains(userKey))
                    {
                        i += rowSpan;
                        continue;
                    }

                    PeriodItem period = new PeriodItem() { Key = userKey};

                    for (int j = i; j < i + rowSpan; j++)
                    {
                        var row = rawRegistrationInformation.thoiGianDangKy[j];
                        if (row.Last().title.Contains(group))
                        {
                            period.StartDate = row[^3].title;
                            period.EndDate = row[^2].title;
                            period.Group = row[^1].title;
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
