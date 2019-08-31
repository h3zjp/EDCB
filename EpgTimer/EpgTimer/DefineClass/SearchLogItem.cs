using EpgTimer.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace EpgTimer.DefineClass
{
    /// <summary>
    /// 
    /// </summary>
    public class SearchLogItem : NotifyPropertyChangedItem, IDBRecord
    {

        static readonly Regex rgxMark = new Regex(@"～|＃| #|\[|\]\（|\）|\(|\)|＜|＞|【|】|「|」");
        Regex _regex_NotWordItem = null;
        Regex _regex_NotWordItem_TitleOnly = null;
        List<ContentKindInfo> _filter_contentKindInfo = new List<ContentKindInfo>();
        bool _isNotWordItemChanged = false;
        List<SearchLogNotWordItem> _notWordItems = new List<SearchLogNotWordItem>();

        #region - Constructor -
        #endregion

        public SearchLogItem()
        {
            resultItems.CollectionChanged += ResultItems_CollectionChanged;
        }

        public SearchLogItem(long groupID0) : this()
        {
            tabID = groupID0;
        }

        #region - Method -
        #endregion

        public void notWordItem_Add(SearchLogNotWordItem notWordItem0)
        {
            _isNotWordItemChanged = true;
            _notWordItems.Add(notWordItem0);
        }

        public void notWordItem_Replace(ICollection<SearchLogNotWordItem> notWordItems0)
        {
            _isNotWordItemChanged = true;
            _notWordItems.Clear();
            _notWordItems.AddRange(notWordItems0);
        }

        public IEnumerable<SearchLogNotWordItem> notWordItems_Get()
        {
            return _notWordItems;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            SearchLogItem logItem0 = obj as SearchLogItem;
            if (logItem0 == null)
            {
                return false;
            }

            return (ID == logItem0.ID);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool matchFilteringCondition(EpgEventInfo epg0)
        {
            if (epg0 == null) { return false; }
            //
            if (_isNotWordItemChanged)
            {
                createFilterTools();
                _isNotWordItemChanged = false;
            }
            // Not Genre
            if (epg0.ContentInfo != null)
            {
                foreach (var item1 in _filter_contentKindInfo)
                {
                    foreach (var item2 in epg0.ContentInfo.nibbleList)
                    {
                        if (item1.Data.Nibble1 == item2.content_nibble_level_1)
                        {
                            if (item1.Data.Nibble2 == 255 || item1.Data.Nibble2 == item2.content_nibble_level_2)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            // Not Word
            if (epg0.ShortInfo != null)
            {
                if (_regex_NotWordItem_TitleOnly != null && _regex_NotWordItem_TitleOnly.IsMatch(epg0.ShortInfo.event_name))
                {
                    return true;
                }
                else if (_regex_NotWordItem != null)
                {
                    if (_regex_NotWordItem.IsMatch(epg0.ShortInfo.event_name) || _regex_NotWordItem.IsMatch(epg0.ShortInfo.text_char))
                    {
                        return true;
                    }
                    else if (epg0.ExtInfo != null && _regex_NotWordItem.IsMatch(epg0.ExtInfo.text_char))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// NotWordItem内のスペースで句切られた単語はAND条件として扱う
        /// </summary>
        void createFilterTools()
        {
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb_TitleOnly1 = new StringBuilder();
            _filter_contentKindInfo.Clear();
            foreach (var item1 in _notWordItems)
            {
                if (item1.isFilteringByGanre)
                {
                    _filter_contentKindInfo.Add(item1.contentKindInfo);
                }
                else
                {
                    StringBuilder sb2 = sb1;
                    if (item1.isTitleOnly)
                    {
                        sb2 = sb_TitleOnly1;
                    }
                    if (0 < sb2.Length)
                    {
                        sb2.Append("|");
                    }
                    if (item1.isRegex)
                    {
                        sb2.Append("(" + item1.word + ")");
                    }
                    else
                    {
                        // AND条件
                        StringBuilder sb3 = new StringBuilder();
                        foreach (var w1 in Regex.Split(item1.word, "\\s+"))
                        {
                            sb3.Append("(?=.*" + Regex.Escape(w1));
                            if (item1.isTitleOnly && !rgxMark.IsMatch(w1)) // 「」がある(tirmKeywordされていない)ものは対象外。(e.g.「丘みどり、わらふぢなるお、笑福亭松喬」)
                            {
                                //
                                // 番組タイトルだけを対象とする場合、シリーズもののナンバリングタイトルの数字部分を正確にマッチさせるため
                                // 単語境界を設ける（「相棒」または「相棒1」のNotWordに「相棒17」をマッチさせない）
                                //  単語境界が無い（空白文字が無い）単語にマッチさせる場合は「正規表現モード」にチェックする
                                //
                                sb3.Append("\\b");
                            }
                            sb3.Append(")");
                        }
                        sb2.Append("(" + sb3.ToString() + ")");
                    }
                }
            }
            //
            _regex_NotWordItem = null;
            if (0 < sb1.Length)
            {
                _regex_NotWordItem = new Regex(sb1.ToString(), RegexOptions.IgnoreCase);
            }
            _regex_NotWordItem_TitleOnly = null;
            if (0 < sb_TitleOnly1.Length)
            {
                _regex_NotWordItem_TitleOnly = new Regex(sb_TitleOnly1.ToString(), RegexOptions.IgnoreCase);
            }
        }

        /// <summary>
        /// 検索ワードを除去
        /// </summary>
        public void removeAndkeyFromNotWords()
        {
            foreach (var nwItem1 in _notWordItems)
            {
                nwItem1.word = removeAndkeyFromNotWord(nwItem1.word);
            }
        }
      
        string removeAndkeyFromNotWord(string word0)
        {
            if (string.IsNullOrWhiteSpace(epgSearchKeyInfoS.andKey)) { return word0; }
            //
            string word1 = word0;
            foreach (var item1 in Regex.Split(epgSearchKeyInfoS.andKey, "\\s+"))
            {
                word1 = Regex.Replace(word1, Regex.Escape(item1), " ");
            }
            word1 = RecLogWindow.trimKeyword(word1);

            return word1;
        }

        public bool addNotWord(out SearchLogNotWordItem notWordItem0, string word0, bool isTitleOnly0)
        {
            string word1 = removeAndkeyFromNotWord(word0);
            foreach (var item in _notWordItems)
            {
                if (item.word == word1)
                {
                    MessageBox.Show("「" + word1 + "」は登録済みです。", "通知", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    notWordItem0 = null;
                    return false;
                }
            }
            notWordItem0 = new SearchLogNotWordItem(word1)
            {
                searchLogID = ID,
                isTitleOnly = isTitleOnly0
            };
            notWordItem_Add(notWordItem0);

            return true;
        }

        public void setResult_NewProperty()
        {
            result_New = resultItems.Count - resultItems.Where(x => x.confirmed).Count();
        }

        #region - Property -
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public long ID
        {
            get { return this._ID; }
            set { this._ID = value; }
        }
        long _ID = -1;

        /// <summary>
        /// タブグループ
        /// </summary>
        public long tabID
        {
            get { return this._tabID; }
            set { this._tabID = value; }
        }
        long _tabID = -1;

        /// <summary>
        /// ListViewの行番号
        /// </summary>
        public int listOrder
        {
            get { return this._listOrder; }
            set { this._listOrder = value; }
        }
        int _listOrder = -1;

        /// <summary>
        /// 検索実行時のタイムスタンプ
        /// </summary>
        public DateTime lastUpdate
        {
            get { return this._lastUpdate; }
            set { this._lastUpdate = value; }
        }
        DateTime _lastUpdate = DB.minValue_DateTime;

        /// <summary>
        /// 
        /// </summary>
        public long epgSearchKeyInfoID
        {
            get { return this._epgSearchKeyInfoID; }
            set { this._epgSearchKeyInfoID = value; }
        }
        long _epgSearchKeyInfoID = 0;

        public EpgSearchKeyInfoS epgSearchKeyInfoS
        {
            get { return this._epgSearchKeyInfo; }
            set
            {
                this._epgSearchKeyInfo = value;
                // set andKey Property
                if (value == null)
                {
                    andKey = null;
                }
                else
                {
                    andKey = epgSearchKeyInfoS.andKey;
                }
            }
        }
        EpgSearchKeyInfoS _epgSearchKeyInfo = new EpgSearchKeyInfoS();

        public ObservableCollection<SearchLogResultItem> resultItems { get; private set; } = new ObservableCollection<SearchLogResultItem>();

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public Brush borderBrush
        {
            get { return ViewUtil.EpgDataContentBrush(epgSearchKeyInfoS.contentList); }
        }

        /// <summary>
        ///  ListViewItem Property
        /// </summary> 
        public string name
        {
            get { return this._name; }
            set
            {
                if (this._name != value)
                {
                    this._name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        string _name = null;

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public string andKey
        {
            get { return this._andKey; }
            set
            {
                if (this._andKey != value)
                {
                    this._andKey = value;
                    NotifyPropertyChanged();
                }
            }
        }
        string _andKey = null;

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public int result_New
        {
            get { return this._result_New; }
            set
            {
                if (this._result_New != value)
                {
                    this._result_New = value;
                    NotifyPropertyChanged();
                }
            }
        }
        int _result_New = 0;

        /// <summary>
        /// ListViewItem Property
        /// </summary>
        public int result_Total
        {
            get { return this._result_Total; }
            set
            {
                if (this._result_Total != value)
                {
                    this._result_Total = value;
                    NotifyPropertyChanged();
                }
            }
        }
        int _result_Total = 0;

        #region - Event Handler -
        #endregion

        private void ResultItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SearchLogResultItem resultItem1 in e.NewItems)
                {
                    resultItem1.PropertyChanged += ResultItem1_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (SearchLogResultItem resultItem1 in e.OldItems)
                {
                    resultItem1.PropertyChanged -= ResultItem1_PropertyChanged;
                }
            }
            setResult_NewProperty();
            result_Total = resultItems.Count;
        }

        private void ResultItem1_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "confirmed")
            {
                setResult_NewProperty();
            }
        }

    }

    /// <summary>
    /// DBレコード
    /// </summary>
    public class EpgSearchKeyInfoS : EpgSearchKeyInfo, IDBRecord
    {

        #region - Constructor -
        #endregion

        #region - Method -
        #endregion

        #region - Property -
        #endregion

        public long ID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        long _ID = -1;

        /// <summary>
        /// timestanp
        /// </summary>
        public DateTime lastUpdate
        {
            get { return this._lastUpdate; }
            set { this._lastUpdate = value; }
        }
        DateTime _lastUpdate = DB.minValue_DateTime;

        #region - Event Handler -
        #endregion

    }

    /// <summary>
    /// 
    /// </summary>
    public class SearchLogTabInfo : IDBRecord
    {

        #region - Constructor -
        #endregion

        #region - Method -
        #endregion

        #region - Property -
        #endregion

        public long ID
        {
            get { return this._ID; }
            set { this._ID = value; }
        }
        long _ID = -1;

        public string header { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int tabOrder { get; set; }

        #region - Event Handler -
        #endregion

    }

    public class SearchLogNotWordItem : IDBRecord
    {

        #region - Constructor -
        #endregion

        public SearchLogNotWordItem() { }

        public SearchLogNotWordItem(string word0)
        {
            word = word0;
        }

        #region - Method -
        #endregion

        #region - Property -
        #endregion

        public long ID
        {
            get { return this._ID; }
            set { this._ID = value; }
        }
        long _ID = -1;


        public long searchLogID
        {
            get { return this._searchLogID; }
            set { this._searchLogID = value; }
        }
        long _searchLogID = -1;

        public string word
        {
            get { return this._word; }
            set { this._word = value; }
        }
        string _word = null;

        public ContentKindInfo contentKindInfo { get; set; }

        public string displayWord
        {
            get
            {
                if (contentKindInfo != null)
                {
                    return "[" + contentKindInfo.ListBoxView + "]";
                }
                else
                {
                    return this.word;
                }
            }
        }

        public bool isFilteringByGanre
        {
            get { return (contentKindInfo != null); }
        }

        public bool isTitleOnly
        {
            get { return this._isTitleOnly; }
            set { this._isTitleOnly = value; }
        }
        bool _isTitleOnly = false;


        public bool isRegex
        {
            get { return this._isRegex; }
            set { this._isRegex = value; }
        }
        bool _isRegex = false;

        #region - Event Handler -
        #endregion

    }

}
