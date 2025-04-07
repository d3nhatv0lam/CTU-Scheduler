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
        [JsonIgnore]
        public int Row
        {
            get
            {
                // TietBatDau  <=> row index difference 1 when TietBatDau < 5
                if (TietBatDau < 5) return TietBatDau - 1;
                return TietBatDau;
            }
        }

        /// <summary>
        /// ThuDiHoc <=> Column index difference 2
        /// </summary>
        [JsonIgnore]
        public int Column => ThuDihoc - 2;

        /// <summary>
        /// Số tiết học
        /// </summary>
        [JsonIgnore]
        public int RowSpan { get; set; } = 1;
        [JsonIgnore]
        public int ColumnSpan { get; set; } = 1;
        public int ThuDihoc { get; set; }
        public int TietBatDau {  get; set; }

        public int TinChi { get; set; }
    }
}
