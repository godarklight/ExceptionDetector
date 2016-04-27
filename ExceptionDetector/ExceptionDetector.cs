using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ExceptionDetector
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ExceptionDetector : MonoBehaviour
    {
        //===Exception storage===
        //Key: Class name, Value: StackInfo
        private Dictionary<string, StackInfo> classCache = new Dictionary<string, StackInfo>();
        //Key: DLL name, Value: [Key: Class.Method name, Value: Number of throws]
        private Dictionary<string, Dictionary<StackInfo,int>> methodThrows = new Dictionary<string, Dictionary<StackInfo, int>>();
        //Time of all the throws
        private Queue<float> throwTime = new Queue<float>();
        //===Display state===
        private float lastDisplayTime = float.NegativeInfinity;
        private string displayState;
        private Rect windowRect = new Rect(10, 10, 400, 50);
        private GUILayoutOption[] expandOptions = null;
        private HashSet<string> kspDlls = new HashSet<string>();
        private HashSet<string> unityDlls = new HashSet<string>();

        public void Awake()
        {
            DontDestroyOnLoad(this);
            Application.logMessageReceived += HandleLogEntry;
            GUILayoutOption[] expandOptions = new GUILayoutOption[2];
            expandOptions[0] = GUILayout.ExpandWidth(true);
            expandOptions[1] = GUILayout.ExpandHeight(true);
            kspDlls.Add("assembly-csharp-firstpass");
            kspDlls.Add("assembly-csharp");
            kspDlls.Add("kspassets.dll");
            kspDlls.Add("kspcore.dll");
            kspDlls.Add("ksputil.dll");
            unityDlls.Add("unityengine.dll");
            unityDlls.Add("unityengine.networking.dll");
            unityDlls.Add("unityengine.ui.dll");
        }

        private void UpdateDisplayString()
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
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Throws per second: ");
                    sb.Append(throwTime.Count / 10f);
                    sb.AppendLine(" TPS.");
                    foreach (KeyValuePair<string, Dictionary<StackInfo, int>> dllEntry in methodThrows)
                    {
                        sb.AppendLine(dllEntry.Key);
                        foreach (KeyValuePair<StackInfo, int> methodThrowEntry in dllEntry.Value)
                        {
                            sb.Append("    ");
                            if (methodThrowEntry.Key.namespaceName != null)
                            {
                                sb.Append(methodThrowEntry.Key.namespaceName);
                                sb.Append(".");
                            }
                            sb.Append(methodThrowEntry.Key.className);
                            sb.Append(".");
                            sb.Append(methodThrowEntry.Key.methodName);
                            sb.Append(": ");
                            sb.AppendLine(methodThrowEntry.Value.ToString());
                        }
                    }
                    displayState = sb.ToString();
                }
                else
                {
                    displayState = null;
                }
            }
        }

        public void OnGUI()
        {
            //Update the state string every 0.2s so we can read it.
            if (Event.current.type == EventType.Layout)
            {
                UpdateDisplayString();
            }
            if (displayState != null)
            {
                //Random number
                windowRect = GUILayout.Window(1660952404, windowRect, DrawMethod, "Exception Detector", expandOptions);
            }
        }

        private void DrawMethod(int windowID)
        {
            GUI.DragWindow();
            GUILayout.Label(displayState);
        }

        public void HandleLogEntry(string condition, string stackTrace, LogType logType)
        {
            if (logType == LogType.Exception)
            {
                using (StringReader sr = new StringReader(stackTrace))
                {
                    string currentLine = null;
                    StackInfo firstInfo = null;
                    StackInfo stackInfo = null;
                    bool foundMod = false;
                    while (!foundMod && (currentLine = sr.ReadLine()) != null)
                    {
                        stackInfo = GetStackInfo(currentLine);
                        if (firstInfo == null)
                        {
                            firstInfo = stackInfo;
                        }
                        if (stackInfo.isMod)
                        {
                            //We found a mod in the trace, let's blame them.
                            foundMod = true;
                            break;
                        }
                    }
                    if (!foundMod)
                    {
                        //If we didn't find a mod, blame the method that threw.
                        stackInfo = firstInfo;
                    }
                    if (!methodThrows.ContainsKey(stackInfo.dllName))
                    {
                        methodThrows.Add(stackInfo.dllName, new Dictionary<StackInfo, int>());
                    }
                    if (!methodThrows[stackInfo.dllName].ContainsKey(stackInfo))
                    {
                        methodThrows[stackInfo.dllName].Add(stackInfo, 0);
                    }
                    methodThrows[stackInfo.dllName][stackInfo]++;
                }
                throwTime.Enqueue(Time.realtimeSinceStartup);
            }
        }

        public StackInfo GetStackInfo(string stackLine)
        {

            if (classCache.ContainsKey(stackLine))
            {
                return classCache[stackLine];
            }
            string processLine = stackLine.Substring(0, stackLine.LastIndexOf(" ("));
            string methodName = processLine.Substring(processLine.LastIndexOf(".") + 1);
            processLine = processLine.Substring(0, processLine.Length - (methodName.Length + 1));
            if (processLine.Contains("["))
            {
                processLine = processLine.Substring(0, processLine.IndexOf("["));
            }
            //UNITY WHY DO YOU HAVE TO BE SO BAD
            //Type foundType = Type.GetType(processLine);
            Type foundType = null;
            foreach (Assembly testAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type testType in testAssembly.GetExportedTypes())
                {
                    if (testType.FullName == processLine)
                    {
                        foundType = testType;
                        break;
                    }
                }
                if (foundType != null)
                {
                    break;
                }
            }
            if (foundType != null)
            {
                StackInfo retVal = new StackInfo();
                string dllPath = foundType.Assembly.Location;
                retVal.dllName = Path.GetFileNameWithoutExtension(dllPath);
                if (!dllPath.ToLower().Contains("gamedata"))
                {
                    retVal.isMod = false;
                    if (retVal.dllName.ToLower() == "mscorelib")
                    {
                        retVal.dllName = "Mono";
                    }
                    if (unityDlls.Contains(retVal.dllName.ToLower()))
                    {
                        retVal.dllName = "Unity";
                    }
                    if (kspDlls.Contains(retVal.dllName.ToLower()))
                    {
                        retVal.dllName = "KSP";
                    }
                }
                retVal.namespaceName = foundType.Namespace;
                retVal.className = foundType.Name;
                if (retVal.className.Contains("`"))
                {
                    retVal.className = retVal.className.Substring(0, retVal.className.IndexOf("`"));
                }
                retVal.methodName = methodName;
                classCache.Add(stackLine, retVal);
                return retVal;
            }
            StackInfo unknownVal = new StackInfo();
            unknownVal.dllName = "Unknown";
            unknownVal.className = processLine;
            if (unknownVal.className.Contains("`"))
            {
                unknownVal.className = unknownVal.className.Substring(0, unknownVal.className.IndexOf("`"));
            }
            unknownVal.methodName = methodName;
            classCache.Add(stackLine, unknownVal);
            return unknownVal;
        }
    }

    public class StackInfo
    {
        public bool isMod = true;
        public string dllName;
        public string namespaceName;
        public string className;
        public string methodName;
    }
}

