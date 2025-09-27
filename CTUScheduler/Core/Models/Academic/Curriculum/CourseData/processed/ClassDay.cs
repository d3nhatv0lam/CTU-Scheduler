using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed
{
    public class ClassDay
    {
        /// <summary>
        /// từ thứ 2 -> 7
        /// <br></br>
        /// Value = [2, 3, .., 7]
        /// </summary>
        public int AttendingDay { get; set; }
        public string Period { get; set; }
        public string Room { get; set; }

        /// <summary>
        /// Tiết bắt đầu
        /// </summary>
        /// <returns></returns>
        public int StartPeriod() => Period.Trim('-').First() - '0';
        /// <summary>
        /// Số tiết học
        /// </summary>
        /// <returns></returns>
        public int PeriodCount() => Period.Trim('-').Length;
        /// <summary>
        /// Tiết kết thúc
        /// </summary>
        /// <returns></returns>
        public int EndPeriod() => Period.Trim('-').Last() - '0';
    }
}
