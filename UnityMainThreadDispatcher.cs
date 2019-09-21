﻿/*
Copyright 2015 Pim de Witte All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

/// Author: Pim de Witte (pimdewitte.com) and contributors, https://github.com/PimDeWitte/UnityMainThreadDispatcher
/// <summary>
/// A thread-safe class which holds a queue with actions to execute on the next Update() method. It can be used to make calls to the main thread for
/// things such as UI Manipulation in Unity. It was developed for use in combination with the Firebase Unity plugin, which uses separate threads for event handling
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;
    private static Thread _mainThread = null;

    void Awake()
    {
        if (_instance == null)
        {
            _mainThread = Thread.CurrentThread;
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Update()
    {
        lock (ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
            {
                ExecutionQueue.Dequeue().Invoke();
            }
        }
    }

    void OnDestroy()
    {
        _instance = null;
    }

    /// <summary>
    /// Checks if a Dispatcher exists.
    /// </summary>
    /// <returns>Either true of false depending if the Dispatcher exists.</returns>
    public static bool Exists()
    {
        return _instance != null;
    }

    /// <summary>
    /// The current dispatcher.
    /// </summary>
    /// <returns>The dispatcher</returns>
    public static UnityMainThreadDispatcher Instance()
    {
        if (!Exists())
        {
            throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the MainThreadExecutor Prefab to your scene.");
        }

        return _instance;
    }

    private IEnumerator ActionWrapper(Action a)
    {
        a();
        yield return null;
    }

    /// <summary>
    /// Locks the queue and adds the IEnumerator to the queue
    /// </summary>
    /// <param name="action">IEnumerator function that will be executed from the main thread.</param>
    public void Enqueue(IEnumerator action)
    {
        lock (ExecutionQueue)
        {
            ExecutionQueue.Enqueue(() =>
            {
                StartCoroutine(action);
            });
        }
    }

    /// <summary>
    /// Locks the queue and adds the Action to the queue
    /// </summary>
    /// <param name="action">function that will be executed from the main thread.</param>
    public void Enqueue(Action action)
    {
        Enqueue(ActionWrapper(action));
    }

    /// <summary>
    /// Executes the function on the main thread and returns the result to the caller.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The function that will be executed on the main thread.</param>
    /// <returns>The result of the executed function.</returns>
    public static T Dispatch<T>(Func<T> func)
    {
        var obj = default(T);
        if (Thread.CurrentThread == _mainThread)
        {
            obj = func();
        }
        else
        {
            bool finished = false;
            Instance().Enqueue(() =>
            {
                obj = func();
                finished = true;
            });
            while (!finished) Thread.Yield();
        }

        return obj;
    }
}
