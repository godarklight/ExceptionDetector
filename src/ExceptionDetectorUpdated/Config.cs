using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExceptionDetectorUpdated
{
	public class Config
	{
		private static string _showFullLog = "ShowFullLog";
		private static string _hideKnowns = "HideKnowns";
		private static string _showInfoMessage = "ShowInfoMessages";
		private static string _singlePass = "SinglePass";
		private static string _doublePass = "DoublePass";


		public static void Load()
		{
			try
			{
				if (File.Exists(ExceptionDetectorUpdated.SettingsFile))
				{
					var node = ConfigNode.Load(ExceptionDetectorUpdated.SettingsFile);
					if (node == null) return;

					var root = node.GetNode("ExceptionDetectorUpdated");
					if (root == null) return;

					var settings = root.GetNode("Config");
					if (settings == null) return;

					var singleNode = settings.GetNode(_singlePass);
					if (singleNode != null)
						ConvertToDictionary(singleNode, ExceptionDetectorUpdated.SinglePassValues);
					var doubleNode = settings.GetNode(_doublePass);
					if(doubleNode != null)
						ConvertToDictionary(doubleNode, ExceptionDetectorUpdated.DoublePassValues);

					var set = settings.GetValue(_showFullLog);
					if (bool.TryParse(set, out var settf)) ExceptionDetectorUpdated.FullLog = settf;

					var knowns = settings.GetValue(_hideKnowns);
					if (bool.TryParse(knowns, out var knowntf)) ExceptionDetectorUpdated.HideKnowns = knowntf;

					var shInfo = settings.GetValue(_showInfoMessage);
					if (bool.TryParse(shInfo, out var llmtf)) ExceptionDetectorUpdated.ShowInfoMessage = llmtf;
				}
				Save();
			}
			catch (Exception ex)
			{
				ExceptionDetectorUpdated.WriteLog(ex.ToString());
			}
		}

		private static void ConvertToDictionary(ConfigNode node, Dictionary<string, string> passValues)
		{
			for (int x = 0; x < node.CountValues; x++)
			{
				passValues.Add(node.values[x].name, node.values[x].value);
			}
		}

		private static ConfigNode ConvertFromDictionary(String name, Dictionary<string, string> passValues)
		{
			ConfigNode node = new ConfigNode(name);

			foreach (KeyValuePair<string,string> val in passValues)
			{
				node.AddValue(val.Key, val.Value);
			}
			return node;
		}

		public static void Save()
		{
			try
			{
				var node = new ConfigNode();
				var root = node.AddNode("ExceptionDetectorUpdated");
				var settings = root.AddNode("Config");
				settings.AddValue(_showFullLog, ExceptionDetectorUpdated.FullLog);
				settings.AddValue(_hideKnowns, ExceptionDetectorUpdated.HideKnowns);
				settings.AddValue(_showInfoMessage, ExceptionDetectorUpdated.ShowInfoMessage);
				var sNode = settings.AddNode(ConvertFromDictionary(_singlePass, ExceptionDetectorUpdated.SinglePassValues));
				var dNode = settings.AddNode(ConvertFromDictionary(_doublePass, ExceptionDetectorUpdated.DoublePassValues));
				node.Save(ExceptionDetectorUpdated.SettingsFile);
			}
			catch (Exception ex)
			{
				ExceptionDetectorUpdated.WriteLog(ex.ToString());
			}
		}
	}		
}
