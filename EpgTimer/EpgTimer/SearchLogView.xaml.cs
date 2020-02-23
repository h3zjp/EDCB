using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EpgTimer.UserCtrlView;
using EpgTimer.Common;
using EpgTimer.DefineClass;
namespace EpgTimer
{
    /// <summary>
    /// SearchLog.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchLogView : UserControl
    {

        DB_SearchLog _db_SearchLog = new DB_SearchLog();
        DB_SearchLogTab _db_SearchLogTab = new DB_SearchLogTab();
        RecLogView _recLogView;
        List<TabItem> _tabItems = new List<TabItem>();
        List<TabItem> _tabItems_SearchLog = new List<TabItem>();
        List<SearchLogTabItem> _searchLogTabs = new List<SearchLogTabItem>();
        TabItem _tabItem_Add;
        TabItem _tab_Dragging = null;
        bool _isUpdateView_Doing = false;

        #region - Constructor -
        #endregion

        public SearchLogView()
        {
            InitializeComponent();
            //
            _tabItem_Add = new TabItem()
            {
                Header = " + "
            };
            _tabItems.Add(_tabItem_Add);
            tabControl.DataContext = _tabItems;
        }

        #region - Method -
        #endregion

        public void showHelp(bool isVisible0 = true)
        {
            if (isVisible0)
            {
                {
                    tabControl.Visibility = Visibility.Collapsed;
                    richTextBox.Visibility = Visibility.Visible;
                    if (Settings.Instance.RecLog_SearchLog_IsEnabled)
                    {
                        button_Close.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        button_Close.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                if (Settings.Instance.RecLog_SearchLog_IsEnabled)
                {
                    tabControl.Visibility = Visibility.Visible;
                    richTextBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void RefreshSetting()
        {
            foreach (var tab1 in _searchLogTabs)
            {
                tab1.setServiceListView();
            }
        }

        public void update_EpgData()
        {
            foreach (var item1 in _searchLogTabs)
            {
                item1.update_EpgData();
            }
        }

        public void update_ReserveInfo()
        {
            foreach (var item1 in _searchLogTabs)
            {
                item1.update_ReserveInfo();
            }
        }

        public void Init(RecLogView recLogView0)
        {
            _recLogView = recLogView0;
            //
            if (Settings.Instance.RecLog_SearchLog_IsEnabled)
            {
                initSearchLog();
            }
        }

        public void initSearchLog()
        {
            // create DBTable
            if (_db_SearchLogTab.notExistsTable())
            {
                _db_SearchLog.createTables();
                _db_SearchLogTab.createTable();
            }
            //  alterTable
            if (_db_SearchLog.createNotWordTableIfNotExists())
            {
                System.Diagnostics.Trace.WriteLine("DB Table searchLogNotWord created.");
            }
            if (_db_SearchLog.alterTable_NotWord())
            {
                System.Diagnostics.Trace.WriteLine("DB Table searchLogNotWord changed.");
            }
        }

        void updateView()
        {
            if (_isUpdateView_Doing) { return; }
            _isUpdateView_Doing = true;
            //
            bool isUpdated1 = false;
            List<SearchLogTabInfo> tabInfoList1 = _db_SearchLogTab.select();
            if (tabInfoList1.Count != _searchLogTabs.Count)
            {
                isUpdated1 = true;
            }
            else
            {
                foreach (var tabInfo1 in tabInfoList1)
                {
                    bool exist1 = false;
                    foreach (var searchLogTab1 in _searchLogTabs)
                    {
                        if (searchLogTab1.tabInfo.ID == tabInfo1.ID)
                        {
                            exist1 = true;
                            break;
                        }
                    }
                    if (!exist1)
                    {
                        isUpdated1 = true;
                        break;
                    }
                }
            }
            //
            if (isUpdated1)
            {
                _tabItems_SearchLog.Clear();
                _searchLogTabs.Clear();
                _tabItems.Clear();
                _tabItems.Add(_tabItem_Add);
                addSearchLogTabItem(tabInfoList1, 0);
                if (_searchLogTabs.Count == 0)
                {
                    addSearchLogTabItem();
                }
            }
            //
            _isUpdateView_Doing = false;
        }

        void addSearchLogTabItem()
        {
            long nextTabID1 = 1;
            if (0 < _searchLogTabs.Count)
            {
                nextTabID1 = _searchLogTabs.OrderBy(x1 => x1.tabInfo.ID).Last().tabInfo.ID + 1;
            }
            SearchLogTabInfo tabInfo1 = new SearchLogTabInfo()
            {
                header = "タブ" + nextTabID1,
                tabOrder = (int)nextTabID1
            };
            _db_SearchLogTab.insert(tabInfo1);
            addSearchLogTabItem(
                new List<SearchLogTabInfo>() { tabInfo1 });
        }

        void addSearchLogTabItem(List<SearchLogTabInfo> tabInfoList0, int selectedIndex0 = -1)
        {
            if (tabInfoList0.Count == 0) { return; }
            //
            foreach (var tabInfo1 in tabInfoList0)
            {
                SearchLogTabItem searchLogTab1 = new SearchLogTabItem()
                {
                    searchLogView = this,
                    tabInfo = tabInfo1,
                    tabItems = _tabItems_SearchLog,
                    db_SearchLog = _db_SearchLog,
                    db_RecLog = _recLogView.db_RecLog
                };
                _searchLogTabs.Add(searchLogTab1);
                //
                TabItem tab1 = new TabItem()
                {
                    Header = " " + tabInfo1.header + " ",
                };
                tab1.PreviewMouseDown += TabItem_PreviewMouseDown;
                tab1.MouseEnter += TabItem_MouseEnter;
                tab1.ContextMenu = new ContextMenu();
                MenuItem menu_Header1 = new MenuItem();
                menu_Header1.Header = "ヘッダを変更(_C)";
                menu_Header1.Click += delegate
                {
                    searchLogTab1.showTagHeaderEditor();
                };
                tab1.ContextMenu.Items.Add(menu_Header1);
                //
                MenuItem menu_DeleteTab1 = new MenuItem();
                menu_DeleteTab1.Header = "タブを削除(_D)";
                menu_DeleteTab1.Click += delegate
                {
                    if (_searchLogTabs.Count == 1)
                    {
                        MessageBox.Show("最後の１つは削除できません。");
                        return;
                    }
                    else if (MessageBox.Show("タブに登録した全データが削除されます。\nよろしいですか？", "タブの削除", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel) == MessageBoxResult.OK)
                    {
                        searchLogTab1.deleteSearchLogItem_All();
                        _db_SearchLogTab.delete(searchLogTab1.tabInfo.ID);
                        _searchLogTabs.Remove(searchLogTab1);
                        _tabItems.Remove(tab1);
                        _tabItems_SearchLog.Remove(tab1);
                        tabControl.Items.Refresh();
                        tabControl.SelectedIndex = _tabItems.Count - 2;
                    }
                };
                tab1.ContextMenu.Items.Add(menu_DeleteTab1);

                tab1.Content = searchLogTab1;
                _tabItems.Insert(_tabItems.Count - 1, tab1);
                _tabItems_SearchLog.Add(tab1);
                //
                searchLogTab1.tabHeaderChanged += delegate
                {
                    tab1.Header = " " + searchLogTab1.tabInfo.header + " ";
                    _db_SearchLogTab.update(searchLogTab1.tabInfo);
                };
            }
            //
            if (selectedIndex0 < 0)
            {
                selectedIndex0 = _tabItems.Count - 2;
            }
            tabControl.SelectedIndex = selectedIndex0;
        }

        #region - Property -
        #endregion

        #region - Event Handler -
        #endregion

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl.Items.Count < 2) { return; }
            //
            if (tabControl.SelectedItem == _tabItem_Add)
            {
                addSearchLogTabItem();
            }
        }

        /// <summary>
        /// Drag  TabItem 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabItem_PreviewMouseDown(object sender, MouseEventArgs e)
        {
            TabItem tab1 = sender as TabItem;
            if (tab1 == _tabItem_Add) { return; }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _tab_Dragging = tab1;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                tabControl.SelectedItem = tab1;
            }
        }

        /// <summary>
        /// Drag  TabItem 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TabItem tab1 = sender as TabItem;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_tab_Dragging == null) { return; }
                if (tab1 == _tabItem_Add) { return; }
                //
                if (tab1 != _tab_Dragging)
                {
                    int index1 = _tabItems.IndexOf(tab1);
                    _tabItems.Remove(_tab_Dragging);
                    _tabItems.Insert(index1, _tab_Dragging);
                    tabControl.Items.Refresh();
                    _tabItems_SearchLog.Remove(_tab_Dragging);
                    _tabItems_SearchLog.Insert(index1, _tab_Dragging);
                    //
                    SearchLogTabItem slti1 = (SearchLogTabItem)tab1.Content;
                    slti1.tabInfo.tabOrder = _tabItems.IndexOf(tab1) + 1;
                    SearchLogTabItem slti2 = (SearchLogTabItem)_tab_Dragging.Content;
                    slti2.tabInfo.tabOrder = _tabItems.IndexOf(_tab_Dragging) + 1;
                    _db_SearchLogTab.update(
                        new List<SearchLogTabInfo>() { slti1.tabInfo, slti2.tabInfo });
                }
            }
            else if (_tab_Dragging != null)
            {
                _tab_Dragging = null;
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                if (Settings.Instance.RecLog_SearchLog_IsEnabled)
                {
                    updateView();
                    showHelp(false);
                }
                else
                {
                    showHelp(true);
                }
            }
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            showHelp(false);
        }
    }
}