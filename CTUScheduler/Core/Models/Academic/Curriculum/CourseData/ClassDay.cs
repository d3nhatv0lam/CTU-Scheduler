using System;

namespace CTUScheduler.Core.Models.Academic.Curriculum.CourseData
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
        /// Chuyển đổi số Thứ (int) sang DayOfWeek 
        /// </summary>
        public DayOfWeek DayOfWeek => AttendingDay switch
        {
            2 => System.DayOfWeek.Monday,
            3 => System.DayOfWeek.Tuesday,
            4 => System.DayOfWeek.Wednesday,
            5 => System.DayOfWeek.Thursday,
            6 => System.DayOfWeek.Friday,
            7 => System.DayOfWeek.Saturday,
            8 or 1 => System.DayOfWeek.Sunday, // Hỗ trợ cả 1 hoặc 8 cho Chủ nhật
            _ => throw new ArgumentOutOfRangeException(nameof(AttendingDay),
                $"Giá trị {AttendingDay} không hợp lệ cho một ngày trong tuần.")
        };


        /// <summary>
        /// Tiết bắt đầu
        /// </summary>
        /// <returns></returns>
        public int StartPeriod
        {
            get
            {
                if (string.IsNullOrEmpty(Period)) return 0;
                var span = Period.AsSpan().TrimStart('-');
                return span.Length > 0 ? span[0] - '0' : 0;
            }
        }


        ///  /// <summary>
        /// Tiết kết thúc
        /// </summary>
        /// <returns></returns>
        public int EndPeriod
        {
            get
            {
                if (string.IsNullOrEmpty(Period)) return 0;
                var span = Period.AsSpan().TrimEnd('-');
                return span.Length > 0 ? span[^1] - '0' : 0;
            }
        }

        /// <summary>
        /// Số tiết học
        /// </summary>
        /// <returns></returns>
        public int PeriodCount
        {
            get
            {
                if (string.IsNullOrEmpty(Period)) return 0;
                var span = Period.AsSpan().Trim('-');
                return span.Length;
            }
        }
    }
}