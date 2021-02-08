using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace EpgTimer.Common
{

    public abstract class DB
    {
        public enum connectTestResults
        {
            success,
            createDB,
            serverNotFound,
            unKnownError,
        };

        public static readonly DateTime minValue_DateTime = new DateTime(1900, 1, 1);

        public static readonly string timeStampStrFormat = "yyyy-MM-dd HH:mm:ss";
        protected static readonly string startTimeStrFormat = "yyyy-MM-dd HH:mm";

        #region - Constructor -
        #endregion

        #region - Method -
        #endregion

        public static void errLog(Exception ex0)
        {
            System.Diagnostics.Trace.WriteLine(ex0.ToString());
        }

        #region - Property -
        #endregion

        public abstract string tableName { get; }

        /// <summary>
        /// 
        /// </summary>
        public static string machineName
        {
            get { return _machineName; }
            set { _machineName = value; }
        }
        static string _machineName = null;

        /// <summary>
        /// 
        /// </summary>
        public static string instanceName
        {
            get { return _instanceName; }
            set { _instanceName = value; }
        }
        static string _instanceName = null;

        protected string dataSource
        {
            get
            {
                string dataSource1 = machineName;
                if (!string.IsNullOrWhiteSpace(instanceName))
                {
                    dataSource1 += "\\" + instanceName;
                }
                return dataSource1;
            }
        }

        #region - Event Handler -
        #endregion

    }

    public abstract class DBBase<T> : DB
        where T : class, IDBRecord
    {

        protected const string COLUMN_ID = "ID";
        protected const string FULLTEXTCATALOG = "epg_catalog";

        #region - Constructor -
        #endregion

        protected DBBase() { }

        #region - Method -
        #endregion

        /// <summary>
        /// SQLServerへの接続テスト
        /// 
        /// </summary>
        /// <param name="isTestOnly"></param>
        /// <returns></returns>
        public connectTestResults connectionTest(bool isTestOnly)
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    return connectTestResults.success;
                }
            }
            catch (SqlException sqlEx0)
            {
                switch (sqlEx0.Number)
                {
                    case -1:
                        // SQLSERVERが見つからない
                        // "SQL Server への接続を確立しているときにネットワーク関連またはインスタンス固有のエラーが発生しました。サーバーが見つからないかアクセスできません。インスタンス名が正しいこと、および SQL Server がリモート接続を許可するように構成されていることを確認してください。 (provider: SQL Network Interfaces, error: 26 - 指定されたサーバーまたはインスタンスの位置を特定しているときにエラーが発生しました)"
                        return connectTestResults.serverNotFound;
                    case 4060:
                        // DBが無い
                        // 「このログインで要求されたデータベース "%1!" を開けません。ログインに失敗しました。」
                        if (!isTestOnly)
                        {
                            createDB_EDCB();
                        }
                        return connectTestResults.createDB;
                    default:
                        errLog(sqlEx0);
                        return connectTestResults.unKnownError;
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return connectTestResults.unKnownError;
        }

        protected string getQuery_Update(ICollection<T> items0)
        {
            StringBuilder query1 = new StringBuilder();
            foreach (var item1 in items0)
            {
                StringBuilder keyValue1 = new StringBuilder();
                foreach (var kvp1 in getFieldNameValues(item1))
                {
                    if (0 < keyValue1.Length)
                    {
                        keyValue1.Append(", ");
                    }
                    keyValue1.Append("[" + kvp1.Key + "]=" + kvp1.Value);
                }
                query1.AppendLine("UPDATE " + tableName + " SET " + keyValue1.ToString() + " WHERE " + COLUMN_ID + "=" + item1.ID);
            }

            return query1.ToString();
        }

        public string getQuery_Insert(T item0)
        {
            StringBuilder keys1 = new StringBuilder();
            StringBuilder values1 = new StringBuilder();
            foreach (var kvp1 in getFieldNameValues(item0))
            {
                if (0 < keys1.Length)
                {
                    keys1.Append(", ");
                }
                keys1.Append("[" + kvp1.Key + "]");
                //
                if (0 < values1.Length)
                {
                    values1.Append(", ");
                }
                string val1 = kvp1.Value;
                if (string.IsNullOrEmpty(val1))
                {
                    val1 = "'" + val1 + "'";
                }
                values1.Append(val1);
            }

            return "INSERT INTO " + tableName + "(" + keys1.ToString() + ")" + " OUTPUT INSERTED." + COLUMN_ID + " VALUES(" + values1.ToString() + ")";
        }

        protected long insert_(T item0, SqlCommand cmd0)
        {
            cmd0.CommandText = getQuery_Insert(item0);
            item0.ID = (long)cmd0.ExecuteScalar();

            return item0.ID;
        }

        protected void insert(IEnumerable<T> items0, SqlCommand cmd0)
        {
            foreach (var item1 in items0)
            {
                insert_(item1, cmd0);
            }
        }

        protected long insert_(T item0)
        {
            string query1 = getQuery_Insert(item0);
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = new SqlCommand(query1, sqlConn1))
                    {
                        item0.ID = (long)cmd1.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return item0.ID;
        }

        protected int update_(T item0)
        {
            return update_(
                new List<T>() { item0 });
        }

        protected int update_(ICollection<T> items0)
        {
            int res1 = -1;
            if (items0.Count == 0) { return res1; }
            //
            string query1 = getQuery_Update(items0);
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = new SqlCommand(query1.ToString(), sqlConn1))
                    {
                        res1 = cmd1.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return res1;
        }

        protected int update_(T item0, SqlCommand cmd0)
        {
            return update_(
                new List<T>() { item0 },
                cmd0);
        }

        protected int update_(List<T> items0, SqlCommand cmd0)
        {
            if (items0.Count == 0) { return -1; }
            //
            cmd0.CommandText = getQuery_Update(items0);

            return cmd0.ExecuteNonQuery();
        }

        protected int delete_(T item0, SqlCommand cmd0)
        {
            return delete_(new long[] { item0.ID }, cmd0);
        }

        protected int delete_(List<T> items0, SqlCommand cmd0)
        {
            if (items0.Count() == 0) { return 0; }
            //
            StringBuilder sb1 = new StringBuilder();
            foreach (var item1 in items0)
            {
                if (0 < sb1.Length)
                {
                    sb1.Append(" OR ");
                }
                sb1.Append(COLUMN_ID + "=" + item1.ID);
            }
            cmd0.CommandText = "DELETE FROM " + tableName + " WHERE " + sb1.ToString();

            return cmd0.ExecuteNonQuery();
        }

        protected int delete_(IEnumerable<long> ids0, SqlCommand cmd0)
        {
            if (ids0.Count() == 0) { return 0; }
            //
            StringBuilder sb1 = new StringBuilder();
            foreach (long id1 in ids0)
            {
                if (0 < sb1.Length)
                {
                    sb1.Append(" OR ");
                }
                sb1.Append(COLUMN_ID + "=" + id1);
            }
            cmd0.CommandText = "DELETE FROM " + tableName + " WHERE " + sb1.ToString();

            return cmd0.ExecuteNonQuery();
        }

        protected int delete_(long id0)
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        return delete_(new long[] { id0 }, cmd1);
                    }
                }
            }
            catch (Exception ex0)
            {
                errLog(ex0);
            }

            return -1;
        }

        protected abstract Dictionary<string, string> getFieldNameValues(T item0);

        public abstract T getItem(SqlDataReader reader0, ref int i0);

        public bool exists(long id0, SqlCommand cmd0)
        {
            return exists(id0, COLUMN_ID, cmd0);
        }

        public bool exists(long id0, string columnName0, SqlCommand cmd1)
        {
            if (id0 < 0) { return false; }
            //
            cmd1.CommandText = "SELECT " + columnName0 + " FROM " + tableName + " WHERE " + columnName0 + "=" + id0;
            long? res1 = cmd1.ExecuteScalar() as long?;

            return (res1 != null);
        }

        public List<long> exist(IEnumerable<long> ids0, string columnName0, SqlCommand cmd1)
        {
            List<long> ids1 = new List<long>();
            if (ids0.Count() == 0) { return ids1; }
            //
            StringBuilder sb1 = new StringBuilder();
            foreach (var item in ids0)
            {
                if (0 < sb1.Length)
                {
                    sb1.Append(" OR ");
                }
                sb1.Append(columnName0 + "=" + item);
            }
            cmd1.CommandText = "SELECT " + columnName0 + " FROM " + tableName + " WHERE " + sb1.ToString();
            using (SqlDataReader reader1 = cmd1.ExecuteReader())
            {
                while (reader1.Read())
                {
                    ids1.Add(
                        (long)reader1[columnName0]);
                }
            }

            return ids1;
        }

        /// <summary>
        /// シングル・クォートで囲む
        /// </summary>
        /// <param name="str1"></param>
        /// <returns></returns>
        protected static string q(string str1)
        {
            return "'" + str1 + "'";
        }

        /// <summary>
        /// シングルクォートをエスケープ
        /// シングルクォートで囲む
        /// </summary>
        /// <param name="text0"></param>
        /// <returns></returns>
        protected static string createTextValue(string text0)
        {
            return "N" + q(text0.Replace("'", "''"));
        }

        public void createDB_EDCB()
        {
            string query1 = "USE master;" +
                "CREATE DATABASE EDCB;";
            string query2 = "USE EDCB;" +
                  "CREATE FULLTEXT CATALOG " + FULLTEXTCATALOG + "; ";
            string connStr1 = "Data Source=" + machineName + "\\" + instanceName + ";Initial Catalog=master;Integrated Security=True";
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(connStr1))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = new SqlCommand(query1, sqlConn1))
                    {
                        cmd1.ExecuteNonQuery();
                    }
                    using (SqlCommand cmd1 = new SqlCommand(query2, sqlConn1))
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

        protected string convert2BitString(bool val0)
        {
            return val0 ? "1" : "0";
        }

        protected void createIndex(string indexName0, string[] columns0, SqlCommand cmd1)
        {
            StringBuilder sb1 = new StringBuilder();
            foreach (var item1 in columns0)
            {
                if (0 < sb1.Length)
                {
                    sb1.Append(", ");
                }
                sb1.Append(item1);
            }
            cmd1.CommandText = "IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'" + indexName0 + "')" +
                " CREATE INDEX " + indexName0 + " ON [dbo].[" + tableName + "](" + sb1.ToString() + ")";
            cmd1.ExecuteNonQuery();
        }

        #region - Property -
        #endregion

        /// <summary>
        /// 
        /// </summary>
        protected string sqlConnStr
        {
            get { return "Data Source=" + dataSource + ";Initial Catalog=EDCB;Integrated Security=True"; }
        }

        #region - Event Handler -
        #endregion

    }
}
