using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UTools.Utility
{
    [InitializeOnLoad]
    internal static class EditorParallelUtil
    {
        private class ParalleWork
        {
            internal List<Thread> threads;
            internal Action doneCallback;

            internal ParalleWork(List<Thread> threads, Action doneCallback)
            {
                this.threads = threads;
                this.doneCallback = doneCallback;
            }
        }

        private static List<ParalleWork> works = new List<ParalleWork>();
        internal static int DefaultMaxWorkThreadCount = Environment.ProcessorCount - 1;

        static EditorParallelUtil() => EditorApplication.update += Update;

        private static void Update()
        {
            for (var i = 0; i < works.Count; i++)
            {
                var work = works[i];
                var done = true;
                foreach (var v in work.threads)
                {
                    if (
                        (v.ThreadState & ThreadState.WaitSleepJoin) != 0 ||
                        v.ThreadState == ThreadState.Running ||
                        v.ThreadState == ThreadState.WaitSleepJoin
                    )
                    {
                        done = false;
                        break;
                    }
                    else
                    {
                        if (
                            (v.ThreadState & ThreadState.Stopped) != 0 && v.ThreadState != ThreadState.Stopped
                        )
                        {
                            Debug.LogWarning(v.ThreadState);
                        }
                    }
                }

                if (done)
                {
                    work.doneCallback?.Invoke();
                    works.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void WaitParallelEnd(List<Thread> threads, Action callback) => works.Add(new ParalleWork(threads, callback));

        internal static void RunParallel<T>(T data, Action allDoneCallback, Action<T> workAction) =>
            RunParallel(new List<T> {data}, allDoneCallback, workAction, 1);

        internal static void RunThread<T>(T data, Action allDoneCallback, Action<T> workAction) =>
            RunParallel(new List<T> {data}, allDoneCallback, workAction, 1);

        internal static void RunThread(Action allDoneCallback, Action workAction) => RunParallel(
            new List<object> {null},
            allDoneCallback,
            v => workAction.Invoke(),
            1
        );

        internal static void RunParallel<T>(IList<T> datas, Action allDoneCallback, Action<T> workAction, int maxWorkThreadCount = -1)
        {
            var threads = new List<Thread>();

            var datasQueue = new Queue<T>(datas);

            for (var i = 0;
                i < Mathf.Min(datas.Count, maxWorkThreadCount == -1 ? DefaultMaxWorkThreadCount : maxWorkThreadCount);
                i++)
            {
                var thread = new Thread(
                    () =>
                    {
                        while (true)
                        {
                            try
                            {
                                T data;
                                lock (datasQueue)
                                {
                                    if (datasQueue.Count <= 0)
                                    {
                                        return;
                                    }

                                    data = datasQueue.Dequeue();
                                }

                                workAction.Invoke(data);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }
                    }
                );
                thread.Start();
                threads.Add(thread);
            }

            WaitParallelEnd(threads, allDoneCallback);
        }
    }
}