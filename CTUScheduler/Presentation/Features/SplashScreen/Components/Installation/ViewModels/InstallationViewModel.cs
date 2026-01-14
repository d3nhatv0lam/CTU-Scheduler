using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using CTUScheduler.Presentation.Base;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.SplashScreen.Components.Installation.ViewModels;

public class InstallationViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    private string _consoleLog = "> Khởi tạo môi trường...\n";

    // 1. Regex xóa mã màu rác (ANSI)
    private readonly Regex _ansiRegex = new Regex(@"\x1B\[[0-9;?]*[a-zA-Z]", RegexOptions.Compiled);

    // 2. Regex để nhận diện xem 1 dòng có phải là Progress Bar không (Chứa % và dung lượng)
    private readonly Regex _progressPattern = new Regex(@"\d+%.*MiB", RegexOptions.Compiled);

    // 3. Regex lấy thông tin để vẽ lại thanh đẹp hơn
    private readonly Regex _extractInfoRegex = new Regex(@"(\d+)% of ([\d\.]+ \w+)", RegexOptions.Compiled);

    public string ConsoleLog
    {
        get => _consoleLog;
        set => this.RaiseAndSetIfChanged(ref _consoleLog, value);
    }

    public InstallationViewModel() { }

    public InstallationViewModel(IObservable<string> consoleLogObservable)
    {
        consoleLogObservable
            .Buffer(TimeSpan.FromMilliseconds(50))
            .Where(x => x.Count > 0)
            .Select(x => string.Concat(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ProcessLogChunk)
            .DisposeWith(_disposables);
    }

    private void ProcessLogChunk(string chunk)
    {
        try
        {
            // Xóa mã màu rác
            var text = _ansiRegex.Replace(chunk, string.Empty);

            // Nếu có ký tự \r (Ghi đè)
            if (text.Contains('\r'))
            {
                var parts = text.Split('\r');
                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    // Thử vẽ lại thanh bar đẹp hơn
                    var (isProgress, prettyBar) = TryRenderProgressBar(part);

                    if (isProgress)
                    {
                        SafeReplaceLastLine(prettyBar);
                    }
                    else
                    {
                        // Log thường -> Cộng dồn
                        AppendLog(part);
                    }
                }
            }
            else
            {
                AppendLog(text);
            }
        }
        catch
        {
            ConsoleLog += chunk;
        }
    }

    private void AppendLog(string text)
    {
        // Làm sạch xuống dòng thừa
        string clean = text.Trim('\n', '\r');
        if (string.IsNullOrWhiteSpace(clean)) return;

        if (!ConsoleLog.EndsWith("\n")) ConsoleLog += "\n";
        ConsoleLog += clean;
    }

    /// <summary>
    /// Hàm thay thế dòng cuối THÔNG MINH (Chỉ thay thế nếu dòng cuối cũng là progress bar)
    /// </summary>
    private void SafeReplaceLastLine(string newBar)
    {
        if (string.IsNullOrEmpty(ConsoleLog))
        {
            ConsoleLog = newBar;
            return;
        }

        // 1. Lấy ra dòng cuối cùng hiện tại
        var trimmedLog = ConsoleLog.TrimEnd('\n');
        var lastNewLineIndex = trimmedLog.LastIndexOf('\n');
        
        string lastLineContent;
        if (lastNewLineIndex >= 0)
        {
            lastLineContent = trimmedLog.Substring(lastNewLineIndex + 1);
        }
        else
        {
            lastLineContent = trimmedLog;
        }

        // 2. KIỂM TRA: Dòng cuối cùng có phải là Progress Bar không?
        // Nếu dòng cuối là "Downloading Chromium..." (không chứa %) -> Đừng xóa nó! Xuống dòng mới.
        // Nếu dòng cuối là "[=== ] 10%..." -> Xóa nó đi thay cái mới vào.
        bool lastLineIsProgress = _progressPattern.IsMatch(lastLineContent) || lastLineContent.Contains("[");

        if (lastLineIsProgress)
        {
            // Thay thế (Ghi đè)
            if (lastNewLineIndex >= 0)
                ConsoleLog = ConsoleLog.Substring(0, lastNewLineIndex + 1) + newBar;
            else
                ConsoleLog = newBar;
        }
        else
        {
            // Dòng cuối là text thường, nên ta phải xuống dòng để in Progress Bar mới
            ConsoleLog += "\n" + newBar;
        }
    }

    private (bool Success, string Result) TryRenderProgressBar(string input)
    {
        var match = _extractInfoRegex.Match(input);
        if (match.Success)
        {
            int percent = int.Parse(match.Groups[1].Value);
            string details = match.Groups[2].Value;

            int totalWidth = 25; // Độ dài thanh bar
            int filledWidth = (int)((percent / 100.0) * totalWidth);

            string bar = new string('■', filledWidth) + new string(' ', totalWidth - filledWidth);
            
            // Format: [■■■■     ] 20% of 140 MiB
            return (true, $"[{bar}] {percent}% of {details}");
        }
        return (false, input);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}