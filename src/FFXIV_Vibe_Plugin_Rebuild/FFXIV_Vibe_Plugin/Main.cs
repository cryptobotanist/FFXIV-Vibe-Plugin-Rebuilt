using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.Experimental;
using FFXIV_Vibe_Plugin.Hooks;
using FFXIV_Vibe_Plugin.Migrations;
using FFXIV_Vibe_Plugin.Triggers;

namespace FFXIV_Vibe_Plugin;

public class Main
{
	private bool ThreadMonitorPartyListRunning = true;

	private readonly Plugin Plugin;

	private readonly bool wasInit;

	public readonly string CommandName = "";

	private readonly string ShortName = "";

	private bool _firstUpdated;

	private readonly PlayerStats PlayerStats;

	private ConfigurationProfile ConfigurationProfile;

	private readonly Logger Logger;

	private readonly ActionEffect hook_ActionEffect;

	private readonly DevicesController DeviceController;

	private readonly TriggersController TriggersController;

	private readonly Patterns Patterns;

	private Premium Premium;

	private readonly NetworkCapture experiment_networkCapture;

	private IChatGui? DalamudChat { get; init; }

	public Configuration Configuration { get; init; }

	private IGameNetwork GameNetwork { get; init; }

	private IDataManager DataManager { get; init; }

	private IClientState ClientState { get; init; }

	private ISigScanner Scanner { get; init; }

	private IObjectTable GameObjects { get; init; }

	private DalamudPluginInterface PluginInterface { get; init; }

	private IPartyList PartyList { get; init; }

    public ITextureProvider TextureProvider { get; init; }

    private IGameInteropProvider InteropProvider { get; init; }

	public PluginUI PluginUi { get; init; }

	public Main(Plugin plugin, string commandName, string shortName, IGameNetwork gameNetwork, IClientState clientState, IDataManager dataManager, IChatGui? dalamudChat, Configuration configuration, ISigScanner scanner, IObjectTable gameObjects, DalamudPluginInterface pluginInterface, ITextureProvider textureProvider, IPartyList partyList, IGameInteropProvider interopProvider) : base()
	{
		PartyList = partyList;
		Main main = this;
		Plugin = plugin;
		CommandName = commandName;
		ShortName = shortName;
		GameNetwork = gameNetwork;
		ClientState = clientState;
		DataManager = dataManager;
		DalamudChat = dalamudChat;
		Configuration = configuration;
		GameObjects = gameObjects;
		Scanner = scanner;
		PluginInterface = pluginInterface;
		InteropProvider = interopProvider;
        TextureProvider = textureProvider;
		if (DalamudChat != null)
		{
			DalamudChat.ChatMessage += ChatWasTriggered;
		}
		Logger = new Logger(DalamudChat, ShortName, FFXIV_Vibe_Plugin.Commons.Logger.LogLevel.VERBOSE);
		if (DalamudChat == null)
		{
			Logger.Error("DalamudChat was not initialized correctly.");
		}
		new Migration(Configuration, Logger);
		ConfigurationProfile = Configuration.GetDefaultProfile();
		Patterns = new Patterns();
		Patterns.SetCustomPatterns(ConfigurationProfile.PatternList);
		DeviceController = new DevicesController(Logger, Configuration, ConfigurationProfile, Patterns);
		if (ConfigurationProfile.AUTO_CONNECT)
		{
			new Thread((ThreadStart)delegate
			{
				Thread.Sleep(2000);
				main.Command_DeviceController_Connect();
			}).Start();
		}
		hook_ActionEffect = new ActionEffect(DataManager, Logger, (SigScanner)Scanner, clientState, gameObjects, interopProvider);
		hook_ActionEffect.ReceivedEvent += SpellWasTriggered;
		ClientState.Login += ClientState_LoginEvent;
		PlayerStats = new PlayerStats(Logger, ClientState);
		PlayerStats.Event_CurrentHpChanged += PlayerCurrentHPChanged;
		PlayerStats.Event_MaxHpChanged += PlayerCurrentHPChanged;
		TriggersController = new TriggersController(Logger, PlayerStats, ConfigurationProfile);
		Premium = new Premium(Logger, ConfigurationProfile);
		PluginUi = new PluginUI(this, Logger, PluginInterface, Configuration, ConfigurationProfile, DeviceController, TriggersController, Patterns, Premium);
		experiment_networkCapture = new NetworkCapture(Logger, GameNetwork);
		PartyList = partyList;
		new Thread((ThreadStart)delegate
		{
			main.MonitorPartyList(partyList);
		}).Start();
		SetProfile(Configuration.CurrentProfileName);
		wasInit = true;
	}

	public void Dispose()
	{
		Logger.Debug("Disposing plugin...");
		if (!wasInit)
		{
			return;
		}
		if (DeviceController != null)
		{
			try
			{
				DeviceController.Dispose();
			}
			catch (Exception e)
			{
				Logger.Error("App.Dispose: " + e.Message);
			}
		}
		if (DalamudChat != null)
		{
			DalamudChat.ChatMessage -= ChatWasTriggered;
		}
		hook_ActionEffect.Dispose();
		PluginUi.Dispose();
		experiment_networkCapture.Dispose();
		Premium.Dispose();
		Logger.Debug("Plugin disposed!");
		ThreadMonitorPartyListRunning = false;
	}

	public static string GetHelp(string command)
	{
		return $"Usage:\n      {command} config      \n      {command} connect\n      {command} disconnect\n      {command} send <0-100> # Send vibe intensity to all toys\n      {command} stop\n";
	}

	public void OnCommand(string command, string args)
	{
		if (args.Length == 0)
		{
			DisplayUI();
		}
		else if (args.StartsWith("help"))
		{
			Logger.Chat(GetHelp("/" + ShortName));
		}
		else if (args.StartsWith("config"))
		{
			DisplayConfigUI();
		}
		else if (args.StartsWith("connect"))
		{
			Command_DeviceController_Connect();
		}
		else if (args.StartsWith("disconnect"))
		{
			Command_DeviceController_Disconnect();
		}
		else if (args.StartsWith("send"))
		{
			Command_SendIntensity(args);
		}
		else if (args.StartsWith("stop"))
		{
			DeviceController.SendVibeToAll(0);
		}
		else if (args.StartsWith("profile"))
		{
			Command_ProfileSet(args);
		}
		else if (args.StartsWith("exp_network_start"))
		{
			experiment_networkCapture.StartNetworkCapture();
		}
		else if (args.StartsWith("exp_network_stop"))
		{
			experiment_networkCapture.StopNetworkCapture();
		}
		else
		{
			Logger.Chat("Unknown subcommand: " + args);
		}
	}

	private void FirstUpdated()
	{
		Logger.Debug("First updated");
		if (ConfigurationProfile != null && ConfigurationProfile.AUTO_OPEN)
		{
			DisplayUI();
		}
	}

	private void DisplayUI()
	{
		Plugin.DrawConfigUI();
	}

	private void DisplayConfigUI()
	{
		Plugin.DrawConfigUI();
	}

	public void DrawUI()
	{
		if (PluginUi != null)
		{
			if (ClientState != null && ClientState.IsLoggedIn)
			{
				PlayerStats.Update(ClientState);
			}
			if (!_firstUpdated)
			{
				FirstUpdated();
				_firstUpdated = true;
			}
		}
	}

	public void Command_DeviceController_Connect()
	{
		if (DeviceController == null)
		{
			Logger.Error("No device controller available to connect.");
		}
		else if (ConfigurationProfile != null)
		{
			string host = ConfigurationProfile.BUTTPLUG_SERVER_HOST;
			int port = ConfigurationProfile.BUTTPLUG_SERVER_PORT;
			DeviceController.Connect(host, port);
		}
	}

	private void Command_DeviceController_Disconnect()
	{
		if (DeviceController == null)
		{
			Logger.Error("No device controller available to disconnect.");
			return;
		}
		try
		{
			DeviceController.Disconnect();
		}
		catch (Exception e)
		{
			Logger.Error("App.Command_DeviceController_Disconnect: " + e.Message);
		}
	}

	private void Command_SendIntensity(string args)
	{
		int intensity;
		try
		{
			intensity = int.Parse(args.Split(" ", 2)[1]);
			Logger.Chat($"Command Send intensity {intensity}");
		}
		catch (Exception ex) when (((ex is FormatException || ex is IndexOutOfRangeException) ? 1 : 0) != 0)
		{
			Logger.Error("Malformed arguments for send [intensity].", ex);
			return;
		}
		if (DeviceController == null)
		{
			Logger.Error("No device controller available to send intensity.");
		}
		else
		{
			DeviceController.SendVibeToAll(intensity);
		}
	}

	private void SpellWasTriggered(object? sender, HookActionEffects_ReceivedEventArgs args)
	{
		if (TriggersController == null)
		{
			Logger.Warn("SpellWasTriggered: TriggersController not init yet, ignoring spell...");
			return;
		}
		Structures.Spell spell = args.Spell;
		if (ConfigurationProfile != null && ConfigurationProfile.VERBOSE_SPELL)
		{
			Logger.Debug($"VERBOSE_SPELL: {spell}");
		}
		foreach (Trigger trigger in TriggersController.CheckTrigger_Spell(spell))
		{
			DeviceController.SendTrigger(trigger);
		}
	}

	private void ChatWasTriggered(XivChatType chatType, uint senderId, ref SeString _sender, ref SeString _message, ref bool isHandled)
	{
		string fromPlayerName = _sender.ToString();
		if (TriggersController == null)
		{
			Logger.Warn("ChatWasTriggered: TriggersController not init yet, ignoring chat...");
			return;
		}
		if (ConfigurationProfile != null && ConfigurationProfile.VERBOSE_CHAT)
		{
			XivChatType xivChatType = chatType;
			string XivChatTypeName = xivChatType.ToString();
			Logger.Debug($"VERBOSE_CHAT: {fromPlayerName} type={XivChatTypeName}: {_message}");
		}
		foreach (Trigger trigger in TriggersController.CheckTrigger_Chat(chatType, fromPlayerName, _message.TextValue))
		{
			DeviceController.SendTrigger(trigger);
		}
	}

	private void PlayerCurrentHPChanged(object? send, EventArgs e)
	{
		float currentHP = PlayerStats.GetCurrentHP();
		float maxHP = PlayerStats.GetMaxHP();
		if (TriggersController == null)
		{
			Logger.Warn("PlayerCurrentHPChanged: TriggersController not init yet, ignoring HP change...");
			return;
		}
		float percentageHP = currentHP * 100f / maxHP;
		List<Trigger> list = TriggersController.CheckTrigger_HPChanged((int)currentHP, percentageHP);
		Logger.Verbose($"PlayerCurrentHPChanged SelfPlayer {currentHP}/{maxHP} {percentageHP:0.##}%");
		foreach (Trigger trigger in list)
		{
			DeviceController.SendTrigger(trigger);
		}
	}

	private void ClientState_LoginEvent()
	{
		PlayerStats.Update(ClientState);
	}

	private void MonitorPartyList(IPartyList partyList)
	{
		while (ThreadMonitorPartyListRunning)
		{
			if (TriggersController == null)
			{
				Logger.Warn("HPChangedOtherPlayer: TriggersController not init yet, ignoring HP change other...");
				break;
			}
			if (partyList.Length >= 0)
			{
				foreach (Trigger trigger in TriggersController.CheckTrigger_HPChangedOther(partyList))
				{
					Logger.Verbose($"HPChangedOtherPlayer {trigger.FromPlayerName} min:{trigger.AmountMinValue} max:{trigger.AmountMaxValue} triggered!");
					DeviceController.SendTrigger(trigger);
				}
			}
			Thread.Sleep(500);
		}
	}

	public bool SetProfile(string profileName)
	{
		if (!Configuration.SetCurrentProfile(profileName))
		{
			Logger.Warn("You are trying to use profile " + profileName + " which can't be found");
			return false;
		}
		ConfigurationProfile configProfileToCheck = Configuration.GetProfile(profileName);
		if (configProfileToCheck != null)
		{
			ConfigurationProfile = configProfileToCheck;
			PluginUi.SetProfile(ConfigurationProfile);
			DeviceController.SetProfile(ConfigurationProfile);
			TriggersController.SetProfile(ConfigurationProfile);
		}
		return true;
	}

	private void Command_ProfileSet(string args)
	{
		List<string> a = args.Split(" ").ToList();
		if (a.Count == 2)
		{
			if (Premium.IsPremium())
			{
				string profileName = a[1];
				SetProfile(profileName);
			}
			else
			{
				Logger.Warn("Premium feature Only: /fvp profile [name]");
			}
		}
		else
		{
			Logger.Error("Wrong command: /fvp profile [name]");
		}
	}
}
