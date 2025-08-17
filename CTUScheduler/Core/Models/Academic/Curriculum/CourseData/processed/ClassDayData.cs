using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed
{
    public class ClassDayData
    {
        /// <summary>
        /// từ thứ 2 -> 7
        /// <br></br>
        /// Value = [2, 3, .., 7]
        /// </summary>
        public int AttendingDay { get; set; }
        public string Period { get; set; }
        public string Room { get; set; }
    }
}
