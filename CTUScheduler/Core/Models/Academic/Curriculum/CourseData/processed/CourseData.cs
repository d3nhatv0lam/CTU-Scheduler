using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed
{
    public class CourseData
    {
        public int Key { get; set; }
        public string Code { get; set; }
        public  string Group { get; set; }
        public string Lecturer { get; set; }
        public string LecturerEmail { get; set; }
        public int TotalStudents { get; set; }
        public int RemainingStudents { get; set; }
        public List<ClassDayData> ClassDayDatas { get; set; } = new List<ClassDayData>();

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder($"Nhóm:{Group} - {Lecturer} - {RemainingStudents}/{TotalStudents}\n");
            strBuilder.Append("ClassDayDatas:\n");

            foreach (var classDayData in ClassDayDatas)
            {
                strBuilder.Append($"AttendingDay: {classDayData.AttendingDay} {classDayData.Period} {classDayData.Room} \n");
            }

            return strBuilder.ToString();
        }
    }
}
