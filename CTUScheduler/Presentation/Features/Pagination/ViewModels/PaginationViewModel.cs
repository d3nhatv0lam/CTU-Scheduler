using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Presentation.Features.Pagination.Interfaces;
using DynamicData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Pagination.ViewModels;

public class PaginationViewModel<T>: ReactiveObject, IDisposable, IPaginationBinding, IPaginationViewModel<T> where T : class
{
    protected const int DEFAULT_PAGE_SIZE = 12;
    private readonly bool _ownsData; 
    private readonly CompositeDisposable _disposables = new();
    private readonly SourceList<T> _data;
    private readonly BehaviorSubject<PageRequest> _pageRequest;
    private ReadOnlyObservableCollection<T> _bindablePagedData;
    private int _totalPages;
    private int _currentPage;
    private bool _isFirstPage;
    private bool _isLastPage;
    
    protected CompositeDisposable Disposables => _disposables;
    protected ISourceList<T> Data => _data;
    protected IObservable<IChangeSet<T>> DataSharedConnection => _data.Connect().Publish().RefCount();
    protected IObservable<PageRequest> PageRequest => _pageRequest.AsObservable();
    public IEnumerable<T> CurrentData => _data.Items;
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public PaginationViewModel(SourceList<T> data,int pageSize)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        PageSize = pageSize > 0 ? pageSize : DEFAULT_PAGE_SIZE;
        _currentPage = 0;
        _pageRequest = new BehaviorSubject<PageRequest>(new PageRequest(CurrentPage, PageSize));
        Initialize();
    }
    
    private void Initialize()
    {
        OnObservableInit();
        OnCommandInit();
    }

    protected virtual void OnObservableInit()
    {
        _data.CountChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(count => (int)Math.Ceiling(count / (double)PageSize))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(totalPages =>
            {
                TotalPages = totalPages;
            });
        
        DataSharedConnection
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Page(_pageRequest)
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
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(totalPage =>
            {
                CurrentPage = totalPage > 0 ? 1 : 0;
            })
            .DisposeWith(_disposables);
        
        this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(tuple =>
            {
                var (currentPage, totalPages) = tuple;
                IsFirstPage = currentPage <= 1;
                IsLastPage = currentPage >= totalPages;
            })
            .DisposeWith(_disposables);
    }

    protected virtual void OnCommandInit()
    {
        var canGoNext = this.WhenAnyValue(x => x.IsLastPage, isLastPage => !isLastPage);
        NextPageCommand = ReactiveCommand.Create(GoNextPage,canGoNext);
        
        var canGoPrevious = this.WhenAnyValue(x => x.IsFirstPage, isFirstPage => !isFirstPage);
        PreviousPageCommand = ReactiveCommand.Create(GoPreviousPage,canGoPrevious);
    }

    public virtual void GoNextPage()
    {
        if (!IsLastPage)
            CurrentPage++;
    }

    public virtual void GoPreviousPage()
    {
        if (!IsFirstPage)
            CurrentPage--;
    }
    
    public virtual void AddItem(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        _data.Add(item);
    }
   

    public virtual void AddAll(IEnumerable<T> items)
    {
        _data.AddRange(items);
    }

    public virtual void Clear()
    {
        _data.Clear();
    }

    public void Dispose()
    {
        if (_ownsData) _data.Dispose();
        _disposables.Dispose();
    }
    
}