using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ExceptionDetector
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ExceptionDetector : MonoBehaviour
    {
        private Dictionary<string, int> methodThrows = new Dictionary<string, int>();
        private Queue<float> throwTime = new Queue<float>();
        private static Application.LogCallback stockDebug;
        private ScreenMessage throwDisplay;
        private float lastDisplayTime = float.NegativeInfinity;

        public void Awake()
        {
            DontDestroyOnLoad(this);
            Debug.Log("Hijacking the KSP logging utility!");
            stockDebug = (Application.LogCallback)typeof(Application).GetField("s_LogCallback", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Application.RegisterLogCallback(HandleLogEntry);
            Debug.Log("Hijack finished!");
        }

        public void Update()
        {
            if ((Time.realtimeSinceStartup - lastDisplayTime) > 0.2f)
            {
                lastDisplayTime = Time.realtimeSinceStartup;
                if (throwTime.Count > 0)
                {
                    //10 second average sampling
                    while (throwTime.Count > 0 && (throwTime.Peek() < (Time.realtimeSinceStartup - 10f)))
                    {
                        throwTime.Dequeue();
                    }
                    //Give us a message to write to
                    if (throwDisplay == null)
                    {
                        throwDisplay = ScreenMessages.PostScreenMessage("Something", float.PositiveInfinity, ScreenMessageStyle.UPPER_RIGHT);
                    }
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Throws per second: ");
                    sb.Append(throwTime.Count / 10f);
                    sb.AppendLine(" TPS.");
                    foreach (KeyValuePair<string, int> kvp in methodThrows)
                    {
                        sb.Append(kvp.Key);
                        sb.Append(" ");
                        sb.AppendLine(kvp.Value.ToString());
                    }
                    throwDisplay.message = sb.ToString();

                }
                else
                {
                    if (throwDisplay != null)
                    {
                        throwDisplay.duration = float.NegativeInfinity;
                        throwDisplay = null;
                    }
                }
            }
        }

        public void HandleLogEntry(string condition, string stackTrace, LogType logType)
        {
            if (logType == LogType.Exception)
            {
                if (stackTrace.Contains(" ("))
                {
                    string trimmedTrace = stackTrace.Substring(0, stackTrace.IndexOf(" ("));
                    throwTime.Enqueue(Time.realtimeSinceStartup);
                    if (!methodThrows.ContainsKey(trimmedTrace))
                    {
                        methodThrows.Add(trimmedTrace, 0);
                    }
                    methodThrows[trimmedTrace]++;
                }
            }
            stockDebug(condition, stackTrace, logType);
        }
    }
}

