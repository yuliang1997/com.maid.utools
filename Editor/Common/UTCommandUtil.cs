using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace UTools.Utility
{
    internal static class UTCommandUtil
    {
        internal static void ExecuteRG(string args, Action<List<string>> callback)
        {
            string fileName = EditorPrefs.GetString("ripgrep_path", "");
            if (string.IsNullOrEmpty(fileName))
            {
                if (UToolsUtil.IsMac)
                {
                    fileName = "/usr/local/bin/rg";
                }
                else
                {
                    fileName = UToolsSetting.depPath + "/rg.exe";
                }
            }

            if (!File.Exists(fileName))
            {
                fileName = EditorUtility.OpenFilePanel("Open Ripgrep", UToolsSetting.depPath, "*");
                EditorPrefs.SetString("ripgrep_path", fileName);
            }

            ExecuteCommand(fileName, args, callback);
        }

        internal static void ExecuteCommand(string fileName, string args, Action<List<string>> callback)
        {
//            Debug.LogWarning($"{info.binpath} {info.args}");

            var results = new List<string>();

            void WorkAction()
            {
                var psi = new ProcessStartInfo();
                psi.WindowStyle = ProcessWindowStyle.Maximized;
                psi.FileName = fileName;
                psi.Arguments = args;
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

                process.Close();
            }

            void AllDoneCallback()
            {
                Debug.LogWarning($"Results.Count:{results.Count}");
                callback?.Invoke(results);
            }

            EditorParallelUtil.RunThread(AllDoneCallback, WorkAction);
        }
    }
}