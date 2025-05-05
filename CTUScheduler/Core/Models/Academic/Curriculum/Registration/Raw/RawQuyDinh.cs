using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw
{
    public class RawQuyDinh
    {
        public List<RawQuyDinhData> leftData { get; set; }
        public List <RawQuyDinhData> rightData { get; set; }
    }
}
