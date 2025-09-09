namespace CTUScheduler.Core.Interfaces;

public interface ICleanup
{
    /// <summary>
    /// Perform operations before VM terminates / closes view.
    /// <br/>
    /// Thực hiện các thao tác trước khi VM kết thúc / đóng view.
    /// </summary>
    public void Cleanup();
}