using System;
using System.Diagnostics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using FFXIV_Vibe_Plugin.Commons;
using ImGuiNET;

namespace FFXIV_Vibe_Plugin.UI.Components;

internal class ButtonLink
{
	public static void Draw(string text, string link, FontAwesomeIcon Icon, Logger Logger)
	{
		if (ImGuiComponents.IconButton(Icon))
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = link,
					UseShellExecute = true
				});
			}
			catch (Exception e)
			{
				Logger.Error("Could not open repoUrl: " + link, e);
			}
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(text);
		}
	}
}
