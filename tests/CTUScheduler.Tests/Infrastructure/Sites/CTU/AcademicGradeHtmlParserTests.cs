// Created: 2026-11-28
// Implemented by AI with author's review.

using System;
using System.IO;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Infrastructure.Sites.CTU.Parsers;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.Sites.CTU;

public class AcademicGradeHtmlParserTests
{
    private readonly AcademicGradeHtmlParser _parser = new();
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 11, 28, 13, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Parse_EmptyOrWhitespaceHtml_ThrowsInvalidDataException()
    {
        Action act = () => _parser.Parse("   ", FixedTimestamp);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_MissingFormXemDiem_ThrowsSessionExpiredException()
    {
        const string expiredHtml = """
            <!DOCTYPE html>
            <html>
                <head><title>He thong quan ly</title></head>
                <script language="javascript">location.href='../../login.php';</script>
            </html>
            """;

        Action act = () => _parser.Parse(expiredHtml, FixedTimestamp);

        act.Should().Throw<SessionExpiredException>();
    }

    [Fact]
    public void Parse_ValidHtmlWithEdgeCases_ParsesSemestersCoursesAndSummariesAccurately()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
            <body>
                <form name="frmXemDiem" method="post">
                    <table width="100%">
                        <tr><td class="main_1_left"></td><td class="main_1">Xem Điểm Học Kỳ </td><td class="main_1_right"></td></tr>
                    </table>
                    <table width="100%">
                        <tr><td class="main_1_left"></td><td class="main_1">Xem Ðiểm Học Kỳ 1 Năm Học 2026 - 2027</td><td class="main_1_right"></td></tr>
                        <tr>
                            <td colspan="3">
                                <table class="border_1">
                                    <tr><td>Stt</td><td>Mã HP</td><td>Tên học phần</td><td>Điều kiện</td><td>Nhóm</td><td>Tín chỉ</td><td>Điểm chữ</td><td>Điểm số</td><td>Tích lũy</td></tr>
                                    <tr><td>1</td><td>CT250E&nbsp;</td><td>Niên luận ngành KTPM&nbsp;</td><td>&nbsp;</td><td>01&nbsp;</td><td>3&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>
                                    <tr><td>2</td><td>CT287&nbsp;</td><td>Kiểm chứng mô hình&nbsp;</td><td>&nbsp;</td><td>01&nbsp;</td><td>3&nbsp;</td><td>I&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>
                                    <tr><td>3</td><td>CT299&nbsp;</td><td>Môn ví dụ rớt&nbsp;</td><td>&nbsp;</td><td>02&nbsp;</td><td>2&nbsp;</td><td>F&nbsp;</td><td>2.5&nbsp;</td><td>&nbsp;</td></tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                    <table width="100%">
                        <tr><td class="main_1_left"></td><td class="main_1">Xem Ðiểm Học Kỳ Hè Năm Học 2023 - 2024</td><td class="main_1_right"></td></tr>
                        <tr>
                            <td colspan="3">
                                <table class="border_1">
                                    <tr><td>Stt</td><td>Mã HP</td><td>Tên học phần</td><td>Điều kiện</td><td>Nhóm</td><td>Tín chỉ</td><td>Điểm chữ</td><td>Điểm số</td><td>Tích lũy</td></tr>
                                    <tr><td>1</td><td>ML016&nbsp;</td><td>Kinh tế chính trị Mác - Lênin&nbsp;</td><td>&nbsp;</td><td>12&nbsp;</td><td>2&nbsp;</td><td>A&nbsp;</td><td>9.0&nbsp;</td><td>* &nbsp;</td></tr>
                                </table>
                                <table>
                                    <tr><td>Tổng số tín chỉ đăng ký</td><td>5&nbsp;</td><td>Ðiểm trung bình học kỳ</td><td>4.00&nbsp;</td></tr>
                                    <tr><td>Tổng số tín chỉ tích lũy học kỳ</td><td>5&nbsp;</td><td>Ðiểm trung bình tích lũy</td><td>3.94&nbsp;</td></tr>
                                    <tr><td>Tổng số tín chỉ tích lũy</td><td>40&nbsp;</td><td>Ðiểm rèn luyện</td><td>&nbsp;</td></tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                    <table width="100%">
                        <tr><td class="main_1_left"></td><td class="main_1">Xem Ðiểm Học Kỳ 1 Năm Học 2023 - 2024</td><td class="main_1_right"></td></tr>
                        <tr>
                            <td colspan="3">
                                <table class="border_1">
                                    <tr><td>Stt</td><td>Mã HP</td><td>Tên học phần</td><td>Điều kiện</td><td>Nhóm</td><td>Tín chỉ</td><td>Điểm chữ</td><td>Điểm số</td><td>Tích lũy</td></tr>
                                    <tr><td>1</td><td>XH023&nbsp;</td><td>Anh văn căn bản 1 (*)&nbsp;</td><td>x&nbsp;</td><td>&nbsp;</td><td>4&nbsp;</td><td>M&nbsp;</td><td>*(BL)</td></tr>
                                    <tr><td>2</td><td>CT200&nbsp;</td><td>Nền tảng công nghệ thông
                                        tin&nbsp;</td><td>&nbsp;</td><td>D01&nbsp;</td><td>4&nbsp;</td><td>B+&nbsp;</td><td>
                                        8.6&nbsp;</td><td>* &nbsp;</td></tr>
                                    <tr><td>3</td><td>SHCVHT&nbsp;</td><td>Cố vấn học tập sinh hoạt lớp&nbsp;</td><td>&nbsp;</td><td>D23&nbsp;</td><td>0&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>
                                </table>
                                <table>
                                    <tr><td>Tổng số tín chỉ đăng ký</td><td>14&nbsp;</td><td>Ðiểm trung bình học kỳ</td><td>3.79&nbsp;</td></tr>
                                    <tr><td>Tổng số tín chỉ tích lũy học kỳ</td><td>14&nbsp;</td><td>Ðiểm trung bình tích lũy</td><td>4.00&nbsp;</td></tr>
                                    <tr><td>Tổng số tín chỉ tích lũy</td><td>18&nbsp;</td><td>Ðiểm rèn luyện</td><td>83&nbsp;</td></tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </form>
            </body>
            </html>
            """;

        var transcript = _parser.Parse(html, FixedTimestamp);

        transcript.FetchedAt.Should().Be(FixedTimestamp);
        transcript.Semesters.Should().HaveCount(3);

        // Sắp xếp tăng dần theo (AcademicYearStart, Term)
        var sem1 = transcript.Semesters[0];
        sem1.AcademicYearStart.Should().Be(2023);
        sem1.Term.Should().Be(1);
        sem1.Summary.Should().NotBeNull();
        sem1.Summary!.SemesterAccumulatedCredits.Should().Be(14);
        sem1.Summary.TotalAccumulatedCredits.Should().Be(18);
        sem1.Summary.SemesterGpa.Should().Be(3.79);
        sem1.Summary.CumulativeGpa.Should().Be(4.00);
        sem1.Summary.ConductScore.Should().Be(83);
        sem1.Courses.Should().HaveCount(3);

        var exemptCourse = sem1.Courses[0];
        exemptCourse.CourseCode.Should().Be("XH023");
        exemptCourse.IsConditionCourse.Should().BeTrue();
        exemptCourse.GroupCode.Should().BeNull();
        exemptCourse.LetterGrade.Should().Be(GradeLetter.M);
        exemptCourse.IsExempted.Should().BeTrue();
        exemptCourse.NumericScore.Should().BeNull();
        exemptCourse.RawScoreNote.Should().Be("*(BL)");
        exemptCourse.IsAccumulated.Should().BeTrue();

        var normalCourse = sem1.Courses[1];
        normalCourse.CourseCode.Should().Be("CT200");
        normalCourse.CourseName.Should().Be("Nền tảng công nghệ thông tin");
        normalCourse.LetterGrade.Should().Be(GradeLetter.BPlus);
        normalCourse.NumericScore.Should().Be(8.6);
        normalCourse.IsAccumulated.Should().BeTrue();

        var advisoryCourse = sem1.Courses[2];
        advisoryCourse.IsAdvisoryActivity.Should().BeTrue();
        advisoryCourse.RawLetterGrade.Should().BeNull();

        // Học kỳ Hè quy thành Term = 3 và ConductScore = null khi trống
        var summerSem = transcript.Semesters[1];
        summerSem.AcademicYearStart.Should().Be(2023);
        summerSem.Term.Should().Be(3);
        summerSem.Summary.Should().NotBeNull();
        summerSem.Summary!.ConductScore.Should().BeNull();

        // Học kỳ đang học chưa tổng kết: Summary = null, hỗ trợ điểm I và F
        var currentSem = transcript.Semesters[2];
        currentSem.AcademicYearStart.Should().Be(2026);
        currentSem.Term.Should().Be(1);
        currentSem.Summary.Should().BeNull();
        currentSem.Courses[0].NumericScore.Should().BeNull();
        currentSem.Courses[1].LetterGrade.Should().Be(GradeLetter.I);
        currentSem.Courses[2].LetterGrade.Should().Be(GradeLetter.F);
        currentSem.Courses[2].NumericScore.Should().Be(2.5);
    }

    [Fact]
    public void Parse_HtmlWithTbodyElements_ParsesSuccessfully()
    {
        const string htmlWithTbody = """
            <html>
            <body>
                <form name="frmXemDiem">
                    <table>
                        <tbody>
                            <tr><td class="main_1_left"></td><td class="main_1">Xem Điểm Học Kỳ 3 Năm Học 2024 - 2025</td><td class="main_1_right"></td></tr>
                            <tr>
                                <td colspan="3">
                                    <table class="border_1">
                                        <tbody>
                                            <tr><td>Stt</td><td>Mã HP</td><td>Tên học phần</td><td>Điều kiện</td><td>Nhóm</td><td>Tín chỉ</td><td>Điểm chữ</td><td>Điểm số</td><td>Tích lũy</td></tr>
                                            <tr><td>1</td><td>CT178</td><td>Nguyên lý hệ điều hành</td><td></td><td>02</td><td>3</td><td>A</td><td>9.8</td><td>*</td></tr>
                                        </tbody>
                                    </table>
                                    <table>
                                        <tbody>
                                            <tr><td>Tổng số tín chỉ tích lũy học kỳ</td><td>3</td><td>Điểm trung bình học kỳ</td><td>4.0</td></tr>
                                            <tr><td>Tổng số tín chỉ tích lũy</td><td>80</td><td>Điểm trung bình tích lũy</td><td>3.9</td></tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </form>
            </body>
            </html>
            """;

        var transcript = _parser.Parse(htmlWithTbody, FixedTimestamp);

        transcript.Semesters.Should().ContainSingle();
        var sem = transcript.Semesters[0];
        sem.AcademicYearStart.Should().Be(2024);
        sem.Term.Should().Be(3);
        sem.Courses.Should().ContainSingle();
        sem.Courses[0].CourseCode.Should().Be("CT178");
        sem.Summary.Should().NotBeNull();
        sem.Summary!.TotalAccumulatedCredits.Should().Be(80);
    }
}
