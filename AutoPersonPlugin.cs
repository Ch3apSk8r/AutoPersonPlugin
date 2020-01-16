using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace Ch3apSk8r
{
	public class AutoPersonPlugin : MVRScript
	{
		// Session plugin: Tries to find all people and add a list of
		// plugins to them

		// List of plugins. Perhaps this should come from a JSON file? Could add settings too then
		string[] personPlugins = new string[] {
			"Custom/Scripts/passenger.cs",
			"Custom/Scripts/ImprovedPov.cs",
		};
		string[] settings = new string[] {
			@"{""id"":""auto#0_Passenger"",""Rotation Lock"":""true"",""No Roll"":""false"",""Position Z"":""0.1"",""Position Y"":""0.05""}",
			@"{""id"":""auto#1_ImprovedPoV"",""Camera depth"":""0""}"
		};
		string[] blacklist = new string[] {
			"Passenger.cs",
			"ImprovedPoV.cs",
		};
		public override void Init()
		{
		}
		void Update()
		{
			foreach (Atom at in SuperController.singleton.GetAtoms().Where(a=>a.type=="Person"))
			{
				MVRPluginManager manager = at.GetStorableByID("PluginManager") as MVRPluginManager;
				JSONClass current=manager.GetJSON(true,true,true);

				if(current["plugins"]!=null && current["plugins"]["auto#0"]!=null) {
					// We've done this one.
				} else	if(current["plugins"]!=null && current["plugins"]["plugin#0"]!=null && current["plugins"]["plugin#0"].Value!="")
				{
					// Save current plugin settings
					List<JSONClass> storables=new List<JSONClass>();
					int j=0;
					while(current["plugins"]["plugin#"+j.ToString()]!=null) {
						// Perhaps see if we have a newer version of this plugin installed.
						//Log(current["plugins"]["plugin#"+j.ToString()].Value);
						// Or just check against a blacklist
						foreach (string bad in blacklist) {
							if(current["plugins"]["plugin#"+j.ToString()].Value.EndsWith(bad)) {
								current["plugins"]["plugin#"+j.ToString()].Value="";
							}
						} 
						string sid=at.GetStorableIDs().FirstOrDefault(id=>id.StartsWith("plugin#"+j.ToString()));
						JSONStorable jsons = at.GetStorableByID(sid);
						storables.Add(jsons.GetJSON(true,true,true));
						j++;
					}
					// perhaps determine gender?
					// Look at id="geometry" character="..."
					for(int i=0;i<personPlugins.Length;i++) {
						current["plugins"]["auto#"+i.ToString()]=personPlugins[i]; 
					}
					try
					{
						// this will re-init plugins!
						manager.LateRestoreFromJSON(current);
					}
					catch (Exception e)
					{
						SuperController.LogError("Failed to load plugin");
						SuperController.LogError(e.ToString());

					}
					try
					{
						// perhaps restore storables
						for(int i=0;i<j;i++) {
							string sid=at.GetStorableIDs().FirstOrDefault(id=>id.StartsWith("plugin#"+i.ToString()));
							JSONStorable s=at.GetStorableByID(sid);
							Log("Restoring storable: "+i.ToString()+"="+storables[i].ToString());
							s.RestoreFromJSON(storables[i]);
						}
						for(int i=0;i<personPlugins.Length;i++) {
							string sid=at.GetStorableIDs().FirstOrDefault(id=>id.StartsWith("auto#"+i.ToString()));
							Log("Attempting to restore setting: "+settings[i]);
							JSONStorable s=at.GetStorableByID(sid);
							JSONClass setting=JSON.Parse(settings[i]) as JSONClass;
							Log("Parsed as: "+setting.ToString());
							s.RestoreFromJSON(setting);
						}
					}
					catch (Exception e)
					{
						SuperController.LogError("Failed to restore storables");
						SuperController.LogError(e.ToString());
					}
				} 
				else
				{
					// load person plugins
					for(int i=0;i<personPlugins.Length;i++){
						current["plugins"]["auto#"+i.ToString()]=personPlugins[i];
					}
					try
					{
						manager.LateRestoreFromJSON(current);
					}
					catch (Exception e)
					{
						SuperController.LogError("Failed to load plugin");
						SuperController.LogError(e.ToString());
					}
				}
			}
		}
		void OnDestroy()
		{
		}
		void Log(string msg)
		{
			SuperController.LogMessage(msg);
		}
	}
}


