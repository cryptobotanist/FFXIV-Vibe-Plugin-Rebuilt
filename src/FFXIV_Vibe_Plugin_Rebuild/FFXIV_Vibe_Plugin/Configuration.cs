using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.Triggers;

namespace FFXIV_Vibe_Plugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
	public string CurrentProfileName = "Default";

	public List<ConfigurationProfile> Profiles = new List<ConfigurationProfile>();

	public bool VERBOSE_SPELL;

	public bool VERBOSE_CHAT;

	public List<Pattern> PatternList = new List<Pattern>();

	public string EXPORT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\FFXIV_Vibe_Plugin";

	public Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> VISITED_DEVICES = new Dictionary<string, FFXIV_Vibe_Plugin.Device.Device>();

	[NonSerialized]
	private DalamudPluginInterface? pluginInterface;

	public int Version { get; set; }

	public bool VIBE_HP_TOGGLE { get; set; }

	public int VIBE_HP_MODE { get; set; }

	public int MAX_VIBE_THRESHOLD { get; set; } = 100;


	public bool AUTO_CONNECT { get; set; } = true;


	public bool AUTO_OPEN { get; set; }

	public string BUTTPLUG_SERVER_HOST { get; set; } = "127.0.0.1";


	public int BUTTPLUG_SERVER_PORT { get; set; } = 12345;


	public bool BUTTPLUG_SERVER_SHOULD_WSS { get; set; }

	public List<Trigger> TRIGGERS { get; set; } = new List<Trigger>();


	public string PREMIUM_TOKEN { get; set; } = "";


	public string PREMIUM_TOKEN_SECRET { get; set; } = "";


	public void Initialize(DalamudPluginInterface pluginInterface)
	{
		this.pluginInterface = pluginInterface;
		try
		{
			Directory.CreateDirectory(EXPORT_DIR);
		}
		catch
		{
		}
	}

	public void Save()
	{
		pluginInterface.SavePluginConfig(this);
	}

	public ConfigurationProfile? GetProfile(string name = "")
	{
		if (name == "")
		{
			name = CurrentProfileName;
		}
		return Profiles.Find((ConfigurationProfile i) => i.Name == name);
	}

	public ConfigurationProfile GetDefaultProfile()
	{
		string defaultProfileName = "Default profile";
		ConfigurationProfile profileToCheck = GetProfile(CurrentProfileName);
		if (profileToCheck == null)
		{
			profileToCheck = GetProfile(defaultProfileName);
		}
		ConfigurationProfile profileToReturn = profileToCheck ?? new ConfigurationProfile();
		if (profileToCheck == null)
		{
			profileToReturn.Name = defaultProfileName;
			Profiles.Add(profileToReturn);
			CurrentProfileName = defaultProfileName;
			Save();
		}
		return profileToReturn;
	}

	public ConfigurationProfile? GetFirstProfile()
	{
		ConfigurationProfile profile = null;
		if (profile == null && Profiles.Count > 0)
		{
			profile = Profiles[0];
		}
		return profile;
	}

	public void RemoveProfile(string name)
	{
		ConfigurationProfile profile = GetProfile(name);
		if (profile != null)
		{
			Profiles.Remove(profile);
		}
	}

	public bool AddProfile(string name)
	{
		ConfigurationProfile profile = GetProfile(name);
		if (profile == null)
		{
			profile = new ConfigurationProfile();
			profile.Name = name;
			Profiles.Add(profile);
			return true;
		}
		return false;
	}

	public bool SetCurrentProfile(string name)
	{
		ConfigurationProfile profile = GetProfile(name);
		if (profile != null)
		{
			CurrentProfileName = profile.Name;
			return true;
		}
		return false;
	}
}
