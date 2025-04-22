using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.processed
{
    public class CourseData
    {
        public  string Group { get; set; }
        public string Lecturer { get; set; }
        public string LecturerEmail { get; set; }
        public int TotalStudents { get; set; }
        public int RemainingStudents { get; set; }
        public List<ClassDayData> ClassDayDatas { get; set; } = new List<ClassDayData>();
    }
}
