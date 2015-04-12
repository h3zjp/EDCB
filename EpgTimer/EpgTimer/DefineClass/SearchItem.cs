﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;

using CtrlCmdCLI;
using CtrlCmdCLI.Def;

namespace EpgTimer
{
    public class SearchItem
    {
        protected MenuUtil mutil = CommonManager.Instance.MUtil;
        protected ViewUtil vutil = CommonManager.Instance.VUtil;

        public virtual EpgEventInfo EventInfo { get; set; }
        public ReserveData ReserveInfo { get; set; }

        public SearchItem() { }
        public SearchItem(EpgEventInfo item)
        {
            EventInfo = item;
        }

        public bool IsReserved
        {
            get
            {
                return (ReserveInfo != null);
            }
        }

        public virtual String EventName
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return EventInfo.Title();
            }
        }
        public virtual String ServiceName
        {
            get
            {
                String view = "";
                if (EventInfo != null)
                {
                    UInt64 serviceKey = EventInfo.Create64Key();
                    if (ChSet5.Instance.ChList.ContainsKey(serviceKey) == true)
                    {
                        view = ChSet5.Instance.ChList[serviceKey].ServiceName;
                    }
                }
                return view;
            }
        }
        public virtual String NetworkName
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return CommonManager.Instance.ConvertNetworkNameText(EventInfo.original_network_id);
            }
        }
        public virtual String StartTime
        {
            get
            {
                if (EventInfo == null) return "";
                if (EventInfo.StartTimeFlag == 0) return "未定";
                //
                return EventInfo.start_time.ToString("yyyy/MM/dd(ddd) HH:mm:ss");
            }
        }
        /// <summary>
        /// 番組放送時間（長さ）
        /// </summary>
        public virtual TimeSpan ProgramDuration
        {
            get
            {
                if (EventInfo == null) { return new TimeSpan(); }
                //
                return TimeSpan.FromSeconds(EventInfo.durationSec);
            }
        }
        /// <summary>
        /// 番組内容
        /// </summary>
        public virtual String ProgramContent
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return EventInfo.ShortInfo.text_char.Replace("\r\n", " ");
            }
        }
        public String JyanruKey
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return CommonManager.Instance.ConvertJyanruText(EventInfo);
            }
        }
        public virtual TextBlock ToolTipView
        {
            get
            {
                if (Settings.Instance.NoToolTip == true) return null;

                String view = "";
                if (EventInfo != null)
                {
                    view = CommonManager.Instance.ConvertProgramText(EventInfo, EventInfoTextMode.All);
                }

                return mutil.GetTooltipBlockStandard(view);
            }
        }
        public virtual String Status
        {
            get
            {
                String[] wiewString = { "", "予", "無", "放", "予+", "無+", "録*", "無*" };
                int index = 0;
                if (EventInfo != null)
                {
                    if (EventInfo.IsOnAir() == true)
                    {
                        index = 3;
                    }
                    if (IsReserved == true)
                    {
                        if (ReserveInfo.IsOnRec() == true)//マージンがあるので、IsOnAir==trueとは限らない
                        {
                            index = 5;
                        }
                        if (ReserveInfo.RecSetting.RecMode == 5) //無効の判定
                        {
                            index += 2;
                        }
                        else
                        {
                            index += 1;
                        }
                    }
                }
                return wiewString[index];
            }
        }
        public virtual SolidColorBrush StatusColor
        {
            get
            {
                SolidColorBrush color = CommonManager.Instance.StatResForeColor;
                if (EventInfo != null)
                {
                    if (EventInfo.IsOnAir() == true)
                    {
                        color = CommonManager.Instance.StatOnAirForeColor;
                    }
                    if (IsReserved == true)
                    {
                        if (ReserveInfo.IsOnRec() == true)
                        {
                            color = CommonManager.Instance.StatRecForeColor;
                        }
                    }
                }
                return color;
            }
        }
        public SolidColorBrush ForeColor
        {
            get
            {
                if (ReserveInfo == null) return CommonManager.Instance.ListDefForeColor;
                //
                return CommonManager.Instance.EventItemForeColor(ReserveInfo.RecSetting.RecMode);
            }
        }
        public SolidColorBrush BackColor
        {
            get
            {
                SolidColorBrush color = CommonManager.Instance.ResDefBackColor;
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.RecSetting.RecMode == 5)
                    {
                        color = CommonManager.Instance.ResNoBackColor;
                    }
                    else if (ReserveInfo.OverlapMode == 2)
                    {
                        color = CommonManager.Instance.ResErrBackColor;
                    }
                    else if (ReserveInfo.OverlapMode == 1)
                    {
                        color = CommonManager.Instance.ResWarBackColor;
                    }
                }
                return color;
            }
        }
        public Brush BorderBrush
        {
            get
            {
                return vutil.EventDataBorderBrush(EventInfo);
            }
        }
    }

    public static class SearchItemEx
    {
        public static List<EpgEventInfo> EventInfoList(this ICollection<SearchItem> itemlist)
        {
            return itemlist.Where(item => item != null).Select(item => item.EventInfo).ToList();
        }
        public static List<EpgEventInfo> NoReserveInfoList(this ICollection<SearchItem> itemlist)
        {
            return itemlist.Where(item => item == null ? false : item.IsReserved == false).Select(item => item.EventInfo).ToList();
        }
        public static List<ReserveData> ReserveInfoList(this ICollection<SearchItem> itemlist)
        {
            return itemlist.Where(item => item == null ? false : item.IsReserved == true).Select(item => item.ReserveInfo).ToList();
        }
        public static bool HasReserved(this List<SearchItem> list)
        {
            return list.Any(info => info == null ? false : info.IsReserved);
        }
        public static bool HasNoReserved(this List<SearchItem> list)
        {
            return list.Any(info => info == null ? false : !info.IsReserved);
        }

    }
}
