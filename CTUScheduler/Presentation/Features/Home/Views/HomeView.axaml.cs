using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using Avalonia.Controls;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Home.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Home.Views;

public partial class HomeView : ReactiveUserControl<HomeViewModel>
{
    public HomeView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo, v => v.txtRegistrationInfo.Text, info => info == null ? 
                            "Không lấy được thông tin!" : 
                            $"Học kỳ: {info.Semester ?? "?"}\n{info.AcademicYear}-{info.AcademicYear+1}\nSố tín chỉ tối đa: {info.MaxCreditPerSemester}").DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RegistrationInfo, v => v.txtRegistrationTime.Text, info => info == null ?
                            "Không lấy được thông tin!" :
                            info.Period).DisposeWith(disposables);
            
            this.OneWayBind<HomeViewModel, HomeView, List<PeriodItem>, IEnumerable>(ViewModel, vm => vm.RegistrationInfo.UserPeriod, v => v.dtgridUserRegistrationTime.ItemsSource, userRegistrationTime => userRegistrationTime ?? Enumerable.Empty<PeriodItem>()).DisposeWith(disposables);

            this.OneWayBind<HomeViewModel, HomeView, List<GroupItem>, IEnumerable>(ViewModel, vm => vm.RegistrationInfo.Groups, v => v.dtgridGroupList.ItemsSource, groups => groups ?? Enumerable.Empty<GroupItem>()).DisposeWith(disposables);
        });
    }
    
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        double breakPoint = 1000;

        if (e.NewSize.Width < breakPoint)
        {
            // Chế độ Màn hình nhỏ (1 Cột dọc)
            MainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            MainGrid.ColumnDefinitions[1].Width = new GridLength(0); // Giấu cột 2

            // Đẻ thêm 2 dòng nữa cho đủ 4 dòng xếp dọc
            while (MainGrid.RowDefinitions.Count < 4)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            Grid.SetColumn(Card2, 0); Grid.SetRow(Card2, 1);
            Grid.SetColumn(Card3, 0); Grid.SetRow(Card3, 2);
            Grid.SetColumn(Card4, 0); Grid.SetRow(Card4, 3);
        }
        else
        {
            // Chế độ Full màn hình (2 Cột)
            MainGrid.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star);
            MainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

            Grid.SetColumn(Card2, 1); Grid.SetRow(Card2, 0);
            Grid.SetColumn(Card3, 0); Grid.SetRow(Card3, 1);
            Grid.SetColumn(Card4, 1); Grid.SetRow(Card4, 1);

            // Xóa sạch các dòng thừa đi để triệt tiêu cái RowSpacing (15px) đội lên từ cõi âm!
            while (MainGrid.RowDefinitions.Count > 2)
            {
                MainGrid.RowDefinitions.RemoveAt(MainGrid.RowDefinitions.Count - 1);
            }
        }
    }
}