using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.UI.Components;
using ImGuiNET;

namespace FFXIV_Vibe_Plugin.UI;

internal class UIBanner
{
	public static void Draw(int frameCounter, Logger logger, string donationLink, IDalamudTextureWrap image, string KofiLink, DevicesController devicesController, Premium premium)
	{
		ImGui.Columns(2, "###main_header", border: false);
		float num = 0.2f;
		ImGui.SetColumnWidth(0, (int)((float)image.Width * num + 20f));
		ImGui.Image(image.ImGuiHandle, new Vector2((float)image.Width * num, (float)image.Height * num));
		ImGui.NextColumn();
		if (devicesController.IsConnected())
		{
			int count = devicesController.GetDevices().Count;
			ImGui.TextColored(ImGuiColors.ParsedGreen, "You are connnected!");
			ImGui.SameLine();
			ImGui.Text($"/ Number of device(s): {count}");
		}
		else
		{
			ImGui.TextColored(ImGuiColors.ParsedGrey, "Your are not connected!");
		}
		if (premium.IsPremium())
		{
			ImGui.Text("Premium subscription: " + premium.GetPremiumLevel());
		}
		ImGui.Text(donationLink ?? "");
		ImGui.SameLine();
		ButtonLink.Draw("Donations", donationLink, FontAwesomeIcon.Pray, logger);
		ImGui.SameLine();
		ImGui.Text(KofiLink ?? "");
		ImGui.SameLine();
		ButtonLink.Draw("Ko-Fi", KofiLink, FontAwesomeIcon.Coffee, logger);
	}
}
