using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEngine;

namespace ExceptionDetectorUpdated
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class ExceptionDetectorUpdated : MonoBehaviour
	{
		#region needscleaned
		//===Exception storage===
		//Key: Class name, Value: StackInfo
		private static Dictionary<string, StackInfo> classCache = new Dictionary<string, StackInfo>();
		//Key: DLL name, Value: [Key: Class.Method name, Value: Number of throws]
		private static Dictionary<string, Dictionary<StackInfo, int>> methodThrows = new Dictionary<string, Dictionary<StackInfo, int>>();
		//Time of all the throws
		private static Queue<float> throwTime = new Queue<float>();
		//===Display state===
		private float lastDisplayTime = float.NegativeInfinity;
		private string displayState;
		private Rect windowRect = new Rect(10, 10, 400, 50);
		private GUILayoutOption[] expandOptions = null;
		private static HashSet<string> kspDlls = new HashSet<string>();
		private static HashSet<string> unityDlls = new HashSet<string>();
		private HashSet<string> textureErrors = new HashSet<string>();

		private static int stackLogTick = 0;
		private static string preStack = String.Empty;

		internal static Dictionary<string, string> SinglePassValues = new Dictionary<string, string>();
		internal static Dictionary<string, string> DoublePassValues = new Dictionary<string, string>();
		private static Dictionary<string, HashSet<string>> _errors = new Dictionary<string, HashSet<string>>();
		internal static Dictionary<string, ulong> ExceptionCount = new Dictionary<string, ulong>();
		private static string prvConditionStatement = String.Empty;

		private static readonly string _assemblyPath = Path.GetDirectoryName(typeof(ExceptionDetectorUpdated).Assembly.Location);
		internal static String SettingsFile { get; } = Path.Combine(_assemblyPath, "settings.cfg");
		internal static String LogFile { get; } = Path.Combine(_assemblyPath, "Log/edu.log");
		private IssueGUI fiGui;
		public static ExceptionDetectorUpdated Instance { get; private set; }
		internal static string strMessage = String.Empty;
		#endregion

		#region Properties
		public static bool FullLog { get; set; } = false;
		public static bool HideKnowns { get; set; } = false;
		public static bool ShowInfoMessage { get; set; } = false;
		#endregion

		public void Awake()
		{
			Instance = this;
			InitLog();
			Config.Load();
			DontDestroyOnLoad(this);
			Application.logMessageReceivedThreaded += HandleLogEntry;
			GUILayoutOption[] expandOptions = new GUILayoutOption[2];
			expandOptions[0] = GUILayout.ExpandWidth(true);
			expandOptions[1] = GUILayout.ExpandHeight(true);
			kspDlls.Add("assembly-csharp-firstpass");
			kspDlls.Add("assembly-csharp");
			kspDlls.Add("kspassets.dll");
			//kspDlls.Add("kspcore.dll");
			// kspDlls.Add("ksputil.dll");
			unityDlls.Add("unityengine.dll");
			unityDlls.Add("unityengine.networking.dll");
			unityDlls.Add("unityengine.ui.dll");
		}

		public void OnDestroy()
		{
			try
			{
				if (fiGui != null)
				{
					Destroy(fiGui);
				}
			}
			catch (Exception ex)
			{
				WriteLog(ex.ToString());
			}
		}

		protected void Start()
		{
			fiGui = gameObject.AddComponent<IssueGUI>();
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
			try
			{
				if (fiGui != null) fiGui.OnGUI();
				//Update the state string every 0.2s so we can read it.
				if (Event.current.type == EventType.Layout)
				{
					UpdateDisplayString();
				}
				if (displayState != null)
				{
					//Random number
					//windowRect = GUILayout.Window(1660952404, windowRect, DrawMethod, "Exception Detector", expandOptions);
				}
			}catch(Exception ex)
			{
				WriteLog(ex.ToString());
			}
		}

		private void DrawMethod(int windowID)
		{
			GUI.DragWindow();
			GUILayout.Label(displayState);
		}

		private static void logTheMessage(string condition, string stackTrace, LogType logType)
		{
			if (ExceptionDetectorUpdated.FullLog)
			{
				if (condition != "\n") WriteLog("Condition:\t" + condition);
				if (stackTrace != "\n") WriteLog("StackTrace:\t" + stackTrace + "\nLogType:\t" + logType);
			}
		}

		private static void logTheMessage(string condition, string stackTrace, String name)
		{
			WriteLog("Condition:\t" + condition);
			WriteLog("StackTrace:\t" + stackTrace);
			WriteLog("\nLogType:\t" + name);
		}

		public static void HandleLogEntry(string condition, string stackTrace, LogType logType)
		{

			//WriteLog("\n<<<<<<<< " + stackLogTick + "\t" + DoublePassValues.Count + "\t" + prvConditionStatement);
			string stkMsg = String.Empty;
			bool doublePass = CheckPass(condition, DoublePassValues);
			bool singlePass = false;
			if(!doublePass)
				singlePass = CheckPass(condition, SinglePassValues);

			if (doublePass)
			{
				// WriteLog(stackTrace);
				// save it
				preStack = condition;
				if (logType != LogType.Log)
					AddException(CleanCondition(condition));
				stackLogTick = 1;
				if (!HideKnowns)
				{
					//WriteLog("hidingknowns\n");
					logTheMessage(condition, stackTrace, logType);
				}
			}
			//else if (stackLogTick == 0 && !condition.Equals(prvConditionStatement))
			//{
			//	logTheMessage(condition, stackTrace, logType);
			//	WriteLog("\n");
			//}

			if (logType == LogType.Log && ShowInfoMessage)
			{
				logTheMessage(condition, stackTrace, logType);
			}
			else if (logType == LogType.Error || logType == LogType.Warning)
			{
				//WriteLog(logType.ToString() + " : " + stackLogTick + " : " + condition);
				if (stackLogTick == 1)
				{
					prvConditionStatement = condition;

					strMessage = "*EDU*\t" + preStack + "--> " + CleanCondition(condition) + "\n";
					WriteLog(strMessage);
					AddException(strMessage);
					stackLogTick = 0;
					preStack = String.Empty;
				}
				else if (singlePass)
				{
					WriteLog("*EDU*\t" + condition + "\n");
					AddException(CleanCondition(condition));
				}
				else if (stackLogTick == 0 && !condition.Equals(prvConditionStatement))
				{
					logTheMessage(condition, stackTrace, logType);
					WriteLog(stackTrace);
					WriteLog("\n");
					AddException(CleanCondition(condition));
				}

			}
			else if (logType == LogType.Exception)
			{
				try
				{
					stkMsg = stackTrace;
					if (!String.IsNullOrEmpty(condition) &&  ExceptionCount.ContainsKey(CleanCondition(condition)) && ExceptionCount[CleanCondition(condition)] > 20)
					{
						ulong ct = ExceptionCount[CleanCondition(condition)];
						stkMsg = "Exception has been called " + ++ct + " times";
						ExceptionCount[CleanCondition(condition)] = ct;
					}
					else
					{
						AddException(CleanCondition(condition));
					}
					logTheMessage(condition, stkMsg, "**EDU-Exception");
					WriteLog("EDU-EXCEPTION****\n\n\n");

					//ExceptionDetectorUpdated.Instance.OnGUI();
					using (StringReader sr = new StringReader(stackTrace))
					{
						StackInfo firstInfo = null;
						StackInfo stackInfo = null;
						bool foundMod = false;
						string currentLine = sr.ReadLine();
						while (!foundMod && currentLine != null)
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
							currentLine = sr.ReadLine();
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
				catch (Exception ex)
				{
					WriteLog(ex.ToString());
				}
			}

			//ExceptionDetectorUpdated.Instance.OnGUI();
			//WriteLog(">>>>>>>>\n");
		}

		private static void AddException(String strMessage)
		{

			if (!String.IsNullOrEmpty(strMessage))
			{
				if (ExceptionCount.ContainsKey(strMessage))
				{
					ulong count = ExceptionCount[strMessage];
					ExceptionCount[strMessage] = ++count;
				}
				else
				{
					ExceptionCount.Add(strMessage, 1);
				}
			}
		}

		private static bool CheckPass(string condition, Dictionary<string, string> passValues)
		{
			bool retVal = false;

			foreach (KeyValuePair<string, string> val in passValues)
			{
				if (condition.Contains(val.Value))
				{
					retVal = true;
					break;
				}
			}
			//if(condition.Contains(TEXTURELOADER) || condition.Contains(PARTLOADER) || condition.Contains(MODELLOADER) || condition.Contains(AUDIOLOADER);
			return retVal;
		}

		private static string CleanCondition(string condition)
		{
			string retVal = String.Empty;

			if (String.IsNullOrEmpty(condition))
			{
				retVal = "UNKNOWN ERROR";
			}
			else
			{
				string pathToGameData = Path.GetFullPath(Path.Combine(_assemblyPath, ".." + Path.DirectorySeparatorChar + ".. " + Path.DirectorySeparatorChar));
				retVal = condition.Replace(pathToGameData, String.Empty).Replace("GameData", String.Empty);
			}
			return retVal;
		}

		public static StackInfo GetStackInfo(string stackLine)
		{

			StackInfo unknownVal = new StackInfo { dllName = "Unknown", className = String.Empty };
			StackInfo retVal = new StackInfo();
			try
			{
				if (stackLine.StartsWith("UnityEngine.") || stackLine.StartsWith("KSPAssets."))
					return retVal;

				if (classCache.ContainsKey(stackLine))
				{
					return classCache[stackLine];
				}

				string processLine = null;
				try
				{
					processLine = stackLine.Substring(0, stackLine.LastIndexOf(" ("));
				}
				catch (ArgumentOutOfRangeException oor)
				{

				}

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


				if (unknownVal.className.Contains("`"))
				{
					unknownVal.className = unknownVal.className.Substring(0, unknownVal.className.IndexOf("`"));
				}

				unknownVal.methodName = methodName;
				classCache.Add(stackLine, unknownVal);
			}catch(Exception ex)
			{
				retVal = unknownVal;
			}
			return retVal;
		}

		public static void WriteLog(string strMessage)
		{
			if (!String.IsNullOrEmpty(strMessage))
			{
				FileStream objFilestream = new FileStream(ExceptionDetectorUpdated.LogFile, FileMode.Append, FileAccess.Write);
				StreamWriter objStreamWriter = new StreamWriter((Stream)objFilestream);
				objStreamWriter.AutoFlush = true;
				objStreamWriter.WriteLine(strMessage);
				objStreamWriter.Close();
				objFilestream.Close();
			}
		}

		private static void InitLog()
		{
			FileStream objFilestream = new FileStream(ExceptionDetectorUpdated.LogFile, FileMode.Create, FileAccess.Write);
			StreamWriter objStreamWriter = new StreamWriter((Stream)objFilestream);
			objStreamWriter.AutoFlush = true;
			objStreamWriter.WriteLine(DateTime.Now);
			objStreamWriter.WriteLine(Path.GetFullPath(Path.Combine(_assemblyPath, ".." + Path.DirectorySeparatorChar + ".. " + Path.DirectorySeparatorChar)));
			objStreamWriter.WriteLine("\n\n");
			objStreamWriter.Close();
			objFilestream.Close();
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