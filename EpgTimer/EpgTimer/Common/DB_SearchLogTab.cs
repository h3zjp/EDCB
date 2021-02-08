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
    class DB_SearchLogTab : DBBase<SearchLogTabInfo>
    {

        public const string TABLE_NAME = "searchLogTab";
        public const string TABLE_NAME_ABBR = "slt";
        public const string COLUMN_header = "header", COLUMN_tabOrder = "tabOrder";

        #region - Constructor -
        #endregion

        #region - Method -
        #endregion

        public int delete(long id0)
        {
            return base.delete_(id0);
        }

        public int update(SearchLogTabInfo item0)
        {
            return base.update_(item0);
        }

        public int update(ICollection<SearchLogTabInfo> items0)
        {
            return base.update_(items0);
        }

        public long insert(SearchLogTabInfo item0)
        {
            return base.insert_(item0);
        }

        public List<SearchLogTabInfo> select()
        {
            StringBuilder query1 = new StringBuilder("SELECT * FROM " + TABLE_NAME + " ORDER BY " + DB_SearchLogTab.COLUMN_tabOrder);
            List<SearchLogTabInfo> itemList1 = new List<SearchLogTabInfo>();
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = new SqlCommand(query1.ToString(), sqlConn1))
                    using (SqlDataReader reader1 = cmd1.ExecuteReader())
                    {
                        while (reader1.Read())
                        {
                            int i1 = 0;
                            itemList1.Add(
                                getItem(reader1, ref i1));
                        }
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return itemList1;
        }

        public override SearchLogTabInfo getItem(SqlDataReader reader0, ref int i0)
        {
            SearchLogTabInfo item1 = new SearchLogTabInfo()
            {
                ID = (long)reader0[i0++],
                header = (string)reader0[i0++],
                tabOrder = (int)reader0[i0++],
            };

            return item1;
        }

        protected override Dictionary<string, string> getFieldNameValues(SearchLogTabInfo item0)
        {
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            dict1.Add(COLUMN_header, q(item0.header));
            dict1.Add(COLUMN_tabOrder, item0.tabOrder.ToString());

            return dict1;
        }

        public bool notExistsTable()
        {
            string query1 = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + TABLE_NAME + "'";
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = new SqlCommand(query1, sqlConn1))
                    {
                        string table1 = (string)cmd1.ExecuteScalar();
                        return string.IsNullOrEmpty(table1);
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return false;
        }

        public void createTable()
        {
            string query1 = "CREATE TABLE [dbo].[" + TABLE_NAME + "](" +
                "[" + COLUMN_ID + "] [bigint] IDENTITY(1,1) NOT NULL," +
                "[" + COLUMN_header + "] [nvarchar](50) NOT NULL," +
                "[" + COLUMN_tabOrder + "] [int] NOT NULL," +
                "CONSTRAINT [PK_searchLogTab] PRIMARY KEY CLUSTERED ([" + COLUMN_ID + "] ASC))";

            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = new SqlCommand(query1, sqlConn1))
                    {
                        cmd1.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }
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
