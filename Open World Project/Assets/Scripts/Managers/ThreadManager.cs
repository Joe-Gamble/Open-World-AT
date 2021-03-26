using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public static class ThreadManager
{
    static List<Thread> open_threads = new List<Thread>();
    const int max_threads = 4;

    public static void StartThreadedFunction(Action function)
    {
        if (open_threads.Count <= max_threads)
        {
            Thread t = new Thread(new ThreadStart(function));
            t.Start();
        }
    }

}
