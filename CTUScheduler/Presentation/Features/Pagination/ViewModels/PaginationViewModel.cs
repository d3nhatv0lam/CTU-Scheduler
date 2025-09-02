using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Features.Pagination.Interfaces;
using DynamicData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Pagination.ViewModels;

public class PaginationViewModel<T>: ReactiveObject, IDisposable, IPaginationBinding, IPaginationViewModel<T> where T : class
{
    private const int DEFAULT_PAGE_SIZE = 12;
    private readonly bool _ownsData = false; 
    private readonly CompositeDisposable _disposables = new();
    private readonly SourceList<T> _data;
    private readonly BehaviorSubject<PageRequest> _pageRequest;
    private ReadOnlyObservableCollection<T> _bindablePagedData;
    private int _totalPages;
    private int _currentPage;
    private bool _isFirstPage;
    private bool _isLastPage;
    
    public ReadOnlyObservableCollection<T> PagedData => _bindablePagedData;

    public int TotalPages
    {
        get => _totalPages;
        private set => this.RaiseAndSetIfChanged(ref _totalPages, value);
    }
    public int PageSize { get; }

    public int CurrentPage
    {
        get => _currentPage;
        private set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }
    
    public bool IsFirstPage
    {
        get => _isFirstPage;
        private set => this.RaiseAndSetIfChanged(ref _isFirstPage, value);
    }
    
    public bool IsLastPage
    {
        get => _isLastPage;
        private set => this.RaiseAndSetIfChanged(ref _isLastPage, value);
    }
    
    public ReactiveCommand<Unit, Unit> NextPageCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; private set; }

    public PaginationViewModel() : this(new SourceList<T>(), DEFAULT_PAGE_SIZE)
    {
        _ownsData = true;
    }
    public PaginationViewModel(int pageSize) : this(new SourceList<T>(), pageSize)
    {
        
    }
    
    public PaginationViewModel(SourceList<T> data) : this(data, DEFAULT_PAGE_SIZE)
    {
        
    }

    public PaginationViewModel(SourceList<T> data,int pageSize)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        PageSize = pageSize > 0 ? pageSize : DEFAULT_PAGE_SIZE;
        _currentPage = 0;
        _pageRequest = new BehaviorSubject<PageRequest>(new PageRequest(CurrentPage, PageSize));
        ObservableInit();
        CommandInit();
    }

    private void ObservableInit()
    {
        _data.CountChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(count => (int)Math.Ceiling(count / (double)PageSize))
            .DistinctUntilChanged()
            .Subscribe(totalPages =>
            {
                TotalPages = totalPages;
            });
           
        _data.Connect()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Page<T>(_pageRequest)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _bindablePagedData)
            .Subscribe()
            .DisposeWith(_disposables);
        
        this.WhenAnyValue(x => x.CurrentPage)
            .Where(page => page >= 0)
            .DistinctUntilChanged()
            .Subscribe(newPage => _pageRequest.OnNext(new PageRequest(newPage,PageSize)))
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.TotalPages)
            .Subscribe(totalPage =>
            {
                CurrentPage = totalPage > 0 ? 1 : 0;
            })
            .DisposeWith(_disposables);
        
        this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages)
            .Subscribe(tuple =>
            {
                var (currentPage, totalPages) = tuple;
                IsFirstPage = currentPage <= 1;
                IsLastPage = currentPage >= totalPages;
            })
            .DisposeWith(_disposables);
    }

    private void CommandInit()
    {
        var canGoNext = this.WhenAnyValue(x => x.IsLastPage, isLastPage => !isLastPage);
        NextPageCommand = ReactiveCommand.Create(GoNextPage,canGoNext);
        
        var canGoPrevious = this.WhenAnyValue(x => x.IsFirstPage, isFirstPage => !isFirstPage);
        PreviousPageCommand = ReactiveCommand.Create(GoPreviousPage,canGoPrevious);
    }

    public void GoNextPage()
    {
        if (!IsLastPage)
            CurrentPage++;
    }

    public void GoPreviousPage()
    {
        if (!IsFirstPage)
            CurrentPage--;
    }
    
    public void AddItem(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        _data.Add(item);
    }

    public void AddAll(IEnumerable<T> items)
    {
        _data.AddRange(items);
    }

    public void Clear()
    {
        _data.Clear();
    }

    public void Dispose()
    {
        if (_ownsData) _data.Dispose();
        _disposables.Dispose();
    }
    
}