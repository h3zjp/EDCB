using EpgTimer.DefineClass;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace EpgTimer.Common
{
    class DB_EpgSearchKeyInfo : DBBase<EpgSearchKeyInfoS>
    {

        public const string TABLE_NAME = "EpgSearchKeyInfo";
        public const string TABLE_NAME_ABBR = "eski";
        public const string COLUMN_lastUpdate = "lastUpdate",
            COLUMN_andKey = "andKey", COLUMN_caseFlag = "caseFlag", COLUMN_keyDisabledFlag = "keyDisabledFlag", COLUMN_notKey = "notKey",
            COLUMN_regExpFlag = "regExpFlag", COLUMN_titleOnlyFlag = "titleOnlyFlag", COLUMN_contentList = "contentList",
            COLUMN_dateList = "dateList", COLUMN_serviceList = "serviceList", COLUMN_videoList = "videoList", COLUMN_audioList = "audioList",
            COLUMN_aimaiFlag = "aimaiFlag", COLUMN_notContetFlag = "notContetFlag", COLUMN_notDateFlag = "notDateFlag", COLUMN_freeCAFlag = "freeCAFlag",
            COLUMN_chkRecEnd = "chkRecEnd", COLUMN_chkRecDay = "chkRecDay", COLUMN_chkRecNoService = "chkRecNoService",
            COLUMN_chkDurationMin = "chkDurationMin", COLUMN_chkDurationMax = "chkDurationMax";

        #region - Constructor -
        #endregion

        #region - Method -
        #endregion

        public int delete(EpgSearchKeyInfoS item0, SqlCommand cmd0)
        {
            return base.delete_(item0, cmd0);
        }

        public int update(EpgSearchKeyInfoS item0, SqlCommand cmd0)
        {
            return base.update_(item0, cmd0);
        }

        public EpgSearchKeyInfoS select(long epgSearchKeyInfoSID0, SqlCommand cmd0)
        {
            cmd0.CommandText = "SELECT * FROM " + tableName + " WHERE " + COLUMN_ID + " = " + epgSearchKeyInfoSID0;
            using (SqlDataReader reader1 = cmd0.ExecuteReader())
            {
                while (reader1.Read())
                {
                    int i1 = 0;
                    return getItem(reader1, ref i1);
                }
            }

            return null;
        }

        public override EpgSearchKeyInfoS getItem(SqlDataReader reader0, ref int i0)
        {
            EpgSearchKeyInfoS keyInfo1 = new EpgSearchKeyInfoS()
            {
                ID = (long)reader0[i0++],
                lastUpdate = (DateTime)reader0[i0++],
                andKey = (string)reader0[i0++],
                caseFlag = convertBool2Byte(reader0, ref i0),
                keyDisabledFlag = convertBool2Byte(reader0, ref i0),
                notKey = (string)reader0[i0++],
                regExpFlag = convertBool2Byte(reader0, ref i0),
                titleOnlyFlag = convertBool2Byte(reader0, ref i0),
                contentList = DB_EpgEventInfo.getEpgContentData(reader0, ref i0),
                dateList = getEpgSearchDateInfo(reader0, ref i0),
                serviceList = convertByte2Long(reader0, ref i0),
                videoList = convertByte2UShort(reader0, ref i0),
                audioList = convertByte2UShort(reader0, ref i0),
                aimaiFlag = convertBool2Byte(reader0, ref i0),
                notContetFlag = convertBool2Byte(reader0, ref i0),
                notDateFlag = convertBool2Byte(reader0, ref i0),
                freeCAFlag = convertBool2Byte(reader0, ref i0),
                chkRecEnd = convertBool2Byte(reader0, ref i0),
                chkRecDay = (ushort)(int)reader0[i0++],
                chkRecNoService = convertBool2Byte(reader0, ref i0),
                chkDurationMin = (ushort)(int)reader0[i0++],
                chkDurationMax = (ushort)(int)reader0[i0++],
            };

            return keyInfo1;
        }

        List<EpgSearchDateInfo> getEpgSearchDateInfo(SqlDataReader reader0, ref int i0)
        {
            List<EpgSearchDateInfo> list1 = new List<EpgSearchDateInfo>();
            byte[] bytes1 = (byte[])reader0[i0++];
            if (10 <= bytes1.Length)
            {
                int i1 = 0;
                while (i1 < bytes1.Length)
                {
                    list1.Add(
                         new EpgSearchDateInfo()
                         {
                             startDayOfWeek = bytes1[i1],
                             startHour = BitConverter.ToUInt16(bytes1, i1 += 1),
                             startMin = BitConverter.ToUInt16(bytes1, i1 += 2),
                             endDayOfWeek = bytes1[i1 += 2],
                             endHour = BitConverter.ToUInt16(bytes1, i1 += 1),
                             endMin = BitConverter.ToUInt16(bytes1, i1 += 2),
                         });
                    i1 += 2;
                }
            }

            return list1;
        }

        protected override Dictionary<string, string> getFieldNameValues(EpgSearchKeyInfoS item0)
        {
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            dict1.Add(COLUMN_lastUpdate, q(item0.lastUpdate.ToString(timeStampStrFormat)));
            dict1.Add(COLUMN_andKey, createTextValue(item0.andKey));
            dict1.Add(COLUMN_caseFlag, item0.caseFlag.ToString());
            dict1.Add(COLUMN_keyDisabledFlag, item0.keyDisabledFlag.ToString());
            dict1.Add(COLUMN_notKey, createTextValue(item0.notKey));
            dict1.Add(COLUMN_regExpFlag, item0.regExpFlag.ToString());
            dict1.Add(COLUMN_titleOnlyFlag, item0.titleOnlyFlag.ToString());
            dict1.Add(COLUMN_serviceList, "0x" + getByteString(item0.serviceList));
            dict1.Add(COLUMN_videoList, "0x" + getByteString(item0.videoList));
            dict1.Add(COLUMN_audioList, "0x" + getByteString(item0.audioList));
            dict1.Add(COLUMN_aimaiFlag, item0.aimaiFlag.ToString());
            dict1.Add(COLUMN_notContetFlag, item0.notContetFlag.ToString());
            dict1.Add(COLUMN_notDateFlag, item0.notDateFlag.ToString());
            dict1.Add(COLUMN_freeCAFlag, item0.freeCAFlag.ToString());
            dict1.Add(COLUMN_chkRecEnd, item0.chkRecEnd.ToString());
            dict1.Add(COLUMN_chkRecDay, item0.chkRecDay.ToString());
            dict1.Add(COLUMN_chkRecNoService, item0.chkRecNoService.ToString());
            dict1.Add(COLUMN_chkDurationMin, item0.chkDurationMin.ToString());
            dict1.Add(COLUMN_chkDurationMax, item0.chkDurationMax.ToString());
            DB_EpgEventInfo.addEpgContentData(ref dict1, COLUMN_contentList, item0.contentList);
            {
                StringBuilder sb1 = new StringBuilder();
                foreach (EpgSearchDateInfo item1 in item0.dateList)
                {
                    if (sb1.Length == 0)
                    {
                        sb1.Append("0x");
                    }
                    sb1.Append(item1.startDayOfWeek.ToString("X2"));
                    sb1.Append(getByteString(item1.startHour));
                    sb1.Append(getByteString(item1.startMin));
                    sb1.Append(item1.endDayOfWeek.ToString("X2"));
                    sb1.Append(getByteString(item1.endHour));
                    sb1.Append(getByteString(item1.endMin));
                }
                if (sb1.Length == 0)
                {
                    sb1.Append(0);
                }
                dict1.Add(COLUMN_dateList, sb1.ToString());
            }

            return dict1;
        }

        protected byte convertBool2Byte(SqlDataReader reader0, ref int i0)
        {
            return Convert.ToByte((bool)reader0[i0++]);
        }

        protected List<long> convertByte2Long(SqlDataReader reader0, ref int i0)
        {
            List<long> list1 = new List<long>();
            byte[] bytes1 = (byte[])reader0[i0++];
            if (1 < bytes1.Length)
            {
                for (int i1 = 0; i1 < bytes1.Length; i1 += 8)
                {
                    list1.Add(
                        BitConverter.ToInt64(bytes1, i1));
                }
            }

            return list1;
        }

        protected List<ushort> convertByte2UShort(SqlDataReader reader0, ref int i0)
        {
            List<ushort> list1 = new List<ushort>();
            byte[] bytes1 = (byte[])reader0[i0++];
            if (1 < bytes1.Length)
            {
                for (int i1 = 0; i1 < bytes1.Length; i1 += 2)
                {
                    list1.Add(
                        BitConverter.ToUInt16(bytes1, i1));
                }
            }

            return list1;
        }

        protected string getByteString(List<long> list0)
        {
            StringBuilder sb1 = new StringBuilder();
            if (list0.Count == 0)
            {
                sb1.Append("0");
            }
            else
            {
                foreach (var n1 in list0)
                {
                    foreach (var b1 in BitConverter.GetBytes(n1))
                    {
                        sb1.Append(b1.ToString("X2"));
                    }
                }
            }

            return sb1.ToString();
        }

        protected string getByteString(ushort us0)
        {
            StringBuilder sb1 = new StringBuilder();
            foreach (var b1 in BitConverter.GetBytes(us0))
            {
                sb1.Append(b1.ToString("X2"));
            }

            return sb1.ToString();
        }

        protected string getByteString(List<ushort> list0)
        {
            StringBuilder sb1 = new StringBuilder();
            if (list0.Count == 0)
            {
                sb1.Append("0");
            }
            else
            {
                foreach (var n1 in list0)
                {
                    foreach (var b1 in BitConverter.GetBytes(n1))
                    {
                        sb1.Append(b1.ToString("X2"));
                    }
                }
            }

            return sb1.ToString();
        }

        public void createTable(SqlCommand cmd0)
        {
            cmd0.CommandText = "CREATE TABLE [dbo].[" + TABLE_NAME + "](" +
                "[" + COLUMN_ID + "][bigint] IDENTITY(1,1) NOT NULL," +
                "[" + COLUMN_lastUpdate + "] [datetime] NOT NULL," +
                "[" + COLUMN_andKey + "] [nvarchar](1000) NOT NULL," +
                "[" + COLUMN_caseFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_keyDisabledFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_notKey + "] [nvarchar](1000) NOT NULL," +
                "[" + COLUMN_regExpFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_titleOnlyFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_contentList + "] [varbinary](40) NOT NULL," +
                "[" + COLUMN_dateList + "] [varbinary](200) NOT NULL," +
                "[" + COLUMN_serviceList + "] [varbinary](2000) NOT NULL," +
                "[" + COLUMN_videoList + "] [varbinary](20) NOT NULL," +
                "[" + COLUMN_audioList + "] [varbinary](20) NOT NULL," +
                "[" + COLUMN_aimaiFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_notContetFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_notDateFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_freeCAFlag + "] [bit] NOT NULL," +
                "[" + COLUMN_chkRecEnd + "] [bit] NOT NULL," +
                "[" + COLUMN_chkRecDay + "] [int] NOT NULL," +
                "[" + COLUMN_chkRecNoService + "] [bit] NOT NULL," +
                "[" + COLUMN_chkDurationMin + "] [int] NOT NULL," +
                "[" + COLUMN_chkDurationMax + "] [int] NOT NULL," +
                "CONSTRAINT [PK_EpgSearchKeyInfo] PRIMARY KEY CLUSTERED ([" + COLUMN_ID + "] ASC))";
            cmd0.ExecuteNonQuery();
        }

        public void alterTable_andKey_notKey()
        {
            try
            {
                using (SqlConnection sqlConn1 = new SqlConnection(sqlConnStr))
                {
                    sqlConn1.Open();
                    using (SqlCommand cmd1 = sqlConn1.CreateCommand())
                    {
                        cmd1.CommandText = "ALTER TABLE " + tableName + " ALTER COLUMN " + COLUMN_andKey + " nvarchar(1000) NOT NULL;";
                        cmd1.ExecuteNonQuery();
                        //
                        cmd1.CommandText = "ALTER TABLE " + tableName + " ALTER COLUMN " + COLUMN_notKey + " nvarchar(1000) NOT NULL; ";
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

