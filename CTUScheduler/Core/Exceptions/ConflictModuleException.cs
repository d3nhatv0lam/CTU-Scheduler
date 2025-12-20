using System;

namespace CTUScheduler.Core.Exceptions
{
    public class ConflictModuleException: Exception
    {
        public string MaHocPhan { get; set; } = string.Empty;
        public string ThuDiHoc { get; set; } = string.Empty;
        public ConflictModuleException(): base() { }
        public ConflictModuleException(string message) : base(message) { }
        public ConflictModuleException(string message, string maHocPhan, string thuDiHoc) : base(message)
        {
            MaHocPhan = maHocPhan;
            ThuDiHoc = thuDiHoc;
        }
        public ConflictModuleException(string message, Exception innerException) : base(message, innerException) { }

    }
}
