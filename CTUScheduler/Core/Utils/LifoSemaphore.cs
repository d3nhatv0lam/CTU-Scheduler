using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Utils;

/// <summary>
/// Simplify Semaphore LIFO cass
/// </summary>
public class LifoSemaphore
{
    private readonly int _maxConcurrency;
    private int _currentCount;
    private readonly Stack<TaskCompletionSource<bool>> _waiters = new();
    private readonly Lock _lock = new();

    public LifoSemaphore(int maxConcurrency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        _maxConcurrency = maxConcurrency;
    }

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_currentCount < _maxConcurrency)
            {
                _currentCount++;
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            }

            _waiters.Push(tcs);
            return tcs.Task;
        }
    }

    /// <summary>
    /// Hiểu đơn giản, khi hàm này được gọi, sẽ coi như xong task, và cho phép thằng tiếp theo trong stack chạy (try set = true)
    /// </summary>
    public void Release()
    {
        lock (_lock)
        {
            // có while là để cho chắc là các task sắp lấy có bị cancel chưa
            while (_waiters.Count > 0)
            {
                var popCts = _waiters.Pop();

                // bị cancel thì lệnh trả về false => tiếp tục lấy task tiếp theo làm
                if (popCts.TrySetResult(true))
                {
                    return;
                }
            }
            
            // không còn ai làm thì trả slot
            _currentCount--;
        }
    }
}