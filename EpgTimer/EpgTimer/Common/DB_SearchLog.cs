using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;

namespace EpgTimer.Common
{
    /// <summary>
    /// 
    /// </summary>
    public class DB_SearchLog : DBBase<SearchLogItem>
    {
        public enum searchColumns
        {
            NONE = 0,
            /// <summary>
            /// 番組名
            /// </summary>
            title = 1,
            /// <summary>
            /// 番組情報
            /// </summary>
            content = 2,
            /// <summary>
            /// 
            /// </summary>
            ALL = 3
        };

        public const string TABLE_NAME = "searchLog";
        const string TABLE_NAME_ABBR = "sl";

        public const string COLUMN_lastUpdate = "lastUpdate", COLUMN_name = "name",
            COLUMN_EpgSearchKeyInfoID = "EpgSearchKeyInfoID", COLUMN_tabID = "tabID",
            COLUMN_listOrder = "listOrder";
        DB_EpgSearchKeyInfo _db_EpgSearchKeyInfo = new DB_EpgSearchKeyInfo();
        DB_SearchLogResult _db_SearchLogResult;
        DB_RecLog _db_RecLog = new DB_RecLog();
        DB_EpgEventInfo _db_EpgEventInfo;
        DB_SearchLogNotWord _db_NotWord = new DB_SearchLogNotWord();

        #region - Constructor -
        #endregion

        public DB_SearchLog()
        {
            _db_EpgEventInfo = new DB_EpgEventInfo();
            _db_SearchLogResult = new DB_SearchLogResult(_db_RecLog);
        }

        #region - Method -
        #endregion

        public int update(ICollection<SearchLogItem> items0)
        {
            return base.update_(items0);
        }

        /// <summary>
        /// EpgSearchKeyInfoと, NotWordとをDBから取得して更新
        /// </summary>
        /// <param name="searchLogItems0"></param>
        /// <param name="search0"></param>
        /// <param name="bgw0"></param>
        public void getSearchResults(IList<SearchLogItem> searchLogItems0, Action<SearchLogItem> search0, BackgroundWorker bgw0)
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        for (int i1 = 0; i1 < searchLogItems0.Count; i1++)
                        {
                            SearchLogItem item1 = searchLogItems0[i1];
                            item1.epgSearchKeyInfoS = _db_EpgSearchKeyInfo.select(item1.epgSearchKeyInfoID, cmd1);
                            item1.notWordItem_Replace(_db_NotWord.getNotWordItems(item1, cmd1));
                            search0(item1);
                            update_RecodeStatus(item1, false, cmd1);
                            //
                            bgw0.ReportProgress(i1 + 1);
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
        }

        public void insertNotWordItem(SearchLogNotWordItem notWordItem0)
        {
            _db_NotWord.insert(notWordItem0);
        }

        public void searchTitleByRecLog(ICollection<SearchLogResultItem> searchLogResults0)
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        foreach (var item1 in searchLogResults0)
                        {
                            if (item1.confirmed) { continue; }
                            if (item1.recodeStatus != RecLogItem.RecodeStatuses.NONE) { continue; }
                            //
                            string tvProgramTitle1 = RecLogWindow.trimKeyword(item1.tvProgramTitle);
                            List<RecLogItem> recLogItems1;
                            switch (Settings.Instance.RecLog_SearchMethod)
                            {
                                case RecLogView.searchMethods.LIKE:
                                    recLogItems1 = _db_RecLog.search_Like(cmd1, tvProgramTitle1, RecLogItem.RecodeStatuses.ALL, DB_RecLog.searchColumns.title, 1, item1.epgEventInfoR.ContentInfo);
                                    break;
                                case RecLogView.searchMethods.Contrains:
                                    recLogItems1 = _db_RecLog.search_Fulltext(cmd1, tvProgramTitle1, RecLogItem.RecodeStatuses.ALL, DB_RecLog.searchColumns.title, 1, item1.epgEventInfoR.ContentInfo);
                                    break;
                                case RecLogView.searchMethods.Freetext:
                                    recLogItems1 = _db_RecLog.search_Fulltext(cmd1, tvProgramTitle1, RecLogItem.RecodeStatuses.ALL, DB_RecLog.searchColumns.title, 1, item1.epgEventInfoR.ContentInfo, true);
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            if (0 < recLogItems1.Count)
                            {
                                item1.recodeStatus = recLogItems1[0].recodeStatus;
                            }
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
        }

        /// <summary>
        /// 検索結果の録画ステータスを変更、または録画ログを新たに追加する。
        /// </summary>
        /// <param name="searchLogResultItems"></param>
        public void update_RecodeStatus(IEnumerable<SearchLogResultItem> searchLogResultItems)
        {
            DateTime lastUpdate1 = DateTime.Now;
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        List<RecLogItem> recLogItems_Insert1 = new List<RecLogItem>();
                        List<RecLogItem> recLogItems_Update1 = new List<RecLogItem>();
                        List<RecLogItem> recLogItems_Delete1 = new List<RecLogItem>();
                        foreach (var item in searchLogResultItems)
                        {
                            RecLogItem recLogItem1 = _db_RecLog.exists(item.epgEventInfoR, cmd1);
                            if (recLogItem1 == null)
                            {
                                if (item.recodeStatus != RecLogItem.RecodeStatuses.NONE)
                                {
                                    recLogItem1 = new RecLogItem()
                                    {
                                        lastUpdate = lastUpdate1,
                                        recodeStatus = item.recodeStatus,
                                        epgEventInfoR = item.epgEventInfoR
                                    };
                                    recLogItems_Insert1.Add(recLogItem1);
                                }
                            }
                            else if (item.recodeStatus == RecLogItem.RecodeStatuses.NONE)
                            {
                                recLogItems_Delete1.Add(recLogItem1);
                            }
                            else
                            {
                                recLogItem1.lastUpdate = lastUpdate1;
                                recLogItem1.recodeStatus = item.recodeStatus;
                                recLogItems_Update1.Add(recLogItem1);
                            }
                        }
                        _db_RecLog.insert(recLogItems_Insert1, cmd1);
                        _db_RecLog.update(recLogItems_Update1, cmd1);
                        _db_RecLog.delete(recLogItems_Delete1, cmd1);
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchLogResultItems"></param>
        /// <param name="isUpdateDB0"></param>
        /// <param name="isInsertRecLog0">登録されていなければ追加する</param>
        public void update_RecodeStatus(SearchLogItem logItem0, bool isUpdateDB0)
        {
            DateTime lastUpdate1 = DateTime.Now;
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        update_RecodeStatus(logItem0, isUpdateDB0, cmd1);
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
        }

        void update_RecodeStatus(SearchLogItem logItem0, bool isUpdateDB0, SqlCommand cmd0)
        {
            List<EpgEventInfo> epgList1 = new List<EpgEventInfo>();
            foreach (var item in logItem0.resultItems)
            {
                epgList1.Add(item.epgEventInfoR);
            }
            List<RecLogItem> recLogItems1 = _db_RecLog.exists(epgList1, cmd0);
            List<SearchLogResultItem> results_Updated1 = new List<SearchLogResultItem>();
            foreach (var item in logItem0.resultItems)
            {
                RecLogItem.RecodeStatuses recodeStatus1 = RecLogItem.RecodeStatuses.NONE;
                RecLogItem recLogItem1 = recLogItems1.Find(x => x.epgEventInfoR.Equals(item.epgEventInfoR));
                if (recLogItem1 != null)
                {
                    recodeStatus1 = recLogItem1.recodeStatus;
                }
                if (item.recodeStatus != recodeStatus1)
                {
                    item.recodeStatus = recodeStatus1;
                    results_Updated1.Add(item);
                }
            }
            if (isUpdateDB0)
            {
                _db_SearchLogResult.update(results_Updated1, cmd0);
            }
        }

        public int delete(SearchLogItem logItem0)
        {
            int res1 = 0;
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        _db_EpgSearchKeyInfo.delete(logItem0.epgSearchKeyInfoS, cmd1);
                        _db_SearchLogResult.delete(logItem0, cmd1);
                        _db_NotWord.delete(logItem0, cmd1);
                        res1 = base.delete_(logItem0, cmd1);
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return res1;
        }

        public void updateSearchResult(IList<SearchLogItem> logItems0, BackgroundWorker bgw0)
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        for (int i1 = 0; i1 < logItems0.Count; i1++)
                        {
                            _db_SearchLogResult.update4LogItem(logItems0[i1], cmd1);
                            bgw0.ReportProgress(i1 + 1);
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
        }

        public void updateSearchResult(ICollection<SearchLogResultItem> resultItems0)
        {
            _db_SearchLogResult.update(resultItems0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logItem0"></param>
        /// <returns>new items count</returns>
        public int updateOrInsert(SearchLogItem logItem0)
        {
            return updateOrInsert(new List<SearchLogItem>() { logItem0 });
        }

        /// <summary>
        /// Update if exists OR Insert New.
        /// </summary>
        /// <param name="logItem0"></param>
        /// <returns>new items count</returns>
        public int updateOrInsert(List<SearchLogItem> logItems0)
        {
            int count_NewItem1 = 0;
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        foreach (var logItem1 in logItems0)
                        {
                            if (base.exists(logItem1.ID, cmd1))
                            {
                                _db_EpgSearchKeyInfo.update(logItem1.epgSearchKeyInfoS, cmd1);
                                _db_SearchLogResult.update4LogItem(logItem1, cmd1);
                                _db_NotWord.update(logItem1, cmd1);
                                update_(logItem1, cmd1);
                            }
                            else
                            {
                                insert(logItem1, cmd1);
                                count_NewItem1++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
            return count_NewItem1;
        }

        public long insert(SearchLogItem logItem0, SqlCommand cmd0)
        {
            cmd0.CommandText = _db_EpgSearchKeyInfo.getQuery_Insert(logItem0.epgSearchKeyInfoS);
            logItem0.epgSearchKeyInfoS.ID = (long)cmd0.ExecuteScalar();

            cmd0.CommandText = getQuery_Insert(logItem0);
            logItem0.ID = (long)cmd0.ExecuteScalar();

            for (int i1 = 0; i1 < logItem0.resultItems.Count; i1++)
            {
                SearchLogResultItem resultItem1 = logItem0.resultItems[i1];
                resultItem1.searchLogItemID = logItem0.ID;
                _db_SearchLogResult.insert(resultItem1, cmd0);
            }
            foreach (SearchLogNotWordItem notWordItem1 in logItem0.notWordItems_Get())
            {
                notWordItem1.searchLogID = logItem0.ID;
                _db_NotWord.insert(notWordItem1, cmd0);
            }

            return logItem0.ID;
        }

        public List<SearchLogItem> selectByTab(long tabID0)
        {
            List<SearchLogItem> searchLogItems = new List<SearchLogItem>();
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        cmd1.CommandText = "SELECT * FROM " + tableName + " " + TABLE_NAME_ABBR +
                          " INNER JOIN " + DB_EpgSearchKeyInfo.TABLE_NAME + " " + DB_EpgSearchKeyInfo.TABLE_NAME_ABBR +
                          " ON (" + TABLE_NAME_ABBR + "." + COLUMN_EpgSearchKeyInfoID + "=" + DB_EpgSearchKeyInfo.TABLE_NAME_ABBR + "." + DB_EpgSearchKeyInfo.COLUMN_ID + ")" +
                          " WHERE " + TABLE_NAME_ABBR + "." + COLUMN_tabID + "=" + tabID0 +
                          " ORDER BY " + COLUMN_listOrder;
                        using (SqlDataReader reader1 = cmd1.ExecuteReader())
                        {
                            while (reader1.Read())
                            {
                                int i1 = 0;
                                SearchLogItem searchLogItem1 = getItem(reader1, ref i1);
                                searchLogItem1.epgSearchKeyInfoS = _db_EpgSearchKeyInfo.getItem(reader1, ref i1);
                                searchLogItems.Add(searchLogItem1);
                            }
                        }
                        //
                        foreach (SearchLogItem item1 in searchLogItems)
                        {
                            foreach (var item in _db_SearchLogResult.select(item1, cmd1))
                            {
                                item1.resultItems.Add(item);
                            }
                            item1.notWordItem_Replace(_db_NotWord.getNotWordItems(item1, cmd1));
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return searchLogItems;
        }

        public override SearchLogItem getItem(SqlDataReader reader0, ref int i0)
        {
            SearchLogItem item1 = new SearchLogItem()
            {
                ID = (long)reader0[i0++],
                lastUpdate = (DateTime)reader0[i0++],
                name = ((string)reader0[i0++]).Trim(),
                epgSearchKeyInfoID = (long)reader0[i0++],
                tabID = (long)reader0[i0++],
                listOrder = (int)reader0[i0++]
            };

            return item1;
        }

        protected override Dictionary<string, string> getFieldNameValues(SearchLogItem item0)
        {
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            dict1.Add(COLUMN_lastUpdate, q(item0.lastUpdate.ToString(timeStampStrFormat)));
            dict1.Add(COLUMN_name, q(item0.name));
            dict1.Add(COLUMN_EpgSearchKeyInfoID, item0.epgSearchKeyInfoS.ID.ToString());
            dict1.Add(COLUMN_tabID, item0.tabID.ToString());
            dict1.Add(COLUMN_listOrder, item0.listOrder.ToString());

            return dict1;
        }

        /// <summary>
        /// DB変更
        /// </summary>
        public bool createNotWordTableIfNotExists()
        {
            if (_db_NotWord.createTableIfNotExixts())
            {
                _db_EpgSearchKeyInfo.alterTable_andKey_notKey();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool alterTable_NotWord()
        {
            return _db_NotWord.alterTable_COLUMN_contentKindInfo();
        }

        /// <summary>
        /// searchLog, epgSearchKeyInfo, searchLogResult, searchLogNotWord Table
        /// </summary>
        public void createTables()
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        createTable(cmd1);
                        _db_EpgSearchKeyInfo.createTable(cmd1);
                        _db_SearchLogResult.createTable(cmd1);
                        _db_NotWord.createTable(cmd1);
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
        }

        void createTable(SqlCommand cmd0)
        {
            cmd0.CommandText = "CREATE TABLE [dbo].[" + TABLE_NAME + "](" +
                "[" + COLUMN_ID + "] [bigint] IDENTITY(1,1) NOT NULL," +
                "[" + COLUMN_lastUpdate + "] [datetime] NOT NULL," +
                "[" + COLUMN_name + "] [nvarchar](100) NOT NULL," +
                "[" + COLUMN_EpgSearchKeyInfoID + "] [bigint] NOT NULL," +
                "[" + COLUMN_tabID + "] [bigint] NOT NULL," +
                "[" + COLUMN_listOrder + "] [int] NOT NULL," +
                "CONSTRAINT [PK_searchLog] PRIMARY KEY CLUSTERED ([" + COLUMN_ID + "] ASC))";
            cmd0.ExecuteNonQuery();
        }

        #region - Property -
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public override string tableName
        {
            get { return TABLE_NAME; }
        }

        #region - Event Handler -
        #endregion

    }

}
