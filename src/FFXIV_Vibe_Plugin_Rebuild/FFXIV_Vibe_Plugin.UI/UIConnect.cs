using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Device;
using ImGuiNET;

namespace FFXIV_Vibe_Plugin.UI;

internal class UIConnect
{
	public static void Draw(Configuration configuration, ConfigurationProfile configurationProfile, Main plugin, DevicesController devicesController, Premium premium)
	{
		ImGui.Spacing();
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Server address & port");
		ImGui.BeginChild("###Server", new Vector2(-1f, 40f), border: true);
		string input = configurationProfile.BUTTPLUG_SERVER_HOST;
		ImGui.SetNextItemWidth(200f);
		if (ImGui.InputText("##serverHost", ref input, 99u))
		{
			configurationProfile.BUTTPLUG_SERVER_HOST = input.Trim().ToLower();
			configuration.Save();
		}
		ImGui.SameLine();
		ImGuiComponents.HelpMarker("Go in the option tab if you need WSS (default: 127.0.0.1)");
		ImGui.SameLine();
		int v = configurationProfile.BUTTPLUG_SERVER_PORT;
		ImGui.SetNextItemWidth(100f);
		if (ImGui.InputInt("##serverPort", ref v, 10))
		{
			configurationProfile.BUTTPLUG_SERVER_PORT = v;
			configuration.Save();
		}
		ImGui.SameLine();
		ImGuiComponents.HelpMarker("Use '-1' as port number to not define it (default: 12345)");
		ImGui.EndChild();
		ImGui.Spacing();
		ImGui.BeginChild("###Main_Connection", new Vector2(-1f, 40f), border: true);
		if (!devicesController.IsConnected())
		{
			if (ImGui.Button("Connect", new Vector2(100f, 24f)))
			{
				plugin.Command_DeviceController_Connect();
			}
		}
		else if (ImGui.Button("Disconnect", new Vector2(100f, 24f)))
		{
			devicesController.Disconnect();
		}
		ImGui.SameLine();
		bool v2 = configurationProfile.AUTO_CONNECT;
		if (ImGui.Checkbox("Automatically connects. ", ref v2))
		{
			configurationProfile.AUTO_CONNECT = v2;
			configuration.Save();
		}
		ImGui.EndChild();
		ImGui.Spacing();
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Premium settings");
		ImGui.BeginChild("###Premium", new Vector2(-1f, 60f), border: true);
		ImGui.Text("Premium token");
		ImGui.SameLine();
		string input2 = configurationProfile.PREMIUM_TOKEN;
		ImGui.SetNextItemWidth(200f);
		if (ImGui.InputText("##premium_token", ref input2, 200u) && input2 != configurationProfile.PREMIUM_TOKEN_SECRET)
		{
			configurationProfile.PREMIUM_TOKEN = ((input2 == "") ? "" : "********");
			configurationProfile.PREMIUM_TOKEN_SECRET = input2;
			configuration.Save();
			premium.updateStatus();
		}
		ImGui.SameLine();
		if (!premium.invalidToken)
		{
			ImGui.TextColored(ImGuiColors.HealerGreen, "VALID TOKEN");
		}
		else if (input2 == "")
		{
			ImGui.TextColored(ImGuiColors.DalamudGrey2, "Put anything in here");
		}
		else
		{
			ImGui.TextColored(ImGuiColors.DPSRed, "Invalid token. " + premium.serverMsg);
		}
		ImGui.SameLine();
		ImGuiComponents.HelpMarker("Put whatever in here, it just works");
		ImGui.TextColored(ImGuiColors.ParsedPink, "In memoriam kaciexx. So long. Thanks for the vibes.");
		ImGui.EndChild();
	}
}
