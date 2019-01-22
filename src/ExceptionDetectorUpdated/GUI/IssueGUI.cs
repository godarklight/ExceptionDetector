// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

#region Using Directives

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.IO;

#endregion

namespace ExceptionDetectorUpdated
{
	public class IssueGUI : MonoBehaviour
	{
		#region Fields

		private GUIStyle buttonStyle;
		private bool hasPositioned;
		private List<string> message;
		private int msgCount = 20;
		private Rect position = new Rect(Screen.width *.8f, Screen.height *.3f, 0, 0);
		private string title;
		private GUIStyle titleStyle;
		private GUIStyle listStyle;
		private bool initDone = false;
		private float lastFrameTime = 0.0f;

		#endregion

		#region Properties

		public bool HasBeenUpdated { get; set; }

		#endregion

		#region Methods: protected

		protected void Awake()
		{
			try
			{
				DontDestroyOnLoad(this);
				message = new List<string>();
				for(int x = 1; x <= msgCount; x++)
				{
					message.Add(String.Format("{0}: ", x));
				}
			}
			catch (Exception ex)
			{
				//Logger.Exception(ex);
			}
			//Logger.Log("FirstRunGui was created.");
		}

		protected void OnDestroy()
		{
			//Logger.Log("FirstRunGui was destroyed.");
		}

		public void OnGUI()
		{
			try
			{
				if (!initDone)
				{
					InitialiseStyles();
				}
					this.position = GUILayout.Window(this.GetInstanceID(), this.position, this.Window, this.title, HighLogic.Skin.window);
					this.PositionWindow();
				
				//UpdateMessages();
			}
			catch (Exception ex)
			{
				//Logger.Exception(ex);
			}
		}

		private void Update()
		{
			//if (lastFrameTime < Time.time + (1 / 24f)) // 24fps
			if (lastFrameTime < Time.time + 1)
			{
				lastFrameTime = Time.time;
				UpdateMessages();
			}
		}
		private void UpdateMessages()
		{
			if (ExceptionDetectorUpdated.ExceptionCount.Count() > 2)
			{
				var list = ExceptionDetectorUpdated.ExceptionCount.ToList();
				list.Sort((x, y) => y.Value.CompareTo(x.Value));
				for (int x = 0; x < msgCount; x++)
				{
					message[x] = x >= list.Count() ? String.Format("{0}", x + 1) : String.Format("{0}:  {1} times : {2}", x + 1, list[x].Value, list[x].Key);
				}
			}
		}

		protected void Start()
		{
			try
			{
				this.title = "ExceptionDetectorUpdated - EDU";
				// this.message = (this.HasBeenUpdated ? "You have successfully updated KSP-AVC to v" : "You have successfully installed KSP-AVC v") + this.version;
			}
			catch (Exception ex)
			{
				//Logger.Exception(ex);
			}
		}

		#endregion

		#region Methods: private

		private void PositionWindow()
		{
			if (this.hasPositioned || !(this.position.width > 0) || !(this.position.height > 0))
			{
				return;
			}
			this.position.center = new Vector2(Screen.width * 0.75f, Screen.height *.25f);
			this.hasPositioned = true;
		}

		private void InitialiseStyles()
		{
			initDone = true;
			this.titleStyle = new GUIStyle(HighLogic.Skin.label)
			{
				normal =
					 {
						  textColor = Color.white
					 },
				fontSize = 13,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				stretchWidth = true
			};
			listStyle = new GUIStyle(HighLogic.Skin.label)
			{
				normal =
					 {
						  textColor = Color.white
					 },
				fontSize = 13,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft,
				stretchWidth = true
			};

			this.buttonStyle = new GUIStyle(HighLogic.Skin.button)
			{
				normal =
					 {
						  textColor = Color.white
					 }
			};
		}

		private void Window(int id)
		{
			try
			{
				GUILayout.MaxHeight(Screen.height * .65f);
				GUILayout.BeginVertical(HighLogic.Skin.box);
				GUILayout.Label(String.Format("TOP {0} ISSUES", msgCount), this.titleStyle, GUILayout.Width(Screen.width * 0.2f));
				GUILayout.Label(String.Format("More info availabe at {0}", Path.GetFullPath(ExceptionDetectorUpdated.LogFile)), this.titleStyle, GUILayout.Width(Screen.width * 0.2f)); 
				for (int x = 0; x < msgCount; x++)
				{
					GUILayout.Label(message[x], this.listStyle, GUILayout.Width(Screen.width * 0.2f));
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				//GUILayout.
				if (GUILayout.Button("CLOSE", this.buttonStyle))
				{
					Destroy(this);
				}
				
				GUI.DragWindow();
			}
			catch (Exception ex)
			{
				//Logger.Exception(ex);
			}
		}

		#endregion
	}
}