using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.StudentAcademicService;

public class CourseCatalogService : ICourseCatalogRefactorService
{
    private readonly ICourseCatalogClient _client;
    private readonly Lazy<IUserSessionService> _userSessionService;

    private readonly ILogger<CourseCatalogService> _logger;

    public CourseCatalogService(ICourseCatalogClient client,
        Lazy<IUserSessionService> userSessionService,
        ILogger<CourseCatalogService> logger)
    {
        _client = client;
        _userSessionService = userSessionService;
        _logger = logger;
    }

    public async Task<OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>> FetchSuggestionsAsync(string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<QuickSelectDmhpCourse>();

        try
        {
            var filters = await _client.GetAutoCompleteQueryAsync(query, cancellationToken);

            return OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>.Success(filters);
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogWarning(ex, "Phiên làm việc đã hết hạn.");
            return OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>.Failed(
                "Phiên đăng ký của bạn đã hết hạn trên hệ thống trường. Vui lòng đăng nhập lại.",
                kind: OperationFailureReason.Unauthorized
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Lỗi kết nối mạng hoặc không có Internet.");
            return OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>.Failed(
                "Không có kết nối Internet hoặc máy chủ trường không phản hồi.",
                kind: OperationFailureReason.Network
            );
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Tác vụ bị hủy bởi người dùng.");
                return OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>.Failed("Đã hủy quá trình lấy dữ liệu.",
                    kind: OperationFailureReason.UserAction);
            }

            _logger.LogWarning(ex, "Yêu cầu đồng bộ quy định bị quá thời gian (Timeout).");
            return OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>.Failed(
                "Thời gian kết nối đến máy chủ trường quá lâu. Vui lòng thử lại.",
                kind: OperationFailureReason.Network
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Dữ liệu trả về từ CTU không thể phân rã.");
            return OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>.Failed(
                "Hệ thống không thể phân tích dữ liệu của trường. Nhà trường có thể đã cập nhật API mới.",
                kind: OperationFailureReason.System
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi Refresh Registration");
            return OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>.FromException(
                ex,
                "Lấy danh sách tên học phần thất bại do lỗi hệ thống chưa xác định.",
                kind: OperationFailureReason.System
            );
        }
    }

    public async Task<OperationResult<Course>> FetchCourseAsync(string courseCode,
        int? academicYear = null,
        int? semester = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedYear = academicYear;
            var resolvedSemester = semester;

            if (resolvedYear is null || resolvedSemester is null)
            {
                var context = _userSessionService.Value.CurrentContext;
                if (context is not null)
                {
                    resolvedYear = context.AcademicYear;
                    resolvedSemester = context.Semester;
                }
            }

            if (resolvedYear is null || resolvedSemester is null)
            {
                return OperationResult<Course>.Failed(
                    "Không thể xác định Năm học hoặc Học kỳ. Vui lòng đăng nhập hoặc chọn một học kỳ hợp lệ.",
                    kind: OperationFailureReason.Validation
                );
            }

            var rawCourse = await _client.GetCoursesRawAsync(
                resolvedYear.Value,
                resolvedSemester.Value,
                courseCode,
                cancellationToken);

            var course = rawCourse.ToCourse();

            if (course is null)
            {
                return OperationResult<Course>.Failed(
                    $"Không thể phân tích thông tin chi tiết cho học phần {courseCode}.",
                    "Catalog.MappingError",
                    OperationFailureReason.System
                );
            }

            _logger.LogDebug("Tải thành công chi tiết môn học {Code}", courseCode);
            return course;
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogWarning(ex, "Phiên làm việc đã hết hạn.");
            return OperationResult<Course>.Failed(
                "Phiên đăng ký của bạn đã hết hạn trên hệ thống trường. Vui lòng đăng nhập lại.",
                kind: OperationFailureReason.Unauthorized
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Lỗi kết nối mạng hoặc không có Internet.");
            return OperationResult<Course>.Failed(
                "Không có kết nối Internet hoặc máy chủ trường không phản hồi.",
                kind: OperationFailureReason.Network
            );
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Dừng lấy môn học bởi người dùng.");
                return OperationResult<Course>.Failed("Đã hủy yêu cầu.", kind: OperationFailureReason.UserAction);
            }

            _logger.LogWarning(ex, "Yêu cầu bị quá thời gian (Timeout).");
            return OperationResult<Course>.Failed(
                "Thời gian kết nối đến máy chủ trường quá lâu. Vui lòng thử lại.",
                kind: OperationFailureReason.Network
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Dữ liệu trả về từ CTU không thể phân rã.");
            return OperationResult<Course>.Failed(
                "Hệ thống không thể phân tích dữ liệu quy định của trường. Nhà trường có thể đã cập nhật API mới.",
                kind: OperationFailureReason.System
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi Refresh Registration");
            return OperationResult<Course>.FromException(
                ex,
                "Đồng bộ quy định đăng ký thất bại do lỗi hệ thống chưa xác định.",
                kind: OperationFailureReason.System
            );
        }
    }


    public async IAsyncEnumerable<Course> FetchCoursesBatchAsync(
        IEnumerable<string> courseCodes,
        int? academicYear = null,
        int? semester = null,
        int maxWorkers = 2,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWorkers, 1);
        var codesList = courseCodes.Distinct().ToList();
        if (codesList.Count == 0)
            yield break;

        var actualWorkers = Math.Min(codesList.Count, maxWorkers);

        _logger.LogInformation("Bắt đầu tải song song {Count} môn học với {Workers} HTTP Worker Tasks...",
            codesList.Count, actualWorkers);

        var queue = new ConcurrentQueue<string>(codesList);

        var channel = Channel.CreateUnbounded<Course>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var workerTasks = new List<Task>(actualWorkers);
        for (int i = 0; i < actualWorkers; i++)
        {
            var workerId = i + 1;
            workerTasks.Add(ProcessQueueAsync(workerId,
                queue,
                channel.Writer,
                academicYear,
                semester,
                cancellationToken));
        }

        var completionTask = CompleteChannelAsync(workerTasks, channel.Writer);

        try
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (channel.Reader.TryRead(out var course))
                {
                    yield return course;
                }
            }
        }
        finally
        {
            await completionTask;
        }

        static async Task CompleteChannelAsync(IEnumerable<Task> tasks, ChannelWriter<Course> writer)
        {
            try
            {
                await Task.WhenAll(tasks);
                writer.TryComplete();
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
            }
        }
    }

    private async Task ProcessQueueAsync(
        int workerId,
        ConcurrentQueue<string> queue,
        ChannelWriter<Course> writer,
        int? academicYear,
        int? semester,
        CancellationToken cancellationToken)
    {
        while (queue.TryDequeue(out var courseCode))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await FetchCourseAsync(courseCode, academicYear, semester, cancellationToken);

            if (result.IsSuccess)
            {
                await writer.WriteAsync(result.Content, cancellationToken);

                _logger.LogDebug("[Worker {Id}] Tải thành công môn {Code}", workerId, courseCode);
            }
            else
            {
                var stringMessage = string.Join("\n", result.Errors.Select(x => x.FormattedMessage));
                if (result.Kind is OperationFailureReason.Unauthorized
                    or OperationFailureReason.System
                    or OperationFailureReason.Validation)
                {
                    _logger.LogError("[Worker {Id}] Lỗi dừng tải môn tại {Code}: {Message}",
                        workerId, courseCode, stringMessage);

                    throw new BatchFetchAbortedException(
                        $"Lỗi nghiêm trọng khiến tiến trình cào bị hủy tại môn {courseCode}: {stringMessage}",
                        result.Exception
                    );
                }

                _logger.LogInformation("[Worker {Id}] Bỏ qua môn {Code} do lỗi: {Message}",
                    workerId, courseCode, stringMessage);
            }

            // chèn delay giảm tải cho server của trường
            await Task.Delay(150, cancellationToken);
        }
    }
}