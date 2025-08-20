using Avalonia.Input;
using CTUScheduler.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Schedule
{
    public class ScheduleCell: ITableCell
    {
        private static readonly int DEFAULT_ATTENDING_DAY = 2;
        private static readonly int DEFAULT_START_PERIOD = 1;
        private static readonly int DEFAULT_NUMBER_OF_PERIODS = 1;
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
        public int RowSpan => NumberOfPeriods;
        public int ColumnSpan { get; set; } = 1;

        public string CourseCode { get; set; }
        public string CourseName_VN { get; set; }
        public string Group { get; set; }
        public int TotalStudents { get; set; }
        public int RemainingStudents { get; set; }
        public string Room { get; set; }
        public int AttendingDay { get; set; } = DEFAULT_ATTENDING_DAY;
        public int StartPeriod { get; set; } = DEFAULT_START_PERIOD;
        public int NumberOfPeriods { get; set; } = DEFAULT_NUMBER_OF_PERIODS;
        public string Lecturer { get; set; }
        public int Credit { get; set; }
        
    }
}
