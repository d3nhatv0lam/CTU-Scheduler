using System;
using System.Collections.ObjectModel;
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
    private ObservableAsPropertyHelper<int> _totalPages;
    private int _currentPage;
    private bool _isFirstPage;
    private bool _isLastPage;
    
    public ReadOnlyObservableCollection<T> PagedData => _bindablePagedData;
    
    public int TotalPages => _totalPages.Value;
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
        _currentPage = 1;
        _pageRequest = new BehaviorSubject<PageRequest>(new PageRequest(CurrentPage, PageSize));
        ObservableInit();
        CommandInit();
    }

    private void ObservableInit()
    {
        var dataConnection =  _data.Connect();

        _totalPages = dataConnection
            .Page<T>(_pageRequest)
            .Select(x => x.Response.Pages)
            .ToProperty(this, nameof(TotalPages), out _totalPages)
            .DisposeWith(_disposables);
        
        dataConnection
            .DisposeMany()
            .Page<T>(_pageRequest)
            .Bind(out _bindablePagedData)
            .Subscribe()
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.TotalPages)
            .Subscribe(totalPage =>
            {
                if (totalPage <= 1)
                    CurrentPage = 1;
            })
            .DisposeWith(_disposables);
        
        this.WhenAnyValue(x => x.CurrentPage)
            .Subscribe(newPage => _pageRequest.OnNext(new PageRequest(newPage, PageSize)))
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => CurrentPage, x => x.TotalPages)
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
        NextPageCommand = ReactiveCommand.CreateFromTask(NextPageAsync,canGoNext);
        
        var canGoPrevious = this.WhenAnyValue(x => x.IsFirstPage, isFirstPage => !isFirstPage);
        PreviousPageCommand = ReactiveCommand.CreateFromTask(PreviousPageAsync,canGoPrevious);
    }

    public Task NextPageAsync()
    {
        if (!IsLastPage)
            CurrentPage++;
        return Task.CompletedTask;
    }

    public Task PreviousPageAsync()
    {
        if (!IsFirstPage)
            CurrentPage--;
        return Task.CompletedTask;
    }
    
    public void AddItem(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        _data.Add(item);
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