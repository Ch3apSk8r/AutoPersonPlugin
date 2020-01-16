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

		private JSONClass _settings;
		public override void Init()
		{
			// in the future save this in the scene
			_settings=JSON.Parse(SuperController.singleton.ReadFileIntoString("Custom/Scripts/Ch3apSk8r/AutoPersonPlugin/settings.json")) as JSONClass;
		}
		private JSONClass _getPluginSettingsByName(Atom at, string name)
		{
			string sid=at.GetStorableIDs().FirstOrDefault(id=>id.StartsWith(name));
			JSONClass settings=at.GetStorableByID(sid).GetJSON();
			//Log(settings.ToString());
			return settings;
		}
		void Update()
		{
			foreach (Atom person in SuperController.singleton.GetAtoms().Where(a=>a.type=="Person"))
			{
				MVRPluginManager manager = person.GetStorableByID("PluginManager") as MVRPluginManager;
				JSONClass currentPlugins=manager.GetJSON(true,true,true);

				if(currentPlugins["plugins"]!=null && currentPlugins["plugins"]["auto#0"]!=null) {
					// We've done this one.
				} else {
					// New
					JSONClass newPlugins=new JSONClass();
					newPlugins["id"]="PluginManager";
					Dictionary<string,JSONClass> pluginSettings = new Dictionary<string,JSONClass>();
					int pluginCount=0;

					// infer gender
					JSONClass geom=person.GetStorableByID("geometry").GetJSON();
					string gender="both";
					if(geom["character"].Value.StartsWith("Male")) {
						gender="male";
					} else {
						gender="female";
					}
					List<string> seen = new List<string>();
					
					// Look at existing plugins, save settings where appropriate
					foreach(KeyValuePair<string, JSONNode> kvp in (JSONClass)currentPlugins["plugins"]) {
						string shortname=kvp.Value;
						seen.Add(shortname);
						if(_settings[shortname]!=null && _settings[shortname]["gender"]=="both" || _settings[shortname]["gender"]==gender ) {
							if(_settings[shortname]["path"]!=null) {
								// we have a replacement script
								newPlugins["plugins"]["auto#"+pluginCount.ToString()]=_settings[shortname]["path"];
							} else {
								// take current path
								newPlugins["plugins"]["auto#"+pluginCount.ToString()]=kvp.Value;
							}
							// We know this one do something
							if(_settings[shortname]["settings"]!=null) {
								Log("Replacing: "+_getPluginSettingsByName(person,kvp.Key).ToString());
								Log("     With: "+_settings[shortname]["settings"].ToString());
								// we have replacement settings
								pluginSettings["auto#"+pluginCount.ToString()]=_settings[shortname]["settings"] as JSONClass;
							} else {
								// copy current settings
								pluginSettings["auto#"+pluginCount.ToString()]=_getPluginSettingsByName(person,kvp.Key);
							}
						} else {
							// We don't know this one, copy it and its settings
							newPlugins["plugins"]["auto#"+pluginCount.ToString()]=kvp.Value;
							pluginSettings["auto#"+pluginCount.ToString()]=_getPluginSettingsByName(person,kvp.Key);
						}
						pluginCount++;
					}
					// Add any we've not done yet
					foreach(KeyValuePair<string, JSONNode> kvp in (JSONClass)_settings) {
						if(seen.Contains(kvp.Key) || kvp.Value["path"]==null) {
							Log("Seen/blank");
						} else {
							Log("Not Seen");
							newPlugins["plugins"]["auto#"+pluginCount.ToString()]=kvp.Value["path"];
							if(kvp.Value["settings"]!=null) {
								Log("Trying to stash settings: "+kvp.Value["settings"]);
								// TODO is this getting set?
								pluginSettings["auto#"+pluginCount.ToString()]=_settings[kvp.Key]["settings"] as JSONClass;

							} else {
								pluginSettings["auto#"+pluginCount.ToString()]=null;
							}
							pluginCount++;
						}
					}
					Log("Old: "+currentPlugins.ToString());
					Log("New: "+newPlugins.ToString());
					try
					{
						// this will re-init plugins!
						manager.LateRestoreFromJSON(newPlugins);
					}
					catch (Exception e)
					{
						SuperController.LogError("Failed to load plugin");
						SuperController.LogError(e.ToString());
						DestroyImmediate(this); 
					}
					Log("About to restore storables");
					try
					{
						// restore storables
						foreach(KeyValuePair<string, JSONClass> kvp in pluginSettings) {
							Log("plugin to restore: "+kvp.Key);
							if(kvp.Value!=null) {
								Log("settings: "+kvp.Value.ToString());
								string sid=person.GetStorableIDs().FirstOrDefault(id=>id.StartsWith(kvp.Key));
								JSONStorable s=person.GetStorableByID(sid);
								s.RestoreFromJSON(kvp.Value);
							}
						}
					}
					catch (Exception e)
					{
						SuperController.LogError("Failed to restore storables");
						SuperController.LogError(e.ToString());
						DestroyImmediate(this); 
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


