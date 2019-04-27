using EpgTimer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace EpgTimer.DefineClass
{
    /// <summary>
    /// DBのレコード
    /// </summary>
    public class RecLogItem : NotifyPropertyChangedItem, IDBRecord
    {

        public enum RecodeStatuses
        {
            NONE = 0,
            予約済み = 1,
            録画完了 = 2,
            視聴済み = 4,
            録画異常 = 8,
            無効登録 = 16,
            不明 = 32,
            放送中止 = 64,
            ALL = 127
        };

        #region - Constructor -
        #endregion

        #region - Method -
        #endregion

        public bool equals(RecFileInfo rfi0)
        {
            return (rfi0.OriginalNetworkID == epgEventInfoR.original_network_id
                && rfi0.TransportStreamID == epgEventInfoR.transport_stream_id
                && rfi0.ServiceID == epgEventInfoR.service_id
                && rfi0.EventID == epgEventInfoR.event_id);
        }

        public ulong Create64Key()
        {
            if (epgEventInfoR == null)
            {
                return 0;
            }
            else
            {
                return CommonManager.Create64Key(epgEventInfoR.original_network_id, epgEventInfoR.transport_stream_id, epgEventInfoR.service_id);
            }
        }

        #region - Property -
        #endregion

        public long ID { get; set; }

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string recFilePath
        {
            get { return _recFilePath; }
            set { _recFilePath = value; }
        }
        string _recFilePath = string.Empty;

        public bool epgAllowOverWrite { get; set; } = true;

        /// <summary>
        /// DB EpgEventInfo Table ID
        /// </summary>
        public long epgEventInfoID
        {
            get { return _epgEventInfoID; }
            set { _epgEventInfoID = value; }
        }
        long _epgEventInfoID = 0;

        public EpgEventInfoR epgEventInfoR
        {
            get { return _epgEventInfoR; }
            set { _epgEventInfoR = value; }
        }
        EpgEventInfoR _epgEventInfoR = new EpgEventInfoR();

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public RecodeStatuses recodeStatus
        {
            get { return this._recodeStatus; }
            set
            {
                this._recodeStatus = value;
                NotifyPropertyChanged();
            }
        }
        RecodeStatuses _recodeStatus = RecodeStatuses.NONE;

        /// <summary>
        /// 略記
        /// </summary>
        public string recodeStatus_Abbr
        {
            get
            {
                switch (recodeStatus)
                {
                    case RecodeStatuses.予約済み:
                        return "予";
                    case RecodeStatuses.録画完了:
                        return "録";
                    case RecodeStatuses.視聴済み:
                        return "視";
                    case RecodeStatuses.録画異常:
                        return "異";
                    case RecodeStatuses.無効登録:
                        return "無";
                    default:
                        return "?";
                }
            }
        }

        /// <summary>
        /// ListViewItem Sort Property
        /// </summary>
        public DateTime date
        {
            get { return epgEventInfoR.start_time; }
        }

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string dateStr
        {
            get { return epgEventInfoR.start_time.ToString("yyyy/MM/dd HH:mm"); }
        }

        /// <summary>
        /// DB更新日時
        /// </summary>
        public DateTime lastUpdate
        {
            get { return _lastUpdate; }
            set { _lastUpdate = value; }
        }
        DateTime _lastUpdate;

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string tvProgramTitle
        {
            get { return epgEventInfoR.ShortInfo.event_name; }
        }

        /// <summary>
        /// 番組情報
        ///  ListViewItem Property
        /// </summary>
        public string tvProgramSummary
        {
            get { return epgEventInfoR.ShortInfo.text_char; }
        }

        public string ExtInfo_text_char
        {
            get { return epgEventInfoR.ExtInfo.text_char; }
        }

        /// <summary>
        /// コメント
        /// </summary>
        public string comment
        {
            get { return _comment; }
            set { _comment = value; }
        }
        string _comment = string.Empty;

        public string tvStationName
        {
            get
            {
                if (_tvStationName == null)
                {
                    UInt64 key = epgEventInfoR.Create64Key();
                    if (ChSet5.ChList.ContainsKey(key) == true)
                    {
                        _tvStationName = ChSet5.ChList[key].service_name;
                    }
                    else
                    {
                        _tvStationName = string.Empty;
                    }
                }
                return _tvStationName;
            }
        }
        string _tvStationName = null;

        public Brush borderBrush
        {
            get
            {
                if (epgEventInfoR == null) return Brushes.White;
                //
                if (epgEventInfoR.ContentInfo.nibbleList.Count == 0)
                {
                    return Brushes.Gainsboro;
                }
                return ViewUtil.EpgDataContentBrush(epgEventInfoR.ContentInfo.nibbleList);
            }
        }

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string imageQuality
        {
            get { return this.epgEventInfoR.imageQuality; }
        }

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public Brush foreground_ImageQuality
        {
            get { return this.epgEventInfoR.foreground_ImageQuality; }
        }

        /// <summary>
        /// 
        /// </summary> 
        public bool isRecFileExist
        {
            get { return this._isRecFileExist; }
            set
            {
                this._isRecFileExist = value;
                if (value)
                {
                    exsitRecFile = "○";
                }
                else
                {
                    exsitRecFile = null;
                }
            }
        }
        bool _isRecFileExist = false;

        /// <summary>
        /// ListViewItem Property
        /// </summary> 
        public string exsitRecFile
        {
            get { return this._exsitRecFile; }
            set
            {
                this._exsitRecFile = value;
                NotifyPropertyChanged();
            }
        }
        string _exsitRecFile = null;

        #region - Event Handler -
        #endregion

    }

}
