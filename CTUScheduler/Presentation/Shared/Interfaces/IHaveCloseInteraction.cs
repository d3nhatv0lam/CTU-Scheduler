using System.Reactive;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shared.Interfaces;

/// <summary>
/// VM con có khả năng đóng và trả kết quả TResult lên VM cha
/// </summary>
public interface IHaveCloseInteraction<TResult>
{
    // TInput (đầu vào từ con truyền lên): TResult
    // TOutput (đầu ra cha trả về sau khi đóng): Unit
    Interaction<TResult, Unit> CloseInteraction { get; }
}