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

using UnityEngine;

#endregion

namespace ExceptionDetectorUpdated
{
	public class IssueGUI : MonoBehaviour
	{
		#region Fields

		private GUIStyle buttonStyle;
		private bool hasPositioned;
		private List<string> message;
		private int msgCount = 5;
		private Rect position = new Rect(Screen.width, Screen.height, 0, 0);
		private string title;
		private GUIStyle titleStyle;
		private GUIStyle listStyle;

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
				message.Add("TOP 5 ISSUES");
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

		protected void OnGUI()
		{
			try
			{
				this.position = GUILayout.Window(this.GetInstanceID(), this.position, this.Window, this.title, HighLogic.Skin.window);
				this.PositionWindow();
			}
			catch (Exception ex)
			{
				//Logger.Exception(ex);
			}
		}

		protected void Start()
		{
			try
			{
				this.InitialiseStyles();
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
			this.position.center = new Vector2(Screen.width * 0.75f, Screen.height * 0.5f);
			this.hasPositioned = true;
		}

		private void InitialiseStyles()
		{
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
				GUILayout.BeginVertical(HighLogic.Skin.box);
				GUILayout.Label("TOP 5 ISSUES", this.titleStyle, GUILayout.Width(Screen.width * 0.2f));
				for (int x = 1; x <= msgCount; x++)
				{
					GUILayout.Label(message[x], this.listStyle, GUILayout.Width(Screen.width * 0.2f));
				}
				GUILayout.EndVertical();
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