using CTUScheduler.Presentation.Features.CrashReport.ViewModels;
using CTUScheduler.Presentation.Shared.Interfaces;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Features.CrashReport.Views;

public partial class CrashReportWindow : ReactiveUrsaWindow<CrashReportViewModel>
{
    public CrashReportWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is IRequestClose requestClose)
            {
                requestClose.RequestClose += _ => Close();
            }
        };
    }
}
