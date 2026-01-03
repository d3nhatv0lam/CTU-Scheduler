using System;

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
        public int StartPeriod()
        {
            if (string.IsNullOrEmpty(Period)) return 0;
            var span = Period.AsSpan().TrimStart('-');
            return span.Length > 0 ? span[0] - '0' : 0;
        }

        /// <summary>
        /// Số tiết học
        /// </summary>
        /// <returns></returns>
        public int PeriodCount()
        {
            if (string.IsNullOrEmpty(Period)) return 0;
            var span = Period.AsSpan().TrimEnd('-');
            return span.Length > 0 ? span[^1] - '0' : 0;
        }

        /// <summary>
        /// Tiết kết thúc
        /// </summary>
        /// <returns></returns>
        public int EndPeriod()
        {
            if (string.IsNullOrEmpty(Period)) return 0;
            var span = Period.AsSpan().Trim('-');
            return span.Length;
        }
    }
}
