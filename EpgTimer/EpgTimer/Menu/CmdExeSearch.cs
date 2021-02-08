using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    public class CmdExeSearch : CmdExeReserve
    {

        #region - Constructor -
        #endregion

        public CmdExeSearch(Control owner) : base(owner)
        {
            cmdList.Add(EpgCmds.SetGenre, new cmdOption(mc_SetGenre, null, cmdExeType.SingleItem));
        }

        #region - Method -
        #endregion

        protected override void mcs_ctxmLoading_switch(ContextMenu ctxm, MenuItem menu)
        {
            base.mcs_ctxmLoading_switch(ctxm, menu);
            //
            // ジャンル登録
            //
            if (menu.Tag == EpgCmds.SetGenre)
            {
                SearchWindow sw1 = Owner as SearchWindow;
                if (sw1.listView_result.SelectedItem != null)
                {
                    SearchItem si1 = sw1.listView_result.SelectedItem as SearchItem;
                    if (si1 != null && si1.EventInfo.ContentInfo != null)
                    {
                        MenuUtil.addGenre(menu, si1.EventInfo.ContentInfo.nibbleList, (contentKindInfo0) =>
                        {
                            sw1.searchKeyView.setGenre(contentKindInfo0);
                        });
                    }
                }
            }
        }

        #region - Property -
        #endregion

        void mc_SetGenre(object sender, ExecutedRoutedEventArgs e) { }

        #region - Event Handler -
        #endregion

        #region - Field -
        #endregion

    }
}
