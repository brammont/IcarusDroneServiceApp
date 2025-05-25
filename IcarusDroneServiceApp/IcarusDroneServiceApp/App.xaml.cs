using System;
using System.Diagnostics;
using System.Windows;

namespace IcarusDroneServiceApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1) Write traces to VS Output and to trace.log
            Trace.Listeners.Add(new DefaultTraceListener());
            Trace.Listeners.Add(new TextWriterTraceListener("trace.log"));
            Trace.AutoFlush = true;

            Trace.TraceInformation("=== Application starting ===");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Trace.TraceInformation("=== Application exiting ===");
            base.OnExit(e);
        }
    }
}
