using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace EpgTimer.Common
{
    /// <summary>
    /// 
    /// </summary>
    class DB_SearchLogResult : DBBase<SearchLogResultItem>, IDB_EpgEventInfo
    {

        public const string TABLE_NAME = "searchLogResult";
        public const string TABLE_NAME_ABBR = "slr";
        public const string COLUMN_searchLogID = "searchLogID", COLUMN_epgEventInfoID = "epgEventInfoID", COLUMN_recodeStatus = "recodeStatus",
            COLUMN_confirmed = "confirmed";

        DB_EpgEventInfo _db_EpgEventInfo;
        DB_RecLog _db_RecLog;

        #region - Constructor -
        #endregion

        public DB_SearchLogResult(DB_RecLog db_RecLog0)
        {
            _db_RecLog = db_RecLog0;
            _db_EpgEventInfo = new DB_EpgEventInfo(this);
        }

        #region - Method -
        #endregion

        public int update(ICollection<SearchLogResultItem> items0)
        {
            return base.update_(items0);
        }

        public int update(List<SearchLogResultItem> items0, SqlCommand cmd0)
        {
            return base.update_(items0, cmd0);
        }

        public int delete(SearchLogItem logItem0, SqlCommand cmd0)
        {
            List<long> ids1 = new List<long>();
            List<long> epgIDs1 = new List<long>();
            cmd0.CommandText = "SELECT " + COLUMN_ID + ", " + COLUMN_epgEventInfoID + " FROM " + TABLE_NAME + " WHERE " + COLUMN_searchLogID + "=" + logItem0.ID;
            using (SqlDataReader reader1 = cmd0.ExecuteReader())
            {
                while (reader1.Read())
                {
                    ids1.Add((long)reader1[0]);
                    epgIDs1.Add((long)reader1[1]);
                }
            }
            int res1 = base.delete_(ids1, cmd0);
            _db_EpgEventInfo.delete(epgIDs1, cmd0);

            return res1;
        }

        public int delete(List<SearchLogResultItem> items0, SqlCommand cmd0)
        {
            int res1 = base.delete_(items0, cmd0);
            _db_EpgEventInfo.delete(
                items0.Select(x => x.epgEventInfoID), cmd0);

            return res1;
        }

        public List<SearchLogResultItem> select(SearchLogItem logItem0, SqlCommand cmd0)
        {
            cmd0.CommandText = "SELECT * FROM " + tableName + " " + TABLE_NAME_ABBR +
                " INNER JOIN " + DB_EpgEventInfo.TABLE_NAME + " " + DB_EpgEventInfo.TABLE_NAME_ABBR +
                " ON (" + TABLE_NAME_ABBR + "." + COLUMN_epgEventInfoID + "=" + DB_EpgEventInfo.TABLE_NAME_ABBR + "." + DB_EpgEventInfo.COLUMN_ID + ")" +
                " WHERE " + TABLE_NAME_ABBR + "." + COLUMN_searchLogID + "=" + logItem0.ID +
                " ORDER BY " + DB_EpgEventInfo.TABLE_NAME_ABBR + "." + DB_EpgEventInfo.COLUMN_start_time;
            List<SearchLogResultItem> itemList1 = new List<SearchLogResultItem>();
            using (SqlDataReader reader1 = cmd0.ExecuteReader())
            {
                while (reader1.Read())
                {
                    int i1 = 0;
                    SearchLogResultItem resutItem1 = getItem(reader1, ref i1);
                    resutItem1.epgSearchKeyInfo = logItem0.epgSearchKeyInfoS;
                    resutItem1.epgEventInfoR = _db_EpgEventInfo.getItem(reader1, ref i1);
                    if (!resutItem1.epgEventInfoR.isBroadcasted())    // 放送終了?
                    {
                        itemList1.Add(resutItem1);
                    }
                }
            }

            return itemList1;
        }

        public long insert(SearchLogResultItem item0, SqlCommand cmd0)
        {
            item0.epgEventInfoID = _db_EpgEventInfo.insert(item0.epgEventInfoR, cmd0);

            return base.insert_(item0, cmd0);
        }

        public void insert(List<SearchLogResultItem> items0, SqlCommand cmd0)
        {
            foreach (var item1 in items0)
            {
                insert(item1, cmd0);
            }
        }

        void updateEpg(List<SearchLogResultItem> items0, SqlCommand cmd0)
        {
            List<EpgEventInfoR> epgList1 = new List<EpgEventInfoR>();
            foreach (var item1 in items0)
            {
                epgList1.Add(item1.epgEventInfoR);
            }
            _db_EpgEventInfo.updateIfNotReferenced(epgList1, tableName, cmd0);
            //
            foreach (SearchLogResultItem item1 in items0)
            {
                cmd0.CommandText = "UPDATE " + tableName + " SET " + COLUMN_recodeStatus + "=" + (int)item1.recodeStatus + " WHERE " + COLUMN_ID + "=" + item1.ID;
                cmd0.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// update SearchResults of the SearchLogItem.
        /// Update or Insert, and Delete.
        /// </summary>
        /// <param name="logItem0"></param>
        /// <param name="cmd0"></param>
        public void update4LogItem(SearchLogItem logItem0, SqlCommand cmd0)
        {
            List<SearchLogResultItem> results_Update1 = new List<SearchLogResultItem>();
            List<SearchLogResultItem> results_Insert1 = new List<SearchLogResultItem>();
            //
            List<SearchLogResultItem> results_DB1 = select(logItem0, cmd0);
            for (int i = 0; i < logItem0.resultItems.Count; i++)
            {
                SearchLogResultItem result2 = results_DB1.Find(
                    x1 => x1.epgEventInfoR.Equals(logItem0.resultItems[i].epgEventInfoR));
                if (result2 != null)
                {
                    results_Update1.Add(logItem0.resultItems[i]);
                    results_DB1.Remove(result2);
                }
                else
                {
                    results_Insert1.Add(logItem0.resultItems[i]);
                }
            }
            updateEpg(results_Update1, cmd0);

            insert(results_Insert1, cmd0);
            delete(results_DB1, cmd0);
            _db_EpgEventInfo.delete(
                results_DB1.Select(x => x.epgEventInfoID), cmd0);
        }

        public override SearchLogResultItem getItem(SqlDataReader reader0, ref int i0)
        {
            SearchLogResultItem item1 = new SearchLogResultItem()
            {
                ID = (long)reader0[i0++],
                searchLogItemID = (long)reader0[i0++],
                epgEventInfoID = (long)reader0[i0++],
                recodeStatus = (RecLogItem.RecodeStatuses)reader0[i0++],
                confirmed = (bool)reader0[i0++],
            };

            return item1;
        }

        protected override Dictionary<string, string> getFieldNameValues(SearchLogResultItem item0)
        {
            Dictionary<string, string> dict1 = new Dictionary<string, string>()
            {
                { COLUMN_searchLogID, item0.searchLogItemID.ToString() },
                { COLUMN_epgEventInfoID, item0.epgEventInfoID.ToString() },
                { COLUMN_recodeStatus, ((int)item0.recodeStatus).ToString() },
                { COLUMN_confirmed, convert2BitString(item0.confirmed) },
            };

            return dict1;
        }

        public void createTable(SqlCommand cmd0)
        {
            cmd0.CommandText = "CREATE TABLE [dbo].[" + TABLE_NAME + "](" +
                "[" + COLUMN_ID + "][bigint] IDENTITY(1,1) NOT NULL," +
                "[" + COLUMN_searchLogID + "] [bigint] NOT NULL," +
                "[" + COLUMN_epgEventInfoID + "] [bigint] NOT NULL," +
                "[" + COLUMN_recodeStatus + "] [int] NOT NULL," +
                "[" + COLUMN_confirmed + "] [bit] NOT NULL," +
                "CONSTRAINT [PK_searchLogResult] PRIMARY KEY CLUSTERED ([" + COLUMN_ID + "] ASC))";
            cmd0.ExecuteNonQuery();
            //
            base.createIndex("IX_searchLogResult_searchLogID ", new string[] { COLUMN_searchLogID }, cmd0);
            base.createIndex("IX_searchLogResult_EpgEventInfoID ", new string[] { COLUMN_epgEventInfoID }, cmd0);
        }

        #region - Property -
        #endregion

        public override string tableName
        {
            get { return TABLE_NAME; }
        }

        public string columnName_epgEventInfoID
        {
            get { return COLUMN_epgEventInfoID; }
        }

        #region - Event Handler -
        #endregion

    }
}
