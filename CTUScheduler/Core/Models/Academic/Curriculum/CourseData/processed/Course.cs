using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed
{
    public class Course
    {
        public string Code { get; set; }
        public string Name_VN { get; set; }
        public int Credit { get; set; }
        public int TheorySessions { get; set; }
        public int PracticalSessions { get; set; }
        public ObservableCollection<CourseData> Sections  { get; set; } = new();
    }
}
