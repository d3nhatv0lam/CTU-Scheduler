using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using CTUScheduler.Converters;
using CTUScheduler.Interfaces;
using CTUScheduler.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Controls
{
    public class TableControl : ItemsControl, IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        private Grid? _gridPanel;
        private Style? _cellStyle = null;

        static TableControl()
        {
            AffectsRender<TableControl>(RowsProperty, ColumnsProperty);
            AffectsMeasure<TableControl>(RowsProperty, ColumnsProperty);
            AffectsArrange<TableControl>(RowsProperty, ColumnsProperty);
        }
        
        public static readonly StyledProperty<int> RowsProperty =
            AvaloniaProperty.Register<TableControl, int>(nameof(Rows), 1);

        public int Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }
        public static readonly StyledProperty<int> ColumnsProperty =
            AvaloniaProperty.Register<TableControl, int>(nameof(Columns), 1);
        public int Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public TableControl() : base()
        {
            ItemsPanelDefinition();

            ItemsTemplateDefinition();
            this.GetObservable(ItemsSourceProperty).Subscribe(_ => CalculateCellIndexInGrid()).DisposeWith(_disposables);
        }

        
        private void ItemsPanelDefinition() 
        {
            this.ItemsPanel = new FuncTemplate<Panel?>(() =>
            {
                var _grid = new Grid()
                {
                    Background = Brushes.Transparent,
                };
                _gridPanel = _grid;
                return _gridPanel;
            });
        }
        /// <summary>
        /// ControlTemplate has ItemsPresenter
        /// </summary>
        private void ItemsTemplateDefinition()
        {
            this.Template = new FuncControlTemplate((control, scope) =>
            {
                var border = new Border()
                {
                    [~Border.BackgroundProperty] = control[~TableControl.BackgroundProperty]
                };
                var presenter = new ItemsPresenter()
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsPanelProperty] = control[~TableControl.ItemsPanelProperty],
                };
                scope.Register("PART_ItemsPresenter", presenter);
                border.Child = presenter;

                return border;
            });
        }

        
        private void CalculateCellIndexInGrid()
        {
            if (_cellStyle != null) return;
            _cellStyle = new Style(x => x.OfType<ContentPresenter>())
            {
                Setters =
                {
                    new Setter(Grid.RowProperty,new Binding("Row",BindingMode.OneWay) {Converter = new TableCellRowConverter()}),
                    new Setter(Grid.ColumnProperty,new Binding("Column",BindingMode.OneWay) {Converter = new TableCellColumnConverter()}),
                    new Setter(Grid.RowSpanProperty,new Binding("RowSpan",BindingMode.OneWay) {Converter = new TableCellRowSpanConverter()}),
                    new Setter(Grid.ColumnSpanProperty,new Binding("ColumnSpan",BindingMode.OneWay) {Converter = new TableCellColumnSpanConverter()}),
                    new Setter(ZIndexProperty,1),
                }
            };
            this.Styles.Add(_cellStyle);
        }

        protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnLoaded(e);

            CreateTableBorder();
        }

        private void UpdateGridLayout()
        {
            if (_gridPanel == null) return;

            _gridPanel.RowDefinitions.Clear();
            _gridPanel.ColumnDefinitions.Clear();
            // create cell
            CreateGridStructure();
        }
        private void CreateGridStructure()
        {
            // Init GridRow Line = 1
            for (int i = 0; i < Rows; i++)
            {
                _gridPanel!.RowDefinitions.Add(new RowDefinition(1d, GridUnitType.Pixel));
                _gridPanel!.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            }
            _gridPanel!.RowDefinitions.Add(new RowDefinition(1d, GridUnitType.Pixel));

            for (int i = 0; i < Columns; i++)
            {
                _gridPanel!.ColumnDefinitions.Add(new ColumnDefinition(1d, GridUnitType.Pixel));
                _gridPanel!.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }
            _gridPanel!.ColumnDefinitions.Add(new ColumnDefinition(1d, GridUnitType.Pixel));
        }


        private void CreateTableBorder()
        {
            int totalRow = _gridPanel!.RowDefinitions.Count;
            int totalColumn = _gridPanel!.ColumnDefinitions.Count;

            // row Border
            //for (int rowIndex = 0 ; rowIndex < totalRow; rowIndex += 2)
            //{
            //    var rectangle = new Rectangle()
            //    {
            //        Fill = Brushes.Black,
            //        Height = 1,
            //    };
            //    Grid.SetRow(rectangle, rowIndex);
            //    Grid.SetColumn(rectangle, 0);
            //    Grid.SetColumnSpan(rectangle, totalColumn);
            //    _gridPanel.Children.Add(rectangle);
            //}

            int[] index = { 0, 2, totalRow - 1 };

            for (int i = 0; i < index.Length; i++)
            {
                var rectangle = new Rectangle()
                {
                    Fill = Brushes.Black,
                    Height = 1,
                };
                Grid.SetRow(rectangle, index[i]);
                Grid.SetColumn(rectangle, 0);
                Grid.SetColumnSpan(rectangle, totalColumn);
                _gridPanel.Children.Add(rectangle);
            }

            // column boder
            for (int columnIndex = 0; columnIndex < totalColumn; columnIndex += 2)
            {
                var rectangle = new Rectangle()
                {
                    Fill = Brushes.Black,
                    Width = 1,
                };
                Grid.SetRow(rectangle, 0);
                Grid.SetColumn(rectangle, columnIndex);
                Grid.SetRowSpan(rectangle, totalRow);
                _gridPanel.Children.Add(rectangle);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateGridLayout();
            return base.ArrangeOverride(finalSize);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            Dispose();
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
