using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace EpgTimer.Common
{
    class DB_SearchLogNotWord : DBBase<SearchLogNotWordItem>
    {

        public const string TABLE_NAME = "searchLogNotWord";
        const string TABLE_NAME_ABBR = "sln";

        public const string COLUMN_searchLogID = "searchLogID", COLUMN_word = "word", COLUMN_isTitleOnly = "isTitleOnly", COLUMN_isRegex = "isRegex",
            COLUMN_contentKindInfo = "contentKindInfo";

        #region - Constructor -
        #endregion

        #region - Method -
        #endregion

        public long insert(SearchLogNotWordItem item0)
        {
            return base.insert_(item0);
        }

        public long insert(SearchLogNotWordItem item0, SqlCommand cmd0)
        {
            return base.insert_(item0, cmd0);
        }

        public int delete(SearchLogItem logItem0, SqlCommand cmd0)
        {
            cmd0.CommandText = "DELETE FROM " + tableName + " WHERE " + COLUMN_searchLogID + "=" + logItem0.ID;
            return (int)cmd0.ExecuteNonQuery();
        }

        public void update(SearchLogItem logItem0, SqlCommand cmd0)
        {
            List<SearchLogNotWordItem> notWordItems_Update = new List<SearchLogNotWordItem>();
            List<SearchLogNotWordItem> notWordItems_Insert = new List<SearchLogNotWordItem>();
            List<SearchLogNotWordItem> notWordItems1 = getNotWordItems(logItem0, cmd0);
            foreach (var item1 in logItem0.notWordItems_Get())
            {
                if (item1.ID < 0)
                {
                    item1.searchLogID = logItem0.ID;
                    notWordItems_Insert.Add(item1);
                }
                else
                {
                    notWordItems_Update.Add(item1);
                }
                SearchLogNotWordItem item2 = notWordItems1.Find((x1) => { return (x1.ID == item1.ID); });
                if (item2 != null)
                {
                    notWordItems1.Remove(item2);
                }
            }
            //
            update_(notWordItems_Update, cmd0);
            insert(notWordItems_Insert, cmd0);
            delete_(notWordItems1, cmd0);
        }

        public List<SearchLogNotWordItem> getNotWordItems(SearchLogItem searchLogItem0, SqlCommand cmd0)
        {
            List<SearchLogNotWordItem> notWordItems1 = new List<SearchLogNotWordItem>();
            cmd0.CommandText = "SELECT * FROM " + tableName + " WHERE " + COLUMN_searchLogID + "=" + searchLogItem0.ID + " ORDER BY " + COLUMN_word;
            using (SqlDataReader reader1 = cmd0.ExecuteReader())
            {
                while (reader1.Read())
                {
                    int i1 = 0;
                    notWordItems1.Add(
                        getItem(reader1, ref i1));
                }
            }

            return notWordItems1;
        }

        protected override Dictionary<string, string> getFieldNameValues(SearchLogNotWordItem item0)
        {
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            dict1.Add(COLUMN_searchLogID, item0.searchLogID.ToString());
            dict1.Add(COLUMN_word, q(item0.word));
            dict1.Add(COLUMN_isTitleOnly, convert2BitString(item0.isTitleOnly));
            dict1.Add(COLUMN_isRegex, convert2BitString(item0.isRegex));
            addFieldNameValue_ContentKindInfo(ref dict1, item0);

            return dict1;
        }

        void addFieldNameValue_ContentKindInfo(ref Dictionary<string, string> dict0, SearchLogNotWordItem item0)
        {
            if (item0.contentKindInfo == null)
            {
                dict0.Add(COLUMN_contentKindInfo, "NULL");
            }
            else
            {
                dict0.Add(
                    COLUMN_contentKindInfo,
                    "0x" + item0.contentKindInfo.Data.Nibble1.ToString("X2") + item0.contentKindInfo.Data.Nibble2.ToString("X2"));
            }
        }

        public override SearchLogNotWordItem getItem(SqlDataReader reader0, ref int i0)
        {
            SearchLogNotWordItem item1 = new SearchLogNotWordItem()
            {
                ID = (long)reader0[i0++],
                searchLogID = (long)reader0[i0++],
                word = ((string)reader0[i0++]).Trim(),
                isTitleOnly = (bool)reader0[i0++],
                isRegex = (bool)reader0[i0++],
                contentKindInfo = getContentKindInfo(reader0, ref i0),
            };

            return item1;
        }

        ContentKindInfo getContentKindInfo(SqlDataReader reader0, ref int i0)
        {
            object o1 = reader0[i0++];
            if (o1 == DBNull.Value)
            {
                return null;
            }
            else
            {
                byte[] bytes1 = (byte[])o1;
                byte nibble_level_1 = bytes1[0];
                byte nibble_level_2 = bytes1[1];
                foreach (var item in CommonManager.ContentKindList)
                {
                    if (item.Data.Nibble1 == nibble_level_1 && item.Data.Nibble2 == nibble_level_2)
                    {
                        return item;
                    }
                }

                return null;
            }
        }

        public bool createTableIfNotExixts()
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        cmd1.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = " + q(tableName) + "";
                        string tableName1 = (string)cmd1.ExecuteScalar();
                        bool notExists1 = (string.IsNullOrEmpty(tableName1));
                        if (notExists1)
                        {
                            createTable(cmd1);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return false;
        }

        public bool alterTable_COLUMN_contentKindInfo()
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        cmd1.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS" +
                            " WHERE TABLE_NAME = " + q(tableName) + " AND COLUMN_NAME = " + q(COLUMN_contentKindInfo);
                        string name1 = (string)cmd1.ExecuteScalar();
                        bool notExists1 = (string.IsNullOrEmpty(name1));
                        if (notExists1)
                        {
                            cmd1.CommandText = "ALTER TABLE " + tableName + " ADD " + COLUMN_contentKindInfo + " [binary](4)";
                            int ret1 = cmd1.ExecuteNonQuery();
                            return (0 < ret1);
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return false;
        }

        public void createTable(SqlCommand cmd0)
        {
            cmd0.CommandText = "CREATE TABLE [dbo].[" + TABLE_NAME + "](" +
                "[" + COLUMN_ID + "] [bigint] IDENTITY(1,1) NOT NULL," +
                "[" + COLUMN_searchLogID + "] [bigint] NOT NULL," +
                "[" + COLUMN_word + "] [nvarchar](50) NOT NULL," +
                "[" + COLUMN_isTitleOnly + "] [bit] NOT NULL," +
                "[" + COLUMN_isRegex + "] [bit] NOT NULL," +
                "[" + COLUMN_contentKindInfo + "] [binary](4)," +
                "CONSTRAINT [PK_searchLogNotWord] PRIMARY KEY CLUSTERED ([" + COLUMN_ID + "] ASC))";
            cmd0.ExecuteNonQuery();
            //
            createIndex("IX_" + tableName + "_" + COLUMN_searchLogID, new string[] { COLUMN_searchLogID }, cmd0);
        }

        #region - Property -
        #endregion

        public override string tableName
        {
            get { return TABLE_NAME; }
        }

        #region - Event Handler -
        #endregion

    }

}
