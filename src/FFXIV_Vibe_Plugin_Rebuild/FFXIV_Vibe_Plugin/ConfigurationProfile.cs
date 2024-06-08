using System;
using System.Collections.Generic;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.Triggers;

namespace FFXIV_Vibe_Plugin;

public class ConfigurationProfile
{
	public string Name = "Default";

	public bool VERBOSE_SPELL;

	public bool VERBOSE_CHAT;

	public List<Pattern> PatternList = new List<Pattern>();

	public string EXPORT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\FFXIV_Vibe_Plugin";

	public Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> VISITED_DEVICES = new Dictionary<string, FFXIV_Vibe_Plugin.Device.Device>();

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

}
