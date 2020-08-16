using EpgTimer.Common;
using EpgTimer.DefineClass;
using EpgTimer.UserCtrlView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Threading;
using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace EpgTimer
{
    /// <summary>
    /// 
    /// </summary>
    public partial class RecLogView
    {

        public enum searchMethods { LIKE, Contrains, Freetext };
        public static readonly string notEnabledMessage = "RecLogが無効に設定されています。";

        MainWindow _mainWindow;
        BackgroundWorker _bgw_Update_ReserveInfo = new BackgroundWorker();
        BackgroundWorker _bgw_Update_RecInfo = new BackgroundWorker();
        BackgroundWorker _bgw_Update_EpgData = new BackgroundWorker();
        ObservableCollection<RecLogItem> _recLogItems = new ObservableCollection<RecLogItem>();
        readonly string _timestampFormat = "MM/dd HH:mm:ss";
        RecLogItem _recLogItem_Edit;
        bool _isReserveInfoChanged = false;
        MenuItem _menu_ReserveChangeOnOff = new MenuItem();
        MenuItem _menu_OpenChgReserveDialog = new MenuItem();

        #region - Constructor -
        #endregion

        public RecLogView()
        {
            InitializeComponent();
            //
            _menu_ReserveChangeOnOff.Header = "簡易予約/有効←→無効(_S)";
            _menu_ReserveChangeOnOff.Click += (object sender, RoutedEventArgs e) =>
            {
                MenuItem menuItem1 = (MenuItem)sender;
                RecLogItem recLogItem1 = (RecLogItem)menuItem1.DataContext;
                recLogItem1.epgEventInfoR.reserveAdd_ChangeOnOff();
            };
            //
            _menu_OpenChgReserveDialog.Header = "予約ダイアログを開く(_O)";
            _menu_OpenChgReserveDialog.Click += (object sender, RoutedEventArgs e) =>
            {
                MenuItem menuItem1 = (MenuItem)sender;
                RecLogItem recLogItem1 = (RecLogItem)menuItem1.DataContext;
                recLogItem1.epgEventInfoR.openReserveDialog(this);
            };
            //
            listView_RecLog.DataContext = _recLogItems;
            comboBox_Edit_Status.DataContext = new object[] {
                RecLogItem.RecodeStatuses.予約済み, RecLogItem.RecodeStatuses.録画完了, RecLogItem.RecodeStatuses.録画異常,  RecLogItem.RecodeStatuses.視聴済み,
                RecLogItem.RecodeStatuses.無効登録, RecLogItem.RecodeStatuses.不明
            };
            //
            db_RecLog = new DB_RecLog(Settings.Instance.RecLog_DB_MachineName, Settings.Instance.RecLog_DB_InstanceName);
            checkBox_RecLogEnabled.IsChecked = Settings.Instance.RecLog_SearchLog_IsEnabled;
            if (!string.IsNullOrWhiteSpace(Settings.Instance.RecLog_DB_MachineName))
            {
                textBox_MachineName.Text = Settings.Instance.RecLog_DB_MachineName;
            }
            else
            {
                textBox_MachineName.Text = Environment.MachineName;
            }
            textBox_InstanceName.Text = Settings.Instance.RecLog_DB_InstanceName;
            searchMethod = Settings.Instance.RecLog_SearchMethod;
            searchColumn = (DB_RecLog.searchColumns)Settings.Instance.RecLog_SearchColumn;
            recodeStatus = (RecLogItem.RecodeStatuses)Settings.Instance.RecLog_RecodeStatus;
            searchResultLimit = Settings.Instance.RecLog_SearchResultLimit;
            textBox_ResultLimit_RecLogWindow.Text = Settings.Instance.RecLogWindow_SearchResultLimit.ToString();
            //
            _bgw_Update_ReserveInfo.DoWork += _bgw_Update_ReserveInfo_DoWork;
            _bgw_Update_ReserveInfo.RunWorkerCompleted += _bgw_Update_ReserveInfo_RunWorkerCompleted;
            _bgw_Update_RecInfo.DoWork += _bgw_RecInfo_DoWork;
            _bgw_Update_EpgData.DoWork += _bgw_EpgData_DoWork;
            //
            grid_Edit.Visibility = Visibility.Collapsed;
            border_Button_DB_ConnectTest.BorderThickness = new Thickness(0);
            if (Settings.Instance.RecLog_SearchLog_IsEnabled)
            {
                border_CheckBox_RecLogEnabled.BorderThickness = new Thickness(0);
                panel_Setting.Visibility = Visibility.Collapsed;
                toggleButton_Setting.IsChecked = false;
                border_ToggleButton_Setting.BorderThickness = new Thickness(0);
            }
            richTextBox_HowTo.Document =
                new FlowDocument(
                    new Paragraph(
                        new Run(this._howto)));
            clearEditor();
            isSearchOptionChanged = false;
        }

        #region - Method -
        #endregion

        void clear_recLogItems()
        {
            this.listView_RecLog.clearSortDescriptions();
            _recLogItems.Clear();
        }

        void search()
        {
            if (!Settings.Instance.RecLog_SearchLog_IsEnabled)
            {
                MessageBox.Show(notEnabledMessage);
                return;
            }
            //
            clear_recLogItems();
            clearEditor();
            //
            if (searchColumn == DB_RecLog.searchColumns.NONE)
            {
                MessageBox.Show("エラー：検索対象を1つ以上選択してください。");
            }
            else
            {
                List<RecLogItem> recLogItemList1 = getRecLogList(textBox_Search.Text, searchResultLimit, recodeStatus, searchColumn);
                foreach (var item in recLogItemList1)
                {
                    _recLogItems.Add(item);
                }
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    foreach (var item in recLogItemList1)
                    {
                        item.isRecFileExist = File.Exists(item.recFilePath);
                    }
                });
            }
        }

        public List<RecLogItem> getRecLogList(string searchWord0, int resultLimit0, RecLogItem.RecodeStatuses recodeStatuse0 = RecLogItem.RecodeStatuses.ALL,
            DB_RecLog.searchColumns searchColumns0 = DB_RecLog.searchColumns.title, EpgContentInfo epgContentInfo0 = null)
        {
            List<RecLogItem> recLogItemList1;
            switch (searchMethod)
            {
                case searchMethods.LIKE:
                    recLogItemList1 = db_RecLog.search_Like(searchWord0, recodeStatuse0, searchColumns0, resultLimit0, epgContentInfo0);
                    break;
                case searchMethods.Contrains:
                    recLogItemList1 = db_RecLog.search_Fulltext(searchWord0, recodeStatuse0, searchColumns0, resultLimit0, epgContentInfo0);
                    break;
                case searchMethods.Freetext:
                    recLogItemList1 = db_RecLog.search_Fulltext(searchWord0, recodeStatuse0, searchColumns0, resultLimit0, epgContentInfo0, true);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return recLogItemList1;
        }

        public void Init(MainWindow mainWindow0)
        {
            _mainWindow = mainWindow0;
        }

        public void update(UpdateNotifyItem notifyItem0)
        {
            if (!Settings.Instance.RecLog_SearchLog_IsEnabled) { return; }
            if (CommonManager.Instance.NWMode == true) { return; }
            //
            switch (notifyItem0)
            {
                case UpdateNotifyItem.EpgData:
                    if (_bgw_Update_EpgData.IsBusy)
                    {
                        ;
                    }
                    else
                    {
                        _bgw_Update_EpgData.RunWorkerAsync();
                    }
                    break;
                case UpdateNotifyItem.ReserveInfo:
                    if (_bgw_Update_ReserveInfo.IsBusy)
                    {
                        ;
                    }
                    else
                    {
                        _bgw_Update_ReserveInfo.RunWorkerAsync();
                    }
                    break;
                case UpdateNotifyItem.RecInfo:
                    if (_bgw_Update_RecInfo.IsBusy)
                    {
                        ;
                    }
                    else
                    {
                        _bgw_Update_RecInfo.RunWorkerAsync();
                    }
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="epgEventInfo0"></param>
        /// <param name="original_network_id0"></param>
        /// <param name="transport_stream_id0"></param>
        /// <param name="service_id0"></param>
        /// <param name="event_id0"></param>
        /// <returns>放送中止？</returns>
        bool getEpgEventInfo(out EpgEventInfo epgEventInfo0, ushort original_network_id0, ushort transport_stream_id0, ushort service_id0, ushort event_id0, DateTime startTime0)
        {
            epgEventInfo0 = null;
            if (event_id0 == ushort.MaxValue) { return false; } // プログラム予約
            bool isBroadCastCancelled1 = false;
            UInt64 key1 = CommonManager.Create64Key(original_network_id0, transport_stream_id0, service_id0);
            if (CommonManager.Instance.DB.ServiceEventList.ContainsKey(key1) == true)
            {
                List<EpgEventInfo> eventList1 = CommonManager.Instance.DB.ServiceEventList[key1].eventList;
                foreach (EpgEventInfo epgEventInfo1 in eventList1)
                {
                    if (epgEventInfo1.event_id == event_id0)
                    {
                        epgEventInfo0 = epgEventInfo1;
                        break;
                    }
                }
                //  1分毎のタイムテーブルを作成してEPGデータの欠落でないことを確認する
                if (epgEventInfo0 == null)
                {
                    Dictionary<DateTime, EpgEventInfo> timeTable1 = new Dictionary<DateTime, EpgEventInfo>();
                    foreach (EpgEventInfo eei1 in eventList1)
                    {
                        DateTime endTime1;
                        if (eei1.DurationFlag == 0)
                        {
                            endTime1 = eei1.start_time;
                        }
                        else
                        {
                            endTime1 = eei1.start_time.AddSeconds(eei1.durationSec);
                        }
                        DateTime date1 = eei1.start_time;
                        do
                        {
                            timeTable1[date1] = eei1;
                            date1 = date1.AddMinutes(1);
                        } while (date1 < endTime1);
                    }
                    if (timeTable1.ContainsKey(startTime0))
                    {
                        isBroadCastCancelled1 = true;
                    }
                }
            }

            return isBroadCastCancelled1;
        }

        void playSelectedItem()
        {
            RecLogItem recLogItem1 = listView_RecLog.SelectedItem as RecLogItem;
            if (recLogItem1 != null)
            {
                CommonManager.Instance.FilePlay(recLogItem1.recFilePath);
            }
        }

        void deleteRecLogItems()
        {
            StringBuilder sb1 = new StringBuilder();
            sb1.AppendLine("録画ログを削除します");
            sb1.AppendLine("削除アイテム数：" + listView_RecLog.SelectedItems.Count);
            //foreach (RecLogItem item in listView_RecLog.SelectedItems)
            //{
            //    sb1.AppendLine("・" + item.tvProgramTitle);
            //}
            if (MessageBox.Show(sb1.ToString(), "録画ログの削除", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK) == MessageBoxResult.OK)
            {
                List<RecLogItem> recLogItems1 = new List<RecLogItem>();
                foreach (RecLogItem item in listView_RecLog.SelectedItems)
                {
                    recLogItems1.Add(item);
                }
                foreach (var item in recLogItems1)
                {
                    _recLogItems.Remove(item);
                }
                db_RecLog.delete(recLogItems1);
            }
        }

        void clearEditor()
        {
            _recLogItem_Edit = null;
            //
            textBox_Edit_ProgramTitle.Clear();
            label_Editor_Date.Content = null;
            label_Editor_ServiceName.Content = null;
            comboBox_Edit_Status.SelectedItem = null;
            checkBox_AllowOverWrite.IsChecked = false;
            textBox_RecFilePath.Clear();
            richTextBox_Comment.Document.Blocks.Clear();
            richTextBox_ShortInfo_text_char.Document.Blocks.Clear();
            richTextBox_ExtInfo_text_char.Document.Blocks.Clear();
            //
            textBox_Edit_ProgramTitle.BorderThickness = new Thickness(0);
            textBox_RecFilePath.BorderThickness = new Thickness(0);
            border_RecStatus.BorderThickness = new Thickness(0);
            border_AllowOverWrite.BorderThickness = new Thickness(0);
            richTextBox_Comment.BorderThickness = new Thickness(0);
            richTextBox_ShortInfo_text_char.BorderThickness = new Thickness(0);
            richTextBox_ExtInfo_text_char.BorderThickness = new Thickness(0);
        }

        void setRecLogItem2editor()
        {
            RecLogItem recLogItem_Edit1 = listView_RecLog.SelectedItem as RecLogItem;
            if (recLogItem_Edit1 == null) { return; }
            //
            clearEditor();
            grid_Edit.Visibility = Visibility.Visible;
            //
            textBox_Edit_ProgramTitle.Text = recLogItem_Edit1.epgEventInfoR.ShortInfo.event_name;
            label_Editor_Date.Content = CommonManager.ConvertTimeText(
                recLogItem_Edit1.epgEventInfoR.start_time, recLogItem_Edit1.epgEventInfoR.durationSec, false, false, true);
            label_Editor_ServiceName.Content = recLogItem_Edit1.tvStationName +
                " (" + CommonManager.ConvertNetworkNameText(recLogItem_Edit1.epgEventInfoR.original_network_id) + ")";
            comboBox_Edit_Status.SelectedItem = recLogItem_Edit1.recodeStatus;
            checkBox_AllowOverWrite.IsChecked = recLogItem_Edit1.epgAllowOverWrite;
            textBox_RecFilePath.Text = recLogItem_Edit1.recFilePath;
            setText(richTextBox_Comment, recLogItem_Edit1.comment);
            setText(richTextBox_ShortInfo_text_char, recLogItem_Edit1.epgEventInfoR.ShortInfo.text_char);
            setText(richTextBox_ExtInfo_text_char, recLogItem_Edit1.epgEventInfoR.ExtInfo.text_char);
            //
            _recLogItem_Edit = recLogItem_Edit1;
        }

        string getText(RichTextBox richTextBox0)
        {
            return new TextRange(richTextBox0.Document.ContentStart, richTextBox0.Document.ContentEnd).Text;
        }

        void setText(RichTextBox richTextBox0, string text0)
        {
            richTextBox0.Document.Blocks.Add(
                new Paragraph(
                    new Run(text0)));
        }

        void addDBLog(string msg0)
        {
#if DEBUG
            System.Diagnostics.Trace.WriteLine(msg0);
#endif
            listBox_DBLog.Dispatcher.BeginInvoke(
                 new Action(
                     () =>
                     {
                         listBox_DBLog.Items.Insert(0, msg0);
                         if (30 < listBox_DBLog.Items.Count)
                         {
                             listBox_DBLog.Items.RemoveAt(listBox_DBLog.Items.Count - 1);
                         }
                     }));
        }

        bool dbConnectTest()
        {
            addDBLog("DB接続テスト");
            bool isSuccess1 = false;
#if DEBUG
            bool isTestOnly1 = false;
#else
            bool isTestOnly1 = CommonManager.Instance.NWMode;  // NWModeからはテストのみ
#endif
            DB.connectTestResults connectTestResult1 = db_RecLog.connectionTest(isTestOnly1);
            switch (connectTestResult1)
            {
                case DB.connectTestResults.success:
                    addDBLog("成功");
                    isSuccess1 = true;
                    break;
                case DB.connectTestResults.createDB:
                    if (isTestOnly1)
                    {
                        addDBLog("データベースEDCBが見つかりません");
                    }
                    else
                    {
                        addDBLog("データベースを新規作成");
                        System.Threading.Thread.Sleep(5000);    // データベース作成完了を待機
                        db_RecLog.createTable_RecLog_EpgEventInfo();
                    }
                    isSuccess1 = true;
                    break;
                case DB.connectTestResults.serverNotFound:
                    addDBLog("SQLServerが見つかりません");
                    isSuccess1 = false;
                    break;
                case DB.connectTestResults.unKnownError:
                    addDBLog("Unknown Error");
                    isSuccess1 = false;
                    break;
            }
            //
            if (isSuccess1)
            {
                _mainWindow.searchLogView.initSearchLog();
            }

            return isSuccess1;
        }

        void updateReserveInfo()
        {
            DateTime lastUpdate1 = DateTime.Now;
            //
            int reservedCount_New1 = 0;
            int reservedCount_Update1 = 0;
            foreach (ReserveData rd1 in CommonManager.Instance.DB.ReserveList.Values)
            {
                if (rd1.EventID == ushort.MaxValue) { continue; }   // プログラム予約 => スキップ
                //
                EpgEventInfo epgEventInfo1;
                getEpgEventInfo(out epgEventInfo1, rd1.OriginalNetworkID, rd1.TransportStreamID, rd1.ServiceID, rd1.EventID, rd1.StartTime);
                RecLogItem recLogItem1 = db_RecLog.exists(rd1);
                if (recLogItem1 == null)
                {
                    // 新規登録
                    if (epgEventInfo1 == null)
                    {
                        System.Diagnostics.Trace.WriteLine("RecLogView.updateReserveInfo(): 新規登録: epgEventInfo1 == null" + " - Title: " + rd1.Title);
                    }
                    else
                    {
                        reservedCount_New1++;
                        RecLogItem recLogItem2 = new RecLogItem()
                        {
                            lastUpdate = lastUpdate1,
                            epgEventInfoR = new EpgEventInfoR(epgEventInfo1, lastUpdate1)
                        };
                        if (rd1.RecSetting.RecMode == 0x05)   // 録画モード：無効
                        {
                            recLogItem2.recodeStatus = RecLogItem.RecodeStatuses.無効登録;
                        }
                        else
                        {
                            recLogItem2.recodeStatus = RecLogItem.RecodeStatuses.予約済み;
                        }
                        db_RecLog.insert(recLogItem2);
                    }
                }
                else
                {
                    //更新
                    reservedCount_Update1++;
                    recLogItem1.lastUpdate = lastUpdate1;
                    if (epgEventInfo1 == null)
                    {
                        System.Diagnostics.Trace.WriteLine("RecLogView.updateReserveInfo(): 更新: epgEventInfo1 == null" + ": " + rd1.Title + " - " + rd1.StartTime);
                    }
                    else if (rd1.RecSetting.RecMode == 0x05)   // 録画モード：無効
                    {
                        if (recLogItem1.recodeStatus != RecLogItem.RecodeStatuses.無効登録)
                        {
                            recLogItem1.recodeStatus = RecLogItem.RecodeStatuses.無効登録;
                            reservedCount_New1++;
                        }
                    }
                    else
                    {
                        if (recLogItem1.recodeStatus != RecLogItem.RecodeStatuses.予約済み)
                        {
                            recLogItem1.recodeStatus = RecLogItem.RecodeStatuses.予約済み;
                            reservedCount_New1++;
                        }
                    }
                    db_RecLog.update(recLogItem1);
                }
            }
            //
            // 予約削除 - 更新されなかったものを対象とする
            //
            List<RecLogItem> list_NotUpdated1 = db_RecLog.select_Reserved_NotUpdated(lastUpdate1);
            List<RecLogItem> list_Deleted1 = new List<RecLogItem>();
            List<RecLogItem> list_RecstatusUpdateErr1 = new List<RecLogItem>();
            foreach (RecLogItem item1 in list_NotUpdated1)
            {
                if (item1.epgEventInfoR != null && lastUpdate1 < item1.epgEventInfoR.start_time)
                {   // 未来に放送
                    list_Deleted1.Add(item1);
                }
                else if (item1.recodeStatus == RecLogItem.RecodeStatuses.無効登録)
                {
                    // 無効登録を削除
                    list_Deleted1.Add(item1);
                }
                else
                {
                    // 録画完了？
                    list_RecstatusUpdateErr1.Add(item1);
                }
            }
            //
            if (0 < list_RecstatusUpdateErr1.Count)
            {
                addDBLog("ステータス更新失敗：" + list_RecstatusUpdateErr1.Count);
                foreach (RecLogItem logItem1 in list_RecstatusUpdateErr1)
                {
                    RecFileInfo rfi1 = null;
                    foreach (RecFileInfo rfi2 in recFileInfoes)
                    {
                        if (logItem1.equals(rfi2))
                        {
                            rfi1 = rfi2;
                            break;
                        }
                    }
                    if (rfi1 != null)
                    {
                        if ((RecEndStatus)rfi1.RecStatus == RecEndStatus.NORMAL)
                        {
                            logItem1.recodeStatus = RecLogItem.RecodeStatuses.録画完了;
                        }
                        else
                        {
                            logItem1.recodeStatus = RecLogItem.RecodeStatuses.録画異常;
                        }
                    }
                    else
                    {
                        logItem1.recodeStatus = RecLogItem.RecodeStatuses.不明;
                    }
                    logItem1.lastUpdate = lastUpdate1;
                    db_RecLog.update(logItem1);
                }
            }
            //
            int reservedCount_Removed1 = db_RecLog.delete(list_Deleted1.ToArray());
            //
            addDBLog("予約更新(+" + reservedCount_New1 + ",-" + reservedCount_Removed1 + ") " + lastUpdate1.ToString(_timestampFormat));
            //
            if (0 < reservedCount_New1 || 0 < reservedCount_Removed1)
            {
                _isReserveInfoChanged = true;
            }
        }

        void hideEditor()
        {
            clearEditor();
            grid_Edit.Visibility = Visibility.Collapsed;
        }

        void changeRecordStatus(RecLogItem.RecodeStatuses status0)
        {
            List<RecLogItem> list1 = new List<RecLogItem>();
            foreach (RecLogItem item in listView_RecLog.SelectedItems)
            {
                item.recodeStatus = status0;
                list1.Add(item);
            }
            db_RecLog.update(list1);
        }

        #region - Property -
        #endregion

        public DB_RecLog db_RecLog { get; private set; }

        DB_RecLog.searchColumns searchColumn
        {
            get { return _searchColumn; }
            set
            {
                _searchColumn = value;
                checkBox_Search_Title.IsChecked = value.HasFlag(DB_RecLog.searchColumns.title);
                checkBox_Search_Content.IsChecked = value.HasFlag(DB_RecLog.searchColumns.content);
                checkBox_Search_Comment.IsChecked = value.HasFlag(DB_RecLog.searchColumns.comment);
                checkBox_Search_RecFileName.IsChecked = value.HasFlag(DB_RecLog.searchColumns.recFilePath);
            }
        }
        DB_RecLog.searchColumns _searchColumn = DB_RecLog.searchColumns.NONE;

        RecLogItem.RecodeStatuses recodeStatus
        {
            get { return _recodeStatus; }
            set
            {
                _recodeStatus = value;
                checkBox_RecStatus_Reserved.IsChecked = value.HasFlag(RecLogItem.RecodeStatuses.予約済み);
                checkBox_RecStatus_Recoded.IsChecked = value.HasFlag(RecLogItem.RecodeStatuses.録画完了);
                checkBox_RecStatus_Recoded_Abnormal.IsChecked = value.HasFlag(RecLogItem.RecodeStatuses.録画異常);
                checkBox_RecStatus_Viewed.IsChecked = value.HasFlag(RecLogItem.RecodeStatuses.視聴済み);
                checkBox_RecStatus_Reserved_Null.IsChecked = value.HasFlag(RecLogItem.RecodeStatuses.無効登録);
                checkBox_RecStatus_Cancelled.IsChecked = value.HasFlag(RecLogItem.RecodeStatuses.放送中止);
                checkBox_RecStatus_Unkown.IsChecked = value.HasFlag(RecLogItem.RecodeStatuses.不明);
            }
        }
        RecLogItem.RecodeStatuses _recodeStatus = RecLogItem.RecodeStatuses.NONE;

        bool isSearchOptionChanged
        {
            get { return _isSearchOptionChanged; }
            set
            {
                _isSearchOptionChanged = value;
                if (border_Button_SaveSearchOption != null)
                {
                    if (value)
                    {
                        border_Button_SaveSearchOption.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        border_Button_SaveSearchOption.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        bool _isSearchOptionChanged = false;

        int searchResultLimit
        {
            get
            {
                int searchResultLimit1;
                if (int.TryParse(textBox_ResultLimit.Text, out searchResultLimit1))
                {
                    return searchResultLimit1;
                }
                else
                {
                    return Settings.Instance.RecLog_SearchResultLimit;
                }
            }
            set { textBox_ResultLimit.Text = value.ToString(); }
        }

        searchMethods searchMethod
        {
            get
            {
                if (radioButton_SearchMethod_Like.IsChecked == true)
                {
                    return searchMethods.LIKE;
                }
                else if (radioButton_SearchMethod_Contains.IsChecked == true)
                {
                    return searchMethods.Contrains;
                }
                else if (radioButton_SearchMethod_Freetext.IsChecked == true)
                {
                    return searchMethods.Freetext;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            set
            {
                switch (value)
                {
                    case searchMethods.LIKE:
                        radioButton_SearchMethod_Like.IsChecked = true;
                        break;
                    case searchMethods.Contrains:
                        radioButton_SearchMethod_Contains.IsChecked = true;
                        break;
                    case searchMethods.Freetext:
                        radioButton_SearchMethod_Freetext.IsChecked = true;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        IEnumerable<RecFileInfo> recFileInfoes
        {
            get
            {
                ErrCode err = CommonManager.Instance.DB.ReloadRecFileInfo();
                if (CommonManager.CmdErrMsgTypical(err, "録画情報の取得") == false)
                {
                    return new RecFileInfo[0];
                }

                return CommonManager.Instance.DB.RecFileInfo.Values;
            }
        }

        #region - Event Handler -
        #endregion

        /// <summary>
        /// EPGデータ更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _bgw_EpgData_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime lastUpdate1 = DateTime.Now;
            //
            int epgUpdatedCount1 = 0;
            int epgNotUpdatedCount1 = 0;
            foreach (RecLogItem rli1 in db_RecLog.select_Reserved())
            {
                if (rli1.epgEventInfoR != null && rli1.epgAllowOverWrite)
                {
                    EpgEventInfo epgEventInfo1;
                    bool isBroadCastCancelled1 = getEpgEventInfo(out epgEventInfo1, rli1.epgEventInfoR.original_network_id, rli1.epgEventInfoR.transport_stream_id, rli1.epgEventInfoR.service_id, rli1.epgEventInfoR.event_id, rli1.epgEventInfoR.start_time);
                    if (epgEventInfo1 == null)
                    {
                        if (isBroadCastCancelled1)
                        {
                            addDBLog("放送中止: " + rli1.epgEventInfoR.Title);
                            if (rli1.recodeStatus != RecLogItem.RecodeStatuses.無効登録)
                            {
                                rli1.recodeStatus = RecLogItem.RecodeStatuses.放送中止;
                                db_RecLog.update(rli1);
                            }
                        }
                        else
                        {
                            addDBLog("NOEPG: " + rli1.epgEventInfoR.Title);
                        }
                    }
                    else
                    {
                        EpgEventInfoR epgEventInfoR1 = new EpgEventInfoR(epgEventInfo1, lastUpdate1);
                        epgUpdatedCount1++;
                        epgEventInfoR1.ID = rli1.epgEventInfoID;
                        epgEventInfoR1.lastUpdate = lastUpdate1;
                        db_RecLog.updateEpg(epgEventInfoR1);
                    }
                }
                else
                {
                    epgNotUpdatedCount1++;
                }
            }
            addDBLog("EPG更新(+" + epgUpdatedCount1 + ",-" + epgNotUpdatedCount1 + ") " + lastUpdate1.ToString(_timestampFormat));
            //
            // 放送中止による予約削除
            //
            List<ReserveData> cancelledList1 = new List<ReserveData>();
            foreach (ReserveData rd1 in CommonManager.Instance.DB.ReserveList.Values)
            {
                EpgEventInfo epgEventInfo1;
                bool isBroadCastCancelled1 = getEpgEventInfo(out epgEventInfo1, rd1.OriginalNetworkID, rd1.TransportStreamID, rd1.ServiceID, rd1.EventID, rd1.StartTime);
                if (isBroadCastCancelled1)
                {
                    if (rd1.StartTime.AddMinutes(-5) < DateTime.Now) // EpgDataCap_Bon.exeの起動に要するマージンを考慮する必要がある?
                    {
                        // 既に始まっているものは除外
                        System.Diagnostics.Trace.WriteLine("予約削除中止：録画中");
                    }
                    else
                    {
                        addDBLog("予約削除: " + rd1.Title);
                        cancelledList1.Add(rd1);
                    }
                }
            }
            if (0 < cancelledList1.Count)
            {
                addDBLog("放送中止予約削除: " + cancelledList1.Count);
                MenuUtil.ReserveDelete(cancelledList1);
            }
        }

        /// <summary>
        /// 録画済更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _bgw_RecInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime lastUpdate1 = DateTime.Now;
            //
            int recordedCount_New1 = 0;
            foreach (RecFileInfo rfi1 in recFileInfoes)
            {
                RecLogItem recLogItem1 = db_RecLog.exists(RecLogItem.RecodeStatuses.予約済み, rfi1.OriginalNetworkID, rfi1.TransportStreamID, rfi1.ServiceID, rfi1.EventID, rfi1.StartTime);
                if (recLogItem1 != null)
                {
                    //  録画済みに変更
                    recordedCount_New1++;
                    if ((RecEndStatus)rfi1.RecStatus == RecEndStatus.NORMAL)
                    {
                        recLogItem1.recodeStatus = RecLogItem.RecodeStatuses.録画完了;
                    }
                    else
                    {
                        recLogItem1.recodeStatus = RecLogItem.RecodeStatuses.録画異常;
                    }
                    recLogItem1.recFilePath = rfi1.RecFilePath;
                    recLogItem1.lastUpdate = lastUpdate1;
                    db_RecLog.update(recLogItem1);
                }
                else
                {
                    //addMessage("*** RecLogItemが見つからない?");
                }
            }
            //
            addDBLog("録画完了(" + recordedCount_New1 + ") " + lastUpdate1.ToString(_timestampFormat));
        }

        private void _bgw_Update_ReserveInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.Sleep(1000); // EPG更新を優先させる
            updateReserveInfo();
        }

        private void _bgw_Update_ReserveInfo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_isReserveInfoChanged)
            {
                _isReserveInfoChanged = false;
                if (IsVisible)
                {
                    if (0 < _recLogItems.Count)
                    {
                        search();
                    }
                }
                else
                {
                    clear_recLogItems();
                }
            }
        }

        private void button_Search_Click(object sender, RoutedEventArgs e)
        {
            search();
        }

        private void menu_RecLog_Del_Click(object sender, RoutedEventArgs e)
        {
            deleteRecLogItems();
        }

        private void menu_RecLog_Play_Click(object sender, RoutedEventArgs e)
        {
            playSelectedItem();
        }

        void listViewItem_RecLog_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            playSelectedItem();
        }

        private void listViewItem_RecLog_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.P:
                        playSelectedItem();
                        break;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.None)
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        setRecLogItem2editor();
                        break;
                    case Key.Delete:
                        deleteRecLogItems();
                        break;
                    case Key.Escape:
                        listView_RecLog.SelectedItem = null;
                        break;
                }
            }
        }

        private void textBox_Search_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    search();
                    break;
            }
        }

        private void button_DB_ConnectTest_Click(object sender, RoutedEventArgs e)
        {
            db_RecLog.setSqlServerMachineName(textBox_MachineName.Text);
            db_RecLog.setSqlServerInstanceName(textBox_InstanceName.Text);
            bool isSuccess1 = dbConnectTest();
            if (isSuccess1)
            {
                Settings.Instance.RecLog_DB_MachineName = textBox_MachineName.Text;
                Settings.Instance.RecLog_DB_InstanceName = textBox_InstanceName.Text;
            }
            border_Button_DB_ConnectTest.BorderThickness = new Thickness();
        }

        private void checkBox_RecLogEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_RecLogEnabled.IsChecked == true)
            {
                if (!Settings.Instance.RecLog_SearchLog_IsEnabled)
                {
                    new BlackoutWindow(Window.GetWindow(this)).showWindow("録画・検索ログを有効化します");
                }
                if (dbConnectTest())
                {
                    border_CheckBox_RecLogEnabled.BorderThickness = new Thickness();
                    //
#if !DEBUG
                    if (CommonManager.Instance.NWMode == false)
#endif
                    {
                        int recFileInfoCount1 = 0;
                        BackgroundWorker bgw1 = new BackgroundWorker();
                        bgw1.RunWorkerCompleted += delegate
                        {
                            addDBLog("準備完了");
                        };
                        bgw1.DoWork += delegate
                        {
                            DateTime lastUpdate1 = DateTime.Now;
                            recFileInfoCount1 = db_RecLog.insert(recFileInfoes, lastUpdate1);
                            StringBuilder sb1 = new StringBuilder();
                            sb1.AppendLine("録画完了リストを登録");
                            sb1.Append("　登録数：" + recFileInfoCount1);
                            addDBLog(sb1.ToString());
                            //
                            updateReserveInfo();
                        };
                        bgw1.RunWorkerAsync();
                    }
                }
                else
                {
                    checkBox_RecLogEnabled.IsChecked = false;
                }
                if (checkBox_RecLogEnabled.IsChecked != true)
                {
                    border_CheckBox_RecLogEnabled.BorderThickness = new Thickness(2);
                }
            }
            //
            Settings.Instance.RecLog_SearchLog_IsEnabled = (bool)checkBox_RecLogEnabled.IsChecked;
        }

        private void richTextBox_Comment_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_recLogItem_Edit == null) { return; }

            richTextBox_Comment.BorderThickness = new Thickness(1);
        }

        private void richTextBox_ShortInfo_text_char_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_recLogItem_Edit == null) { return; }

            richTextBox_ShortInfo_text_char.BorderThickness = new Thickness(1);
        }

        private void comboBox_Edit_Status_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_recLogItem_Edit == null) { return; }

            border_RecStatus.BorderThickness = new Thickness(1);
        }

        private void checkBox_AllowOverWrite_Click(object sender, RoutedEventArgs e)
        {
            if (_recLogItem_Edit == null) { return; }

            border_AllowOverWrite.BorderThickness = new Thickness(1);
        }

        private void richTextBox_ExtInfo_text_char_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_recLogItem_Edit == null) { return; }

            richTextBox_ExtInfo_text_char.BorderThickness = new Thickness(1);
        }

        private void button_Edit_Update_Click(object sender, RoutedEventArgs e)
        {
            if (_recLogItem_Edit == null) { return; }
            //
            _recLogItem_Edit.epgEventInfoR.ShortInfo.event_name = textBox_Edit_ProgramTitle.Text;
            _recLogItem_Edit.recodeStatus = (RecLogItem.RecodeStatuses)comboBox_Edit_Status.SelectedItem;
            _recLogItem_Edit.epgAllowOverWrite = (checkBox_AllowOverWrite.IsChecked == true);
            _recLogItem_Edit.recFilePath = textBox_RecFilePath.Text;
            _recLogItem_Edit.comment = getText(richTextBox_Comment);
            _recLogItem_Edit.epgEventInfoR.ShortInfo.text_char = getText(richTextBox_ShortInfo_text_char);
            _recLogItem_Edit.epgEventInfoR.ExtInfo.text_char = getText(richTextBox_ExtInfo_text_char);
            //
            db_RecLog.edit(_recLogItem_Edit);
            //
            hideEditor();
        }

        private void button_Edit_Cancel_Click(object sender, RoutedEventArgs e)
        {
            hideEditor();
        }

        private void listView_RecLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView_RecLog.SelectedItems.Count == 0 || 1 < listView_RecLog.SelectedItems.Count)
            {
                hideEditor();
            }
            else
            {
                setRecLogItem2editor();
            }
        }

        private void toggleButton_Setting_Click(object sender, RoutedEventArgs e)
        {
            if (toggleButton_Setting.IsChecked == true)
            {
                panel_Setting.Visibility = Visibility.Visible;
                border_ToggleButton_Setting.BorderThickness = new Thickness(2);
                toggleButton_Setting.Content = "設定を閉じる";
            }
            else
            {
                panel_Setting.Visibility = Visibility.Collapsed;
                border_ToggleButton_Setting.BorderThickness = new Thickness(0);
                toggleButton_Setting.Content = "設定";
                //
                switch (searchMethod)
                {
                    case searchMethods.LIKE:
                        Settings.Instance.RecLog_SearchMethod = searchMethods.LIKE;
                        break;
                    case searchMethods.Contrains:
                        Settings.Instance.RecLog_SearchMethod = searchMethods.Contrains;
                        break;
                    case searchMethods.Freetext:
                        Settings.Instance.RecLog_SearchMethod = searchMethods.Freetext;
                        break;
                    default:
                        throw new NotSupportedException(); ;
                }
                int RecLogWindow_SearchResultLimit1;
                if (int.TryParse(textBox_ResultLimit_RecLogWindow.Text, out RecLogWindow_SearchResultLimit1))
                {
                    Settings.Instance.RecLogWindow_SearchResultLimit = RecLogWindow_SearchResultLimit1;
                }
                Settings.SaveToXmlFile();
                addDBLog("設定を保存しました");
            }
        }

        private void textBox_MachineName_KeyDown(object sender, KeyEventArgs e)
        {
            border_Button_DB_ConnectTest.BorderThickness = new Thickness(2);
        }

        private void textBox_InstanceName_KeyDown(object sender, KeyEventArgs e)
        {
            border_Button_DB_ConnectTest.BorderThickness = new Thickness(2);
        }

        private void textBox_ResultLimit_KeyDown(object sender, KeyEventArgs e)
        {
            isSearchOptionChanged = true;
        }

        private void button_SaveSearchOption_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.RecLog_SearchColumn = (int)searchColumn;
            Settings.Instance.RecLog_RecodeStatus = (int)recodeStatus;
            Settings.Instance.RecLog_SearchResultLimit = searchResultLimit;
            //
            Settings.SaveToXmlFile();
            isSearchOptionChanged = false;
            addDBLog("検索オプションを保存しました");

            border_Button_SaveSearchOption.Visibility = Visibility.Collapsed;
        }

        private void button_Reset_Click(object sender, RoutedEventArgs e)
        {
            this.textBox_Search.Clear();
            clear_recLogItems();
        }

        private void menu_RecLog_ChangeStatus_Reserve_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.予約済み);
        }

        private void menu_RecLog_ChangeStatus_Recorded_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.録画完了);
        }

        private void menu_RecLog_ChangeStatus_Error_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.録画異常);
        }

        private void menu_RecLog_ChangeStatus_Viewed_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.視聴済み);
        }

        private void menu_RecLog_ChangeStatus_Disabled_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.無効登録);
        }

        private void menu_RecLog_ChangeStatus_Unknown_Click(object sender, RoutedEventArgs e)
        {
            changeRecordStatus(RecLogItem.RecodeStatuses.不明);
        }

        private void checkBox_RecStatus_All_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox item in panel_RecStatus_CheckBox.Children)
            {
                item.IsChecked = (((CheckBox)sender).IsChecked == true);
            }
            checkBox_RecStatus_Click(sender, e);
        }

        private void checkBox_Search_All_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox item in panel_Search_CheckBox.Children)
            {
                item.IsChecked = (((CheckBox)sender).IsChecked == true);
            }
            checkBox_Search_Click(sender, e);
        }

        private void listViewItem_RecLog_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ListViewItem listViewItem1 = (ListViewItem)sender;
            RecLogItem item1 = (RecLogItem)listViewItem1.Content;
            ContextMenu menu1 = listViewItem1.ContextMenu;
            if (!menu1.Items.Contains(_menu_ReserveChangeOnOff))
            {
                menu1.Items.Add(_menu_ReserveChangeOnOff);
            }
            if (!menu1.Items.Contains(_menu_OpenChgReserveDialog))
            {
                menu1.Items.Add(_menu_OpenChgReserveDialog);
            }
            if (listView_RecLog.SelectedItems.Count == 1
                && (item1.recodeStatus == RecLogItem.RecodeStatuses.予約済み || item1.recodeStatus == RecLogItem.RecodeStatuses.無効登録))
            {
                _menu_ReserveChangeOnOff.IsEnabled = true;
                _menu_OpenChgReserveDialog.IsEnabled = true;
            }
            else
            {
                _menu_ReserveChangeOnOff.IsEnabled = false;
                _menu_OpenChgReserveDialog.IsEnabled = false;
            }
        }

        private void menu_RecLog_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            RecLogItem recLogItem1 = listView_RecLog.SelectedItem as RecLogItem;
            if (recLogItem1 != null)
            {
                if (File.Exists(recLogItem1.recFilePath))
                {
                    FileInfo fi1 = new FileInfo(recLogItem1.recFilePath);
                    System.Diagnostics.Process.Start(fi1.DirectoryName);
                }
                else
                {
                    MessageBox.Show("ファイルが見つかりません。", "失敗", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void menu_RecLog_DeleteRecFile_Click(object sender, RoutedEventArgs e)
        {
            List<RecLogItem> recLogItems1 = new List<RecLogItem>();
            foreach (RecLogItem item1 in listView_RecLog.SelectedItems)
            {
                if (item1.isRecFileExist)
                {
                    recLogItems1.Add(item1);
                }
            }
            if (1 < listView_RecLog.SelectedItems.Count)
            {
                StringBuilder sb1 = new StringBuilder();
                sb1.AppendLine("以下の番組の録画ファイルを削除します。");
                sb1.AppendLine();
                foreach (RecLogItem item1 in listView_RecLog.SelectedItems)
                {
                    if (item1.isRecFileExist)
                    {
                        sb1.AppendLine("・" + item1.tvProgramTitle);
                    }
                }
                if (MessageBox.Show(sb1.ToString(), "複数ファイルの削除", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK) == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            try
            {
                foreach (RecLogItem item1 in recLogItems1)
                {
                    FileSystem.DeleteFile(item1.recFilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    item1.isRecFileExist = File.Exists(item1.recFilePath);
                }
            }
            catch (Exception ex0)
            {
                MessageBox.Show(ex0.Message);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            CommonManager.Instance.FilePlay(e.Uri.OriginalString);
        }

        private void checkBox_RecStatus_Click(object sender, RoutedEventArgs e)
        {
            _recodeStatus = RecLogItem.RecodeStatuses.NONE;
            if (checkBox_RecStatus_Reserved.IsChecked == true)
            {
                _recodeStatus |= RecLogItem.RecodeStatuses.予約済み;
            }
            if (checkBox_RecStatus_Recoded.IsChecked == true)
            {
                _recodeStatus |= RecLogItem.RecodeStatuses.録画完了;
            }
            if (checkBox_RecStatus_Recoded_Abnormal.IsChecked == true)
            {
                _recodeStatus |= RecLogItem.RecodeStatuses.録画異常;
            }
            if (checkBox_RecStatus_Viewed.IsChecked == true)
            {
                _recodeStatus |= RecLogItem.RecodeStatuses.視聴済み;
            }
            if (checkBox_RecStatus_Reserved_Null.IsChecked == true)
            {
                _recodeStatus |= RecLogItem.RecodeStatuses.無効登録;
            }
            if (checkBox_RecStatus_Cancelled.IsChecked == true)
            {
                _recodeStatus |= RecLogItem.RecodeStatuses.放送中止;
            }
            if (checkBox_RecStatus_Unkown.IsChecked == true)
            {
                _recodeStatus |= RecLogItem.RecodeStatuses.不明;
            }
            //
            isSearchOptionChanged = (_recodeStatus != (RecLogItem.RecodeStatuses)Settings.Instance.RecLog_RecodeStatus); ;
        }

        private void checkBox_Search_Click(object sender, RoutedEventArgs e)
        {
            _searchColumn = DB_RecLog.searchColumns.NONE;
            if (checkBox_Search_Title.IsChecked == true)
            {
                _searchColumn |= DB_RecLog.searchColumns.title;
            }
            if (checkBox_Search_Content.IsChecked == true)
            {
                _searchColumn |= DB_RecLog.searchColumns.content;
            }
            if (checkBox_Search_Comment.IsChecked == true)
            {
                _searchColumn |= DB_RecLog.searchColumns.comment;
            }
            if (checkBox_Search_RecFileName.IsChecked == true)
            {
                _searchColumn |= DB_RecLog.searchColumns.recFilePath;
            }
            //
            isSearchOptionChanged = (_searchColumn != (DB_RecLog.searchColumns)Settings.Instance.RecLog_SearchColumn);
        }

        string _howto = @"
[HOWTO]

１．できること

　・予約済みまたは録画済み番組情報の検索および編集。
　・「録画ログ」タブでキーワード検索、検索結果を編集
　・「予約一覧」、「番組表」、「検索ウインドウ」などの右クリック・メニューからタイトルをキーワードにした録画ログ検索


２．準備

　この機能を使用するにはSQLServerが必要です。
　フルテキスト検索付きのものをインストールして下さい
 
　ダウンロード：
　　Microsoft SQL Server 2014 Express（https://www.microsoft.com/ja-jp/download/details.aspx?id=42299）
　　ExpressAdv 64BIT\SQLEXPRADV_x64_JPN.exe（６４ビット）　または　ExpressAdv 32BIT\SQLEXPRADV_x86_JPN.exe　（３２ビット）　

　インストール中の「機能」選択で、「インスタンス機能」-「データベースエンジンサービス」-「検索のためのフルテキスト抽出とセマンティック抽出」にチェックをします。
   
　SQLServerをインストール後「録画・検索ログを有効にする」をチェックし、「準備完了」と表示されるまで待ちます。


３．リモート接続

　SQLServerをリモート接続できるように設定（ファイアウォールなど）すれば、ネットワークモードのEpgTimerでも検索や録画ログの編集をすることができます。

　SQL Server Expressのリモート接続設定メモ
　　・SQL Server構成マネージャ「ネットワーク構成」「TCP/IP」を「有効」
　　　SQL Server 2014 構成マネージャの起動コマンド：「SQLServerManager12.msc」
　　・ServerBrowerを起動
　　・ファイアウォールで許可
　　　TCP 1433 (SQLServer)
　　　UDP 1434 (Server Browser)


４．その他
　
　４－１．「録画ログ・ウインドウ」
　　・トリム・ワード（検索の際に取り除かれる文字）の編集
            「テキストボックス」-「右クリックメニュー」-「トリムワードの編集」
        ・検索結果の録画ステータス変更
　　　「検索結果」-「右クリックメニュー」-「録画ステータス変更」

　４－２．「放送中止番組の予約削除を自動化」
　　・予約時のEPGイベントIDが削除された場合に、その番組が放送中止になったと判断してEpgTimerから予約削除を実行する
　　・放送局がそのEPGイベントIDを削除してもEDCBが古いEPGデータを保持し続けていることがあり、その場合は実際と異なり放送中止と判断されない。

";

    }
}