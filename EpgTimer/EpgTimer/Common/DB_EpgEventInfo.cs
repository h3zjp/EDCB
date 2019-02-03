using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace EpgTimer.Common
{
    /// <summary>
    /// DB_EpgEventInfoへの参照を持つテーブルを管理
    /// </summary>
    public interface IDB_EpgEventInfo
    {
        List<long> exist(IEnumerable<long> ids0, string columnName0, SqlCommand cmd1);
        string tableName { get; }
        string columnName_epgEventInfoID { get; }
    }

    public class DB_EpgEventInfo : DBBase<EpgEventInfoR>
    {
        /// <summary>
        /// key: table name
        /// </summary>
        public static Dictionary<string, IDB_EpgEventInfo> linkedTables = new Dictionary<string, IDB_EpgEventInfo>();

        public const string TABLE_NAME = "EpgEventInfo";
        /// <summary>
        /// 略記
        /// </summary>
        public const string TABLE_NAME_ABBR = "eei";
        public const string COLUMN_lastUpdate = "lastUpdate",
            COLUMN_original_network_id = "original_network_id", COLUMN_transport_stream_id = "transport_stream_id",
            COLUMN_service_id = "service_id", COLUMN_event_id = "event_id",
            COLUMN_StartTimeFlag = "StartTimeFlag", COLUMN_start_time = "start_time",
            COLUMN_DurationFlag = "DurationFlag", COLUMN_durationSec = "durationSec",
            COLUMN_ShortInfo_event_name = "ShortInfo_event_name", COLUMN_ShortInfo_text_char = "ShortInfo_text_char",
            COLUMN_ExtInfo_text_char = "ExtInfo_text_char",
            COLUMN_ContentInfo = "ContentInfo", COLUMN_ComponentInfo = "ComponentInfo", COLUMN_AudioInfo = "AudioInfo";
        const string INDEX_NAME = "UX_Epg";

        #region - Constructor -
        #endregion

        public DB_EpgEventInfo() { }

        public DB_EpgEventInfo(IDB_EpgEventInfo linkedTable0)
        {
            if (!linkedTables.ContainsKey(linkedTable0.tableName))
            {
                linkedTables.Add(linkedTable0.tableName, linkedTable0);
            }
        }

        #region - Method -
        #endregion

        public int update(EpgEventInfoR item0)
        {
            return base.update_(item0);
        }

        public long insert(EpgEventInfoR item0, SqlCommand cmd0)
        {
            StringBuilder query1 = new StringBuilder();
            query1.AppendLine("DECLARE @id BIGINT");
            query1.AppendLine("SELECT @id = " +
                "(" +
                "SELECT " + COLUMN_ID + " FROM " + TABLE_NAME +
                " WHERE " + COLUMN_original_network_id + "=" + item0.original_network_id +
                 " AND " + COLUMN_transport_stream_id + "=" + item0.transport_stream_id +
                 " AND " + COLUMN_service_id + "=" + item0.service_id +
                 " AND " + COLUMN_event_id + "=" + item0.event_id +
                 " AND " + COLUMN_start_time + "= " + q(item0.start_time.ToString(startTimeStrFormat)) +
                 ")");
            query1.AppendLine("IF @id IS NULL");
            query1.AppendLine(getQuery_Insert(item0));
            query1.AppendLine("ELSE");
            query1.AppendLine("SELECT @id");
            cmd0.CommandText = query1.ToString();
            long id1 = (long)cmd0.ExecuteScalar();

            return id1;
        }

        /// <summary>
        /// epgが登録済みであれば、既存データのIDを返す
        /// </summary>
        /// <param name="item0"></param>
        /// <returns></returns>
        public long insert(EpgEventInfoR item0)
        {
            long id1 = -1;

            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        id1 = insert(item0, cmd1);
                    }
                }
            }
            catch (Exception ex0)
            {
                System.Diagnostics.Trace.WriteLine(ex0);
            }

            return id1;
        }

        public static List<EpgContentData> getEpgContentData(SqlDataReader reader0, ref int i0)
        {
            List<EpgContentData> nibbleList1 = new List<EpgContentData>();
            byte[] bytes1 = (byte[])reader0[i0++];
            if (4 <= bytes1.Length)
            {
                int i1 = 0;
                while (i1 < bytes1.Length)
                {
                    EpgContentData ecd1 = new EpgContentData()
                    {
                        content_nibble_level_1 = bytes1[i1++],
                        content_nibble_level_2 = bytes1[i1++],
                        user_nibble_1 = bytes1[i1++],
                        user_nibble_2 = bytes1[i1++]
                    };
                    if (ecd1.content_nibble_level_1 == 0 && ecd1.content_nibble_level_2 == 0 && ecd1.user_nibble_1 == 0 && ecd1.user_nibble_2 == 0)
                    {
                        continue;
                    }
                    else
                    {
                        nibbleList1.Add(ecd1);
                    }
                }
            }

            return nibbleList1;
        }

        public override EpgEventInfoR getItem(SqlDataReader reader0, ref int i0)
        {
            EpgEventInfoR epgEventInfoR1 = new EpgEventInfoR()
            {
                ID = (long)reader0[i0++],
                lastUpdate = (DateTime)reader0[i0++],
                original_network_id = (ushort)(int)reader0[i0++],
                transport_stream_id = (ushort)(int)reader0[i0++],
                service_id = (ushort)(int)reader0[i0++],
                event_id = (ushort)(int)reader0[i0++],
                StartTimeFlag = Convert.ToByte(reader0[i0++]),
                start_time = (DateTime)reader0[i0++],
                DurationFlag = Convert.ToByte(reader0[i0++]),
                durationSec = (uint)(long)reader0[i0++],
                ShortInfo = new EpgShortEventInfo()
                {
                    event_name = (string)reader0[i0++],
                    text_char = (string)reader0[i0++]
                },
                ExtInfo = new EpgExtendedEventInfo()
                {
                    text_char = (string)reader0[i0++]
                },
                ContentInfo = new EpgContentInfo()
                {
                    nibbleList = getEpgContentData(reader0, ref i0)
                },
                ComponentInfo = getComponentInfo(reader0, ref i0),
                AudioInfo = getAudioInfo(reader0, ref i0),
            };

            return epgEventInfoR1;
        }

        EpgAudioComponentInfo getAudioInfo(SqlDataReader reader0, ref int i0)
        {
            object o1 = reader0[i0++];
            if (!o1.Equals(DBNull.Value))
            {
                byte[] bytes1 = (byte[])o1;
                int i1 = 0;
                EpgAudioComponentInfo epgAudioComponentInfo1 = new EpgAudioComponentInfo();
                while (i1 < bytes1.Length)
                {
                    epgAudioComponentInfo1.componentList.Add(
                        new EpgAudioComponentInfoData()
                        {
                            stream_content = bytes1[i1++],
                            component_type = bytes1[i1++],
                            component_tag = bytes1[i1++],
                            stream_type = bytes1[i1++],
                            simulcast_group_tag = bytes1[i1++],
                            ES_multi_lingual_flag = bytes1[i1++],
                            main_component_flag = bytes1[i1++],
                            quality_indicator = bytes1[i1++],
                            sampling_rate = bytes1[i1++],
                        });
                }
                return epgAudioComponentInfo1;
            }
            else
            {
                return null;
            }
        }

        EpgComponentInfo getComponentInfo(SqlDataReader reader0, ref int i0)
        {
            object o1 = reader0[i0++];
            if (!o1.Equals(DBNull.Value))
            {
                byte[] bytes1 = (byte[])o1;
                int i1 = 0;
                EpgComponentInfo epgComponentInfo1 = new EpgComponentInfo()
                {
                    stream_content = bytes1[i1++],
                    component_type = bytes1[i1++],
                    component_tag = bytes1[i1++]
                };
                return epgComponentInfo1;
            }
            else
            {
                return null;
            }
        }

        protected override Dictionary<string, string> getFieldNameValues(EpgEventInfoR item0)
        {
            string shortInfo_event_name1 = string.Empty;
            string shortInfo_text_char1 = string.Empty;
            if (item0.ShortInfo != null)
            {
                shortInfo_event_name1 = item0.ShortInfo.event_name;
                shortInfo_text_char1 = item0.ShortInfo.text_char;
            }
            string extInfo_text_char1 = string.Empty;
            if (item0.ExtInfo != null)
            {
                extInfo_text_char1 = item0.ExtInfo.text_char;
            }
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            dict1.Add(COLUMN_lastUpdate, q(item0.lastUpdate.ToString(timeStampStrFormat)));
            dict1.Add(COLUMN_original_network_id, item0.original_network_id.ToString());
            dict1.Add(COLUMN_transport_stream_id, item0.transport_stream_id.ToString());
            dict1.Add(COLUMN_service_id, item0.service_id.ToString());
            dict1.Add(COLUMN_event_id, item0.event_id.ToString());
            dict1.Add(COLUMN_StartTimeFlag, "0x" + item0.StartTimeFlag.ToString("X"));
            dict1.Add(COLUMN_DurationFlag, "0x" + item0.DurationFlag.ToString("X"));
            dict1.Add(COLUMN_durationSec, item0.durationSec.ToString());
            dict1.Add(COLUMN_ShortInfo_event_name, createTextValue(shortInfo_event_name1));
            dict1.Add(COLUMN_ShortInfo_text_char, createTextValue(shortInfo_text_char1));
            dict1.Add(COLUMN_ExtInfo_text_char, createTextValue(extInfo_text_char1));
            if (item0.start_time < minValue_DateTime)
            {
                dict1.Add(COLUMN_start_time, q(minValue_DateTime.ToString(startTimeStrFormat)));
            }
            else
            {
                dict1.Add(COLUMN_start_time, q(item0.start_time.ToString(startTimeStrFormat)));
            }
            if (item0.ContentInfo != null)
            {
                addEpgContentData(ref dict1, COLUMN_ContentInfo, item0.ContentInfo.nibbleList);
            }
            else
            {
                dict1.Add(COLUMN_ContentInfo, "0");
            }
            if (item0.ComponentInfo != null)
            {
                addEpgComponentInfo(ref dict1, COLUMN_ComponentInfo, item0.ComponentInfo);
            }
            if (item0.AudioInfo != null)
            {
                addEpgAudioComponentInfo(ref dict1, COLUMN_AudioInfo, item0.AudioInfo);
            }

            return dict1;
        }

        void addEpgAudioComponentInfo(ref Dictionary<string, string> dict0, string column0, EpgAudioComponentInfo audioInfo0)
        {
            int i1 = 0;
            StringBuilder sb1 = new StringBuilder();
            foreach (EpgAudioComponentInfoData epgAudioComponentInfoData1 in audioInfo0.componentList)
            {
                if (sb1.Length == 0)
                {
                    sb1.Append("0x");
                }
                sb1.Append(epgAudioComponentInfoData1.stream_content.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.component_type.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.component_tag.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.stream_type.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.simulcast_group_tag.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.ES_multi_lingual_flag.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.main_component_flag.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.quality_indicator.ToString("X2"));
                sb1.Append(epgAudioComponentInfoData1.sampling_rate.ToString("X2"));
                i1++;
                if (2 <= i1) { break; } // ２つまで
            }
            dict0.Add(column0, sb1.ToString());
        }

        void addEpgComponentInfo(ref Dictionary<string, string> dict0, string column0, EpgComponentInfo componentInfo0)
        {
            StringBuilder sb1 = new StringBuilder("0x");
            sb1.Append(componentInfo0.stream_content.ToString("X2"));
            sb1.Append(componentInfo0.component_type.ToString("X2"));
            sb1.Append(componentInfo0.component_tag.ToString("X2"));
            dict0.Add(column0, sb1.ToString());
        }

        public static void addEpgContentData(ref Dictionary<string, string> dict0, string column0, List<EpgContentData> ecdList0)
        {
            StringBuilder sb1 = new StringBuilder();
            foreach (EpgContentData epgContentData1 in ecdList0)
            {
                if (sb1.Length == 0)
                {
                    sb1.Append("0x");
                }
                sb1.Append(epgContentData1.content_nibble_level_1.ToString("X2"));
                sb1.Append(epgContentData1.content_nibble_level_2.ToString("X2"));
                sb1.Append(epgContentData1.user_nibble_1.ToString("X2"));
                sb1.Append(epgContentData1.user_nibble_2.ToString("X2"));
            }
            if (sb1.Length == 0)
            {
                sb1.Append(0);
            }
            dict0.Add(column0, sb1.ToString());
        }

        public void alterTable_COLUMN_ExtInfo_text_char()
        {
            string query1 = "DROP FULLTEXT INDEX ON [dbo].[" + TABLE_NAME + "]";
            string query2 = "ALTER TABLE [dbo].[" + TABLE_NAME + "] ALTER COLUMN " + COLUMN_ExtInfo_text_char + " nvarchar(3000) NOT NULL";
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
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
                System.Diagnostics.Trace.WriteLine(ex0);
            }

            createFulltext();
        }

        public void createTable()
        {
            //epgサンプル数: 37073
            //MAX_ShortInfo_event_name.Length: 62
            //MAX_ShortInfo_text_char.Length: 130
            //MAX_ExtInfo_text_char.Length: 1449
            // Max_ContentInfo_nibbleList_Count1: 5 * 4 = 20 byte

            //epg_Sample: 32352
            //Max_ShortInfo_event_name.Length: 62
            //Max_ShortInfo_text_char.Length: 123
            //Max_ExtInfo_text_char.Length: 2503
            //Max_ContentInfo_nibbleList_Count1: 4

            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        cmd1.CommandText = "CREATE TABLE [dbo].[" + TABLE_NAME + "](" +
                            "[" + COLUMN_ID + "] [bigint] IDENTITY(1,1) NOT NULL," +
                            "[" + COLUMN_lastUpdate + "] [datetime] NOT NULL," +
                            "[" + COLUMN_original_network_id + "] [int] NOT NULL," +
                            "[" + COLUMN_transport_stream_id + "] [int] NOT NULL," +
                            "[" + COLUMN_service_id + "] [int] NOT NULL," +
                            "[" + COLUMN_event_id + "] [int] NOT NULL," +
                            "[" + COLUMN_StartTimeFlag + "] [bit] NOT NULL," +
                            "[" + COLUMN_start_time + "] [smalldatetime] NOT NULL," +
                            "[" + COLUMN_DurationFlag + "] [bit] NOT NULL," +
                            "[" + COLUMN_durationSec + "] [bigint] NOT NULL," +
                            "[" + COLUMN_ShortInfo_event_name + "] [nvarchar](100) NOT NULL," +
                            "[" + COLUMN_ShortInfo_text_char + "] [nvarchar](200) NOT NULL," +
                            "[" + COLUMN_ExtInfo_text_char + "] [nvarchar](3000) NOT NULL," +
                            "[" + COLUMN_ContentInfo + "] [varbinary](20) NOT NULL," +
                            "[" + COLUMN_ComponentInfo + "] [binary](3)," +
                            "[" + COLUMN_AudioInfo + "] [varbinary](18)," +
                            "CONSTRAINT [PK_EpgEventInfo] PRIMARY KEY CLUSTERED ([" + COLUMN_ID + "] ASC))";
                        cmd1.ExecuteNonQuery();
                        //
                        createIndex(cmd1);
                    }
                }
            }
            catch (Exception ex0)
            {
                System.Diagnostics.Trace.WriteLine(ex0);
            }

            createFulltext();
        }

        void createFulltext()
        {
            string query1 = "CREATE FULLTEXT INDEX ON " + TABLE_NAME + "(" +
                COLUMN_ShortInfo_event_name + " Language 1041," +
                COLUMN_ShortInfo_text_char + " Language 1041," +
                COLUMN_ExtInfo_text_char + " Language 1041" +
                ") KEY INDEX PK_EpgEventInfo ON " + FULLTEXTCATALOG + ";";
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
                System.Diagnostics.Trace.WriteLine(ex0);
            }
        }

        public void createIndex(SqlCommand cmd0)
        {
            base.createIndex(
                INDEX_NAME,
                new string[] {
                    COLUMN_start_time,
                    COLUMN_original_network_id,
                    COLUMN_transport_stream_id,
                    COLUMN_service_id,
                    COLUMN_event_id },
                cmd0);
        }

        /// <summary>
        /// 他のテーブルから参照されていなければ更新
        /// RecLogItem.epgAllowOverWriteフラグ優先
        /// </summary>
        /// <param name="epgList0"></param>
        /// <param name="tableName0"></param>
        /// <returns></returns>
        public int updateIfNotReferenced(List<EpgEventInfoR> epgList0, string tableName0, SqlCommand cmd1)
        {
            List<long> ids1 = new List<long>();
            foreach (var item in epgList0)
            {
                ids1.Add(item.ID);
            }
            List<long> ids2 = new List<long>();
            foreach (KeyValuePair<string, IDB_EpgEventInfo> item1 in linkedTables)
            {
                if (item1.Key == tableName0) { continue; }
                //
                ids2.AddRange(
                    item1.Value.exist(ids1, item1.Value.columnName_epgEventInfoID, cmd1));
            }
            List<EpgEventInfoR> epgList1 = new List<EpgEventInfoR>();
            foreach (var epg1 in epgList0)
            {
                if (!ids2.Contains(epg1.ID))
                {
                    epgList1.Add(epg1);
                }
            }
            return update_(epgList1, cmd1);
        }

        /// <summary>
        /// 参照されていなければ削除
        /// </summary>
        /// <param name="ids0"></param>
        /// <param name="cmd0"></param>
        /// <returns></returns>
        public int delete(IEnumerable<long> ids0, SqlCommand cmd0)
        {
            List<long> idList1 = new List<long>();
            foreach (IDB_EpgEventInfo db1 in linkedTables.Values)
            {
                idList1.AddRange(
                    db1.exist(ids0, db1.columnName_epgEventInfoID, cmd0));
            }
            List<long> ids2del1 = new List<long>();
            foreach (var id1 in ids0)
            {
                if (!idList1.Contains(id1))
                {
                    ids2del1.Add(id1);
                }
            }
            return base.delete_(ids0, cmd0);
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

    /// <summary>
    /// DBレコード
    /// </summary>
    public class EpgEventInfoR : EpgEventInfo, IDBRecord
    {

        ComponentInfoAnalyzer _componentInfoAnalyzer;

        #region - Constructor -
        #endregion

        public EpgEventInfoR()
        {
            ShortInfo = new EpgShortEventInfo();
            ExtInfo = new EpgExtendedEventInfo();
            ContentInfo = new EpgContentInfo();
            _componentInfoAnalyzer = new ComponentInfoAnalyzer(this);
        }

        public EpgEventInfoR(RecFileInfo recFileInfo0, DateTime lastUpdate0) : this()
        {
            base.original_network_id = recFileInfo0.OriginalNetworkID;
            base.transport_stream_id = recFileInfo0.TransportStreamID;
            base.service_id = recFileInfo0.ServiceID;
            base.event_id = recFileInfo0.EventID;
            base.start_time = recFileInfo0.StartTime;
            base.durationSec = recFileInfo0.DurationSecond;
            base.ShortInfo = new EpgShortEventInfo()
            {
                event_name = recFileInfo0.Title
            };

            lastUpdate = lastUpdate0;
        }

        public EpgEventInfoR(EpgEventInfo epgEventInfo0, DateTime lastUpdate0) : this()
        {
            if (epgEventInfo0 != null)
            {
                base.original_network_id = epgEventInfo0.original_network_id;
                base.transport_stream_id = epgEventInfo0.transport_stream_id;
                base.service_id = epgEventInfo0.service_id;
                base.event_id = epgEventInfo0.event_id;
                base.StartTimeFlag = epgEventInfo0.StartTimeFlag;
                base.start_time = epgEventInfo0.start_time;
                base.DurationFlag = epgEventInfo0.DurationFlag;
                base.durationSec = epgEventInfo0.durationSec;
                base.ShortInfo = epgEventInfo0.ShortInfo;
                base.ExtInfo = epgEventInfo0.ExtInfo;
                base.ContentInfo = epgEventInfo0.ContentInfo;
                base.ComponentInfo = epgEventInfo0.ComponentInfo;
                base.AudioInfo = epgEventInfo0.AudioInfo;
                base.EventGroupInfo = epgEventInfo0.EventGroupInfo;
                base.EventRelayInfo = epgEventInfo0.EventRelayInfo;
            }

            lastUpdate = lastUpdate0;
        }

        #region - Method -
        #endregion

        public override bool Equals(object obj) {
            EpgEventInfo info0 = (EpgEventInfo)obj;
            return (original_network_id == info0.original_network_id
                && transport_stream_id == info0.transport_stream_id
                && service_id == info0.service_id
                && event_id == info0.event_id);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public void reserveDelete()
        {
            ReserveData reserveData1 = getReserveData();
            if (reserveData1 == null)
            {
                return;
            }
            MenuUtil.ReserveDelete(
               new List<ReserveData>() { reserveData1 });
        }

        public void reserveAdd()
        {
            MenuUtil.ReserveAdd(
                new List<EpgEventInfo>() { this },
                null);
        }

        public void reserveAdd_ChangeOnOff()
        {
            ReserveData reserveData1 = getReserveData();
            if (reserveData1 == null)
            {
                MenuUtil.ReserveAdd(
                    new List<EpgEventInfo>() { this },
                    null);
            }
            else
            {
                MenuUtil.ReserveChangeOnOff(
                    new List<ReserveData>() { reserveData1 });
            }
        }

        public void openReserveDialog(Control owner0)
        {
            ReserveData reserveData1 = getReserveData();
            if (reserveData1 == null)
            {
                MenuUtil.OpenEpgReserveDialog(this, 1);
                //MenuUtil.OpenEpgReserveDialog(this, owner0, 1);
            }
            else
            {
                MenuUtil.OpenChgReserveDialog(reserveData1, 1);
                //MenuUtil.OpenChgReserveDialog(reserveData1, owner0, 1);
            }
        }

        ReserveData getReserveData()
        {
            var query1 = CommonManager.Instance.DB.ReserveList.Values.Where(
                x1 =>
                {
                    return (transport_stream_id == x1.TransportStreamID
                    && original_network_id == x1.OriginalNetworkID
                    && service_id == x1.ServiceID
                    && event_id == x1.EventID);
                });
            foreach (ReserveData reserveData1 in query1)
            {
                return reserveData1;
            }

            return null;
        }

        #region - Property -
        #endregion

        public string Title
        {
            get
            {
                if (ShortInfo != null)
                {
                    return ShortInfo.event_name;
                }
                else
                {
                    return null;
                }
            }
        }

        public long ID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        long _ID = -1;

        public DateTime lastUpdate { get; set; }

        /// <summary>
        /// SD, HD, FHD, UHD
        /// </summary>
        public string imageQuality
        {
            get { return _componentInfoAnalyzer.imageQuality; }
        }

        /// <summary>
        ///
        /// </summary>
        public Brush foreground_ImageQuality
        {
            get { return _componentInfoAnalyzer.foreground_ImageQuality; }
        }

        #region - Event Handler -
        #endregion

        #region - Inner Class -
        #endregion

        /// <summary>
        /// ComponentInfoから画質を取得
        /// </summary>
        class ComponentInfoAnalyzer
        {

            EpgEventInfo _epgEventInfo;
            List<int> _SD = new List<int>();
            List<int> _HD = new List<int>();
            List<int> _FHD = new List<int>();
            List<int> _UHD = new List<int>();

            #region - Constructor -
            #endregion

            public ComponentInfoAnalyzer(EpgEventInfo epgEventInfo0)
            {
                _epgEventInfo = epgEventInfo0;
                //
                int[] keys_SD1 = new int[] { 0x0101, 0x01A1, 0x01D1, 0x01D1, 0x05A1, 0x05D1 };
                foreach (var item in keys_SD1)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _SD.Add(item + i);
                    }
                }
                //
                int[] keys_HD1 = new int[] { 0x01C1, 0x05C1 };
                foreach (var item in keys_HD1)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _HD.Add(item + i);
                    }
                }
                //
                int[] keys_FHD1 = new int[] { 0x01B1, 0x01E1, 0x05B1, 0x05E1 };
                foreach (var item in keys_FHD1)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _FHD.Add(item + i);
                    }
                }
                //
                int[] keys_UHD1 = new int[] { 0x0191, 0x0591 };
                foreach (var item in keys_UHD1)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _UHD.Add(item + i);
                    }
                }
            }

            #region - Method -
            #endregion

            void setProperty()
            {
                if (_epgEventInfo.ComponentInfo == null) { return; }
                //
                UInt16 componentKey = (UInt16)(_epgEventInfo.ComponentInfo.stream_content << 8 | _epgEventInfo.ComponentInfo.component_type);
                if (_SD.Contains(componentKey))
                {
                    _imageQuality = "SD";
                    _foreground_ImageQuality = Brushes.Navy;
                }
                else if (_HD.Contains(componentKey))
                {
                    _imageQuality = "HD";
                    _foreground_ImageQuality = Brushes.OrangeRed;
                }
                else if (_FHD.Contains(componentKey))
                {
                    _imageQuality = "FHD";
                    _foreground_ImageQuality = Brushes.Firebrick;
                }
                else if (_UHD.Contains(componentKey))
                {
                    _imageQuality = "UHD";
                    _foreground_ImageQuality = Brushes.Gold;
                }
                else
                {
                    _imageQuality = null;
                    _foreground_ImageQuality = null;
                }
            }

            #region - Property -
            #endregion

            /// <summary>
            /// 
            /// </summary> 
            public string imageQuality
            {
                get
                {
                    if (this._imageQuality == null)
                    {
                        setProperty();
                    }
                    return this._imageQuality;
                }
            }
            string _imageQuality = null;

            /// <summary>
            /// 
            /// </summary> 
            public Brush foreground_ImageQuality
            {
                get
                {
                    if (this._foreground_ImageQuality == null)
                    {
                        setProperty();
                    }
                    return this._foreground_ImageQuality;
                }
            }
            Brush _foreground_ImageQuality = null;

            #region - Event Handler -
            #endregion

        }

    }

}
