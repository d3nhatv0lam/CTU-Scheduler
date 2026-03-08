using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
//using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Exel;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetablePreviewViewModel: TimetableLayoutBaseViewModel
{
    private readonly List<SectionChoice> _choices = new();
    private readonly IExcelExporterService _excelExporter;

    public TimetablePreviewViewModel(IEnumerable<SectionChoice> choices, IExcelExporterService excelExporter)
    {
        ExportToExcelCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                // 1. In ra để biết lệnh đã bắt đầu chạy
                System.Diagnostics.Debug.WriteLine("=== BẮT ĐẦU XUẤT EXCEL ===");

                var safeName = string.IsNullOrWhiteSpace(this.Name) ? "TKB" : this.Name.Trim();
                string fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string fullPath = Path.Combine(desktopPath, fileName);

                System.Diagnostics.Debug.WriteLine($"Đường dẫn file: {fullPath}");

                // 2. Gọi hàm xuất file
                var result = await ExportToExcelFileAsync(fullPath);

                if (result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("=== XUẤT THÀNH CÔNG ===");
                    // Mở file lên (dùng Explorer để mở thư mục chứa file)
                    new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{fullPath}\"")
                    }.Start();
                }
                else
                {
                    // Lỗi logic từ Service trả về
                    throw new Exception($"Service báo lỗi: {result.FirstErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                // 3. BẮT ĐƯỢC LỖI Ở ĐÂY
                System.Diagnostics.Debug.WriteLine("=================================");
                System.Diagnostics.Debug.WriteLine("CHẾT Ở ĐÂY: " + ex.ToString());
                System.Diagnostics.Debug.WriteLine("=================================");
            }
        });

        _excelExporter = excelExporter ?? throw new ArgumentNullException(nameof(excelExporter));

        if (choices is null) return; // Nếu return ở đây thì lệnh Export ở trên ĐÃ ĐƯỢC GÁN RỒI -> An toàn!

        _choices.AddRange(choices);

        var sourceList = new SourceList<TimetableRenderItem>()
            .DisposeWith(Disposables);

        foreach (var choice in _choices)
        {
            var adapter = new StaticCourseAdapter(choice.Course, choice.Section);
            var item = CreateRenderItem(adapter);
            sourceList.Add(item);
        }

        VisualizerVM = new TimetableViewModel(sourceList.Connect());

        SubjectsCount = sourceList.Count;
        TotalCredits = sourceList.Items.Sum(x => x.SharedData.Credits);
    }
    
    public ScheduleBlueprint ToScheduleBlueprint()
    {
        int count = _choices.Count;
        var courses = new List<Course>(count);
        var groupKeys = new Dictionary<string, string>(count);
        foreach (var choice in _choices)
        {
            courses.Add(choice.Course.WithSection(choice.Section));
            var courseCode = choice.Course.Code;
            groupKeys.TryAdd(courseCode, choice.Section.Group);
        }
        var profile = new ScheduleProfile()
        {   
            Id = Guid.NewGuid(),
            Name = this.Name,
            SavedCourseGroupKeys = groupKeys,
            LastUpdated = this.LastUpdated
        };
        return new ScheduleBlueprint(courses, profile);
    }

    public async Task<OperationResult<string>> ExportToExcelFileAsync(string filePath)
    {
        try
        {
            // 1. Lấy dữ liệu 
            var choicesCopy = _choices.ToList();
            if (choicesCopy.Count == 0) return OperationResult<string>.Failed("Không có dữ liệu để xuất");

            var wb = await Task.Run(() => TimetableExcelBuilder.BuildWorkbook(choicesCopy, "Thời Khóa Biểu"));

            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            // 4. Lưu file xuống đĩa cứng
            await Task.Run(() => wb.SaveAs(filePath));

            return OperationResult<string>.Success(filePath);
        }
        catch (OperationCanceledException ex)
        {
            return OperationResult<string>.FromException(ex);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.FromException(ex);
        }
    }
}