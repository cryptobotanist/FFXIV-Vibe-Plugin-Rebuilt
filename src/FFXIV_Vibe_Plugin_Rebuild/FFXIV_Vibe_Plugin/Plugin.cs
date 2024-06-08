using System;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace FFXIV_Vibe_Plugin;

public sealed class Plugin : IDalamudPlugin, IDisposable
{
	public static readonly string ShortName = "FVPR";

	public readonly string CommandName = "/fvpr";

	public WindowSystem WindowSystem = new WindowSystem("FFXIV Vibe Plugin Rebuild");

	private Main app;

	public string Name => "FFXIV Vibe Plugin Rebuild";

	private IChatGui? DalamudChat { get; init; }

	private DalamudPluginInterface PluginInterface { get; init; }

	private ICommandManager CommandManager { get; init; }

	public Configuration Configuration { get; init; }

	public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface, [RequiredVersion("1.0")] ICommandManager commandManager, [RequiredVersion("1.0")] ITextureProvider textureProvider, [RequiredVersion("1.0")] IClientState clientState, [RequiredVersion("1.0")] IPartyList partyList, [RequiredVersion("1.0")] IGameNetwork gameNetwork, [RequiredVersion("1.0")] ISigScanner scanner, [RequiredVersion("1.0")] IObjectTable gameObjects, [RequiredVersion("1.0")] IDataManager dataManager, [RequiredVersion("1.0")] IChatGui? dalamudChat, [RequiredVersion("1.0")] IGameInteropProvider? interopProvider)
	{
		PluginInterface = pluginInterface;
		CommandManager = commandManager;
		Configuration = (PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();
		Configuration.Initialize(PluginInterface);
		CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
		{
			HelpMessage = "A vibe plugin for fun... (Rebuilt)"
		});
		PluginInterface.UiBuilder.Draw += DrawUI;
		PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        app = new Main(this, CommandName, ShortName, gameNetwork, clientState, dataManager, dalamudChat, Configuration, scanner, gameObjects, pluginInterface, textureProvider, partyList, interopProvider);
		WindowSystem.AddWindow(app.PluginUi);
	}

	public void Dispose()
	{
		WindowSystem.RemoveAllWindows();
		CommandManager.RemoveHandler(CommandName);
		app.Dispose();
	}

	private void OnCommand(string command, string args)
	{
		app.OnCommand(command, args);
	}

	private void DrawUI()
	{
		WindowSystem.Draw();
		if (app != null)
		{
			app.DrawUI();
		}
	}

	public void DrawConfigUI()
	{
		WindowSystem.Windows[0].IsOpen = true;
	}
}
