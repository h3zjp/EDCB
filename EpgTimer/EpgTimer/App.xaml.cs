using System;
using System.Windows;

namespace EpgTimer
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {

        App() {
#if DEBUG
            System.Diagnostics.DefaultTraceListener dtl = (System.Diagnostics.DefaultTraceListener)System.Diagnostics.Debug.Listeners["Default"];
            dtl.LogFileName = Environment.CurrentDirectory + "\\_DEBUG_LOG.txt";
#endif
        }

    }
}
