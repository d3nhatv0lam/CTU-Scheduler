using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed
{
    public class PeriodItem
    {
        // khóa k49, k50 ....
        public string key { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Group { get; set; }
    }
}
