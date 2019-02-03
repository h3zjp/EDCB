using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace EpgTimer.UserCtrlView
{
    /// <summary>
    /// Sort by ColumnHeader on ColumnHeader Click Event
    /// Set Sort Property Name to ColumnHeader Tag
    /// MultiCollumn Sort: Control + Click
    /// </summary>
    public class ListViewSrt : ListView
    {

        ListCollectionView _viewSource;
        Dictionary<GridViewColumnHeader, SortDescription> _sortDescriptionDict = new Dictionary<GridViewColumnHeader, SortDescription>();
        Dictionary<GridViewColumnHeader, SortAdorner> _sortAdornerDict = new Dictionary<GridViewColumnHeader, SortAdorner>();

        #region - Constructor -
        #endregion

        public ListViewSrt()
        {
            Loaded += ListViewSrt_Loaded;
            AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
        }

        #region - Method -
        #endregion

        public void setColumnHeaderToolTip(GridView gridView)
        {
            foreach (GridViewColumn info in gridView.Columns)
            {
                GridViewColumnHeader header = info.Header as GridViewColumnHeader;
                if (header.ToolTip == null)
                {
                    header.ToolTip = "Ctrl+Click(マルチ・ソート)、Shift+Click(解除)";
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void clearSortDescriptions()
        {
            if (_viewSource == null) { return; }
            //
            _viewSource.SortDescriptions.Clear();
            _sortDescriptionDict.Clear();
            foreach (var item in _sortAdornerDict)
            {
                AdornerLayer.GetAdornerLayer(item.Key).Remove(item.Value);
            }
            _sortAdornerDict.Clear();
        }

        #region - Property -
        #endregion

        #region - Event Handler -
        #endregion

        private void ListViewSrt_Loaded(object sender, RoutedEventArgs e)
        {
            _viewSource = (ListCollectionView)CollectionViewSource.GetDefaultView(ItemsSource);
        }

        void ColumnHeader_Click(Object sender, RoutedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                this.clearSortDescriptions();
                return;
            }
            //
            GridViewColumnHeader columnHeader1 = e.OriginalSource as GridViewColumnHeader;
            if (columnHeader1 == null) { return; }
            if (columnHeader1.Role != GridViewColumnHeaderRole.Padding)
            {
                string propertyName1 = columnHeader1.Tag as string;
                if (string.IsNullOrEmpty(propertyName1))
                {
                    System.Diagnostics.Trace.WriteLine("ERROR: ColumnHeader_Click(): propertyName1 == null");
                    return;
                }
                //
                ListSortDirection sortDirection1 = ListSortDirection.Ascending;
                {
                    SortDescription sd1;
                    if (_sortDescriptionDict.TryGetValue(columnHeader1, out sd1))
                    {
                        if (sd1.Direction == ListSortDirection.Ascending)
                        {
                            sortDirection1 = ListSortDirection.Descending;
                        }
                    }
                }
                // clear if Not Control key pressed
                if (Keyboard.Modifiers != ModifierKeys.Control)
                {
                    this.clearSortDescriptions();
                }
                // SortDescription
                int idx_SortDescription1 = _viewSource.SortDescriptions.Count;
                {
                    SortDescription sd1;
                    if (_sortDescriptionDict.TryGetValue(columnHeader1, out sd1))
                    {
                        idx_SortDescription1 = _viewSource.SortDescriptions.IndexOf(sd1);
                        _viewSource.SortDescriptions.Remove(sd1);
                    }
                }
                {
                    SortDescription sd2 = new SortDescription(propertyName1, sortDirection1);
                    _sortDescriptionDict[columnHeader1] = sd2;
                    _viewSource.SortDescriptions.Insert(idx_SortDescription1, sd2);
                }
                // SortAdorner
                {
                    SortAdorner sa1;
                    if (_sortAdornerDict.TryGetValue(columnHeader1, out sa1))
                    {
                        AdornerLayer.GetAdornerLayer(columnHeader1).Remove(sa1);
                    }
                }
                {
                    SortAdorner sa2 = new SortAdorner(columnHeader1, sortDirection1);
                    _sortAdornerDict[columnHeader1] = sa2;
                    AdornerLayer.GetAdornerLayer(columnHeader1).Add(sa2);
                }
            }
        }

    }

    /// <summary>
    /// show Icon of SortDirection in ColumnHeader
    /// </summary>
    public class SortAdorner : Adorner
    {

        static Geometry _ascGeometry = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");
        static Geometry _descGeometry = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");
        static Pen _pen = new Pen(Brushes.LightSlateGray, 1);

        #region - Constructor -
        #endregion

        public SortAdorner(UIElement element, ListSortDirection dir)
                : base(element)
        {
            this.Direction = dir;
        }

        #region - Method -
        #endregion

        #region - Property -
        #endregion

        public ListSortDirection Direction { get; private set; }

        #region - Event Handler -
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (AdornedElement.RenderSize.Width < 20) { return; }

            TranslateTransform transform1 = new TranslateTransform
                (
                AdornedElement.RenderSize.Width - 10,
                (AdornedElement.RenderSize.Height - 5) / 2
                );
            drawingContext.PushTransform(transform1);

            Geometry geometry1 = _ascGeometry;
            if (this.Direction == ListSortDirection.Descending)
            {
                geometry1 = _descGeometry;
            }
            drawingContext.DrawGeometry(Brushes.LightSlateGray, _pen, geometry1);

            drawingContext.Pop();
        }

    }

}
