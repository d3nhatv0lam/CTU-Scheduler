using Avalonia.Input;
using CTUScheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Models
{
    public class ScheduleCell: ITableCell
    {
        public int Row
        {
            get
            {
                // TietBatDau  <=> row index difference 1 when TietBatDau < 5
                if (StartPeriod < 5) return StartPeriod - 1;
                return StartPeriod;
            }
        }
        /// <summary>
        /// ThuDiHoc <=> Column index difference 2
        /// </summary>
        public int Column => AttendingDay - 2;

        /// <summary>
        /// Số tiết học
        /// </summary>
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;

        public string CourseCode { get; set; }
        public string CourseName_VN { get; set; }
        public string Group { get; set; }
        public int TotalStudents { get; set; }
        public int RemainingStudents { get; set; }
        public string Room { get; set; }
        public int AttendingDay { get; set; }
        public int StartPeriod { get; set; }
        public int NumberOfPeriods { get; set; }
        public string Lecturer { get; set; }
        public int Credit { get; set; }
    }
}
