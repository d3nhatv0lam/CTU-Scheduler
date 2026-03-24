using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Presentation.Features.Pagination.Interfaces;
using DynamicData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Pagination.ViewModels;

public class PaginationViewModel<T> : ReactiveObject, IDisposable, IPaginationBinding, IPaginationViewModel<T>
    where T : class
{
    protected const int DEFAULT_PAGE_SIZE = 12;
    private readonly bool _ownsData;
    private readonly ObservableAsPropertyHelper<int> _totalPages;
    private readonly ObservableAsPropertyHelper<bool> _isFirstPage;
    private readonly ObservableAsPropertyHelper<bool> _isLastPage;
    private readonly ReadOnlyObservableCollection<T> _bindablePagedData;
    private int _currentPage;

    protected CompositeDisposable Disposables { get; } = new();
    protected ISourceList<T> DataList { get; }
    protected IObservable<IChangeSet<T>> DataSharedConnection => DataList.Connect();
    protected BehaviorSubject<IPageRequest> PageRequestSubject { get; }


    public int PageSize { get; }
    public int TotalPages => _totalPages.Value;
    public bool IsFirstPage => _isFirstPage.Value;
    public bool IsLastPage => _isLastPage.Value;
    public IEnumerable<T> CurrentData => DataList.Items;
    public ReadOnlyObservableCollection<T> PagedData => _bindablePagedData;

    public int CurrentPage
    {
        get => _currentPage;
        protected set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }


    public ReactiveCommand<Unit, Unit> NextPageCommand { get; protected set; }
    public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; protected set; }


    public PaginationViewModel(int pageSize = DEFAULT_PAGE_SIZE) : this(new SourceList<T>(), pageSize)
    {
        _ownsData = true;
    }


    public PaginationViewModel(SourceList<T> data, int pageSize = DEFAULT_PAGE_SIZE)
    {
        DataList = data ?? throw new ArgumentNullException(nameof(data));
        PageSize = pageSize > 0 ? pageSize : DEFAULT_PAGE_SIZE;

        PageRequestSubject = new BehaviorSubject<IPageRequest>(new PageRequest(1, PageSize)).DisposeWith(Disposables);

        _totalPages = DataList.CountChanged
            .Select(count => (int)Math.Ceiling(count / (double)PageSize))
            .DistinctUntilChanged()
            .ToProperty(this, nameof(TotalPages), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);

        _isFirstPage = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page <= 1)
            .ToProperty(this, nameof(IsFirstPage), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);

        _isLastPage = this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages)
            .Select(tuple => tuple.Item1 >= tuple.Item2)
            .ToProperty(this, nameof(IsLastPage), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);


        var canGoNext = this.WhenAnyValue(x => x.IsLastPage, isLastPage => !isLastPage);
        NextPageCommand = ReactiveCommand.Create(GoNextPage, canGoNext).DisposeWith(Disposables);

        var canGoPrevious = this.WhenAnyValue(x => x.IsFirstPage, isFirstPage => !isFirstPage);
        PreviousPageCommand = ReactiveCommand.Create(GoPreviousPage, canGoPrevious).DisposeWith(Disposables);

        DataSharedConnection
            .SubscribeOn(RxApp.TaskpoolScheduler)
            .DisposeMany()
            .Page(PageRequestSubject)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _bindablePagedData)
            .Subscribe()
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.CurrentPage)
            .Where(page => page > 0)
            .DistinctUntilChanged()
            .Subscribe(newPage => PageRequestSubject.OnNext(new PageRequest(newPage, PageSize)))
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.TotalPages)
            .Subscribe(total =>
            {
                if (total == 0) CurrentPage = 0;
                else if (CurrentPage == 0) CurrentPage = 1;
                else if (CurrentPage > total) CurrentPage = total;
            })
            .DisposeWith(Disposables);
    }

    protected virtual void GoNextPage()
    {
        CurrentPage++;
    }

    protected virtual void GoPreviousPage()
    {
        CurrentPage--;
    }


    public virtual void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        DataList.Add(item);
    }


    public virtual void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        DataList.AddRange(items);
    }

    public virtual void Clear()
    {
        DataList.Clear();
    }

    public virtual void Dispose()
    {
        if (_ownsData) DataList.Dispose();
        Disposables.Dispose();
    }
}