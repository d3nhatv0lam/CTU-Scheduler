using System.Collections.Generic;
using System.Text;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData
{
    public class CourseSection
    {
        /// <summary>
        /// Lớp học phần này có bị hủy?
        /// </summary>
        public bool IsCancelled { get; set; }
        public int Key { get; set; }
        public string Code { get; set; }
        public string Group { get; set; }
        public string Lecturer { get; set; } = string.Empty;
        public string LecturerEmail { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int RemainingStudents { get; set; }
        public List<ClassDay> ClassDays { get; set; } = new ();

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder($"Nhóm:{Group} - {Lecturer} - {RemainingStudents}/{TotalStudents}\n");
            strBuilder.Append("ClassDays:\n");

            foreach (var classDayData in ClassDays)
            {
                strBuilder.Append($"AttendingDay: {classDayData.AttendingDay} {classDayData.Period} {classDayData.Room}\n");
            }
            return strBuilder.ToString();
        }
    }
}
