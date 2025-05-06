using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Converter
{
    public static class RegistrationConverter
    {
        public static RegistrationInformation ToRegistrationInformation(this RawRegistrationInformation rawRegistrationInformation,string userKey, string userUnit)
        {
            RegistrationInformation info = new RegistrationInformation();
            info.AcademicYear = rawRegistrationInformation.namhoc;
            info.Semester = rawRegistrationInformation.hocky;
            info.MaxCreditPerSemester = GetMaxCreditPerSemester(rawRegistrationInformation);
            info.Period = GetPeriod(rawRegistrationInformation);
            info.Groups = GetGroups(rawRegistrationInformation);
            info.UserPeriod = GetUserPeriod(rawRegistrationInformation,userKey,FindGroupByUnit(info.Groups,userUnit));
            return info;
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
                            if (Int32.TryParse(rightData.important.First(), out maxCreditPerSemester))
                                return maxCreditPerSemester;
                        }
                    }
                }
            }
            return maxCreditPerSemester;
        }


        private static string GetPeriod(RawRegistrationInformation rawRegistrationInformation)
        {
            string period = "";
            foreach (var quyDinh in rawRegistrationInformation.quyDinh)
            {
                foreach (var leftData in quyDinh.leftData)
                {
                    if (leftData.value.Contains("Thời gian đăng ký"))
                        return quyDinh.leftData.Last().value;
                }
            }
            return period;
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

                    }
                }
            }

            string NormalizeString(string input)
            {
                var sb = new StringBuilder(input.Length);

                foreach (var c in input.Normalize(NormalizationForm.FormD))
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (unicodeCategory != UnicodeCategory.NonSpacingMark && Char.IsLetterOrDigit(c))
                    {
                        sb.Append(char.ToLowerInvariant(c));
                    }
                }
                return sb.ToString();
            }
            return group;
        }

        private static List<PeriodItem> GetUserPeriod(RawRegistrationInformation rawRegistrationInformation, string userKey, string group)
        {
            List<PeriodItem> userPeriods = new List<PeriodItem>();

            userKey = Regex.Match(userKey, @"Khóa \d+").Value;
            int userKeyInt = Int32.Parse(Regex.Match(userKey, @"\d+").Value);

            int i = 0;
            while(i < rawRegistrationInformation.thoiGianDangKy.Count)
            {
                List<RawThoiGianDangKyItem> firstRow = rawRegistrationInformation.thoiGianDangKy[i];
                try
                {
                    var rowSpan = Int32.Parse(firstRow.First().rowspan);

                    if ((i == 0 && userKeyInt > Int32.Parse(Regex.Match(firstRow[1].title, @"\d+").Value)) 
                        || !firstRow[1].title.Contains(userKey))
                    {
                        i += rowSpan;
                        continue;
                    }

                    PeriodItem period = new PeriodItem();

                    period.Key = userKey;
                    period.Group = group;

                    for (int j = i; j < i + rowSpan; j++)
                    {
                        List<RawThoiGianDangKyItem> row = rawRegistrationInformation.thoiGianDangKy[j];

                        if (!row.Last().title.Contains(group))
                            continue;

                        period.StartDate = row[^3].title;
                        period.EndDate = row[^2].title;
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
