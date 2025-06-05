using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace IcarusDroneServiceApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Choose a folder under %LOCALAPPDATA% where the user always has write access
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logFolder = Path.Combine(appDataFolder, "IcarusDroneServiceApp", "Logs");
            Directory.CreateDirectory(logFolder);  // ensure the folder exists

            string tracePath = Path.Combine(logFolder, "trace.log");

            // Redirect Trace output to our own file
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener(tracePath));
            Trace.AutoFlush = true;
            Trace.TraceInformation("=== Icarus Drone Service starting ===");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Trace.TraceInformation("=== Icarus Drone Service exiting ===");
            Trace.Close();
            base.OnExit(e);
        }
    }
}
