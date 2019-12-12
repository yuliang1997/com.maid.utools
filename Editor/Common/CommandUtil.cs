using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;

namespace UTools.Utility
{
    internal static class CommandUtil
    {
        internal static void ExecuteCommand(
            ArgumentsInfo info,
            Action<List<string>> callback
        )
        {
//            Debug.LogWarning($"{info.binpath} {info.args}");

            var results = new List<string>();

            void WorkAction()
            {
                var psi = new ProcessStartInfo();
                psi.WindowStyle = ProcessWindowStyle.Maximized;
                psi.FileName = info.binpath;
                psi.Arguments = info.args;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                var process = new Process();
                process.StartInfo = psi;

                var errorsb = new StringBuilder();

                void OnProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
                {
                    if (string.IsNullOrEmpty(e.Data))
                    {
                        return;
                    }

                    results.Add(e.Data);
                }

                void OnProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data.IsNNOE())
                    {
                        errorsb.AppendLine("Error: " + e.Data);
                    }
                }

                process.OutputDataReceived += OnProcessOnOutputDataReceived;
                process.ErrorDataReceived += OnProcessOnErrorDataReceived;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                if (errorsb.Length > 0)
                {
                    Debug.LogError(errorsb);
                }
            }

            void AllDoneCallback() => callback?.Invoke(results);

            EditorParallelUtil.RunThread(AllDoneCallback, WorkAction);
        }

        internal static void ExcuteApp(
            string appName,
            string argument = null,
            bool createNoWindow = false,
            string workingDirectory = null
        )
        {
            var info = new ProcessStartInfo(appName);
            info.Arguments = argument;
            info.CreateNoWindow = createNoWindow;
            info.ErrorDialog = true;
            info.UseShellExecute = true;

            info.WorkingDirectory = workingDirectory;

            var process = Process.Start(info);

            process.WaitForExit();

            process.Close();
        }
    }

    internal class ArgumentsInfo
    {
        internal string args;
        internal string binpath;

        internal ArgumentsInfo(string binpath) => this.binpath = binpath;

        internal void AddArgs(string arg) => args = $"{args} {arg}";
        internal void InsertArgs(string arg) => args = $"{arg} {args}";
    }
}