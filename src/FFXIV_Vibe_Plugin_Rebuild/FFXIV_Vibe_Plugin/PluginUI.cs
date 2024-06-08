using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.Triggers;
using FFXIV_Vibe_Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace FFXIV_Vibe_Plugin;

public class PluginUI : Window, IDisposable
{
	private int frameCounter;

	private readonly DalamudPluginInterface PluginInterface;

	private readonly Configuration Configuration;

	private ConfigurationProfile ConfigurationProfile;

	private readonly DevicesController DevicesController;

	private readonly TriggersController TriggerController;

	private readonly Main app;

	private readonly Logger Logger;

	private IDalamudTextureWrap logoImage;

	private readonly Patterns Patterns = new Patterns();

	private readonly string DonationLink = "https://paypal.me/kaciedev";

	private readonly string KofiLink = "https://ko-fi.com/ffxivvibeplugin";

	private bool _expandedOnce;

	private readonly int WIDTH = 700;

	private readonly int HEIGHT = 600;

	private static readonly Vector2 MinSize;

	private readonly int COLUMN0_WIDTH = 130;

	private string _tmp_void = "";

	private int simulator_currentAllIntensity;

	private int TRIGGER_CURRENT_SELECTED_DEVICE = -1;

	private string CURRENT_TRIGGER_SELECTOR_SEARCHBAR = "";

	private int _tmp_currentDraggingTriggerIndex = -1;

	private readonly string VALID_REGEXP_PATTERN = "^(\\d+:\\d+)+(\\|\\d+:\\d+)*$";

	private string CURRENT_PATTERN_SEARCHBAR = "";

	private string _tmp_currentPatternNameToAdd = "";

	private string _tmp_currentPatternValueToAdd = "";

	private string _tmp_currentPatternValueState = "unset";

	private string _tmp_currentProfileNameToAdd = "";

	private string _tmp_currentProfile_ErrorMsg = "";

	private readonly int TRIGGER_MIN_AFTER;

	private readonly int TRIGGER_MAX_AFTER = 120;

	private Trigger? SelectedTrigger;

	private string triggersViewMode = "default";

	private string _tmp_exportPatternResponse = "";

	private Premium Premium;

	private string PremiumFeatureText = "PREMIUM FEATURE";

	private int FreeAccount_MaxTriggers = 10;

	public PluginUI(Main currentPlugin, Logger logger, DalamudPluginInterface pluginInterface, Configuration configuration, ConfigurationProfile profile, DevicesController deviceController, TriggersController triggersController, Patterns Patterns, Premium premium)
		: base("FFXIV Vibe Plugin Config", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse)
	{
		ImGui.SetNextWindowPos(new Vector2(100f, 100f), ImGuiCond.Appearing);
		ImGui.SetNextWindowSize(new Vector2(WIDTH, HEIGHT), ImGuiCond.Appearing);
		Logger = logger;
		Premium = premium;
		Configuration = configuration;
		ConfigurationProfile = profile;
		PluginInterface = pluginInterface;
		app = currentPlugin;
		DevicesController = deviceController;
		TriggerController = triggersController;
		this.Patterns = Patterns;
		LoadImages();
	}

	private void LoadImages()
	{
        // you might normally want to embed resources and load them from the manifest stream
        var file = new FileInfo(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "icon.png"));
        // ITextureProvider takes care of the image caching and dispose
        logoImage = app.TextureProvider.GetTextureFromFile(file);
    }

	public void Dispose()
	{
	}

	public void SetProfile(ConfigurationProfile profile)
	{
		ConfigurationProfile = profile;
	}

	public override void Draw()
	{
		try
		{
			DrawMainWindow();
		}
		catch (Exception e)
		{
			Logger.Error("UI ERROR: ");
			Logger.Error(e.ToString());
		}
		frameCounter = (frameCounter + 1) % 400;
	}

	public void DrawMainWindow()
	{
		FreeAccount_MaxTriggers = ((Premium == null) ? FreeAccount_MaxTriggers : Premium.FreeAccount_MaxTriggers);
		if (!_expandedOnce)
		{
			ImGui.SetNextWindowCollapsed(collapsed: false);
			_expandedOnce = true;
		}
		ImGui.Spacing();
		UIBanner.Draw(frameCounter, Logger, DonationLink, logoImage, KofiLink, DevicesController, Premium);
		ImGui.Columns(1);
		if (ImGui.BeginTabBar("##ConfigTabBar", ImGuiTabBarFlags.None))
		{
			if (ImGui.BeginTabItem("Connect"))
			{
				UIConnect.Draw(Configuration, ConfigurationProfile, app, DevicesController, Premium);
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Options"))
			{
				DrawOptionsTab();
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Devices"))
			{
				DrawDevicesTab();
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Triggers"))
			{
				DrawTriggersTab();
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Patterns"))
			{
				DrawPatternsTab();
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Help"))
			{
				DrawHelpTab();
				ImGui.EndTabItem();
			}
		}
	}

	public void DrawOptionsTab()
	{
		ImGui.TextColored(ImGuiColors.DalamudViolet, "General Settings");
		ImGui.BeginChild("###GENERAL_OPTIONS_ZONE", new Vector2(-1f, 145f), border: true);
		if (ImGui.BeginTable("###GENERAL_OPTIONS_TABLE", 2))
		{
			ImGui.TableSetupColumn("###GENERAL_OPTIONS_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, 250f);
			ImGui.TableSetupColumn("###GENERAL_OPTIONS_TABLE_COL2", ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableNextColumn();
			bool config_BUTTPLUG_SERVER_SHOULD_WSS = ConfigurationProfile.BUTTPLUG_SERVER_SHOULD_WSS;
			ImGui.Text("Connects through WSS");
			ImGui.TableNextColumn();
			if (ImGui.Checkbox("###GENERAL_OPTIONS_WSS", ref config_BUTTPLUG_SERVER_SHOULD_WSS))
			{
				ConfigurationProfile.BUTTPLUG_SERVER_SHOULD_WSS = config_BUTTPLUG_SERVER_SHOULD_WSS;
				Configuration.Save();
			}
			ImGui.SameLine();
			ImGuiComponents.HelpMarker("Connects through WSS rather than WS which should not be needed for local connection (default: false)");
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			bool config_AUTO_OPEN = ConfigurationProfile.AUTO_OPEN;
			ImGui.Text("Automatically open configuration panel.");
			ImGui.TableNextColumn();
			if (ImGui.Checkbox("###GENERAL_OPTIONS_AUTO_OPEN", ref config_AUTO_OPEN))
			{
				ConfigurationProfile.AUTO_OPEN = config_AUTO_OPEN;
				Configuration.Save();
			}
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text("Global threshold: ");
			ImGui.TableNextColumn();
			int config_MAX_VIBE_THRESHOLD = ConfigurationProfile.MAX_VIBE_THRESHOLD;
			ImGui.SetNextItemWidth(201f);
			if (ImGui.SliderInt("###OPTION_MaximumThreshold", ref config_MAX_VIBE_THRESHOLD, 2, 100))
			{
				ConfigurationProfile.MAX_VIBE_THRESHOLD = config_MAX_VIBE_THRESHOLD;
				Configuration.Save();
			}
			ImGui.SameLine();
			ImGuiComponents.HelpMarker("Maximum threshold for vibes (will override every devices).");
			ImGui.TableNextColumn();
			ImGui.Text("Log casted spells:");
			ImGui.TableNextColumn();
			if (ImGui.Checkbox("###OPTION_VERBOSE_SPELL", ref ConfigurationProfile.VERBOSE_SPELL))
			{
				Configuration.Save();
			}
			ImGui.SameLine();
			ImGuiComponents.HelpMarker("Use the /xllog to see all casted spells. Disable this to have better ingame performance.");
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text("Log chat triggered:");
			ImGui.TableNextColumn();
			if (ImGui.Checkbox("###OPTION_VERBOSE_CHAT", ref ConfigurationProfile.VERBOSE_CHAT))
			{
				Configuration.Save();
			}
			ImGui.SameLine();
			ImGuiComponents.HelpMarker("Use the /xllog to see all chat message. Disable this to have better ingame performance.");
			ImGui.EndTable();
		}
		if (ConfigurationProfile.VERBOSE_CHAT || ConfigurationProfile.VERBOSE_SPELL)
		{
			ImGui.TextColored(ImGuiColors.DalamudOrange, "Please, disabled chat and spell logs for better ingame performance.");
		}
		ImGui.EndChild();
		ImGui.Spacing();
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Profile settings");
		float CONFIG_PROFILE_ZONE_HEIGHT = ((_tmp_currentProfile_ErrorMsg == "") ? 100f : 120f);
		ImGui.BeginChild("###CONFIGURATION_PROFILE_ZONE", new Vector2(-1f, CONFIG_PROFILE_ZONE_HEIGHT), border: true);
		if (Premium != null && Premium.IsPremium())
		{
			if (ImGui.BeginTable("###CONFIGURATION_PROFILE_TABLE", 3))
			{
				ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, 150f);
				ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL2", ImGuiTableColumnFlags.WidthFixed, 350f);
				ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL3", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableNextColumn();
				ImGui.Text("Current profile:");
				ImGui.TableNextColumn();
				string[] PROFILES = Configuration.Profiles.Select((ConfigurationProfile profile) => profile.Name).ToArray();
				int currentProfileIndex = Configuration.Profiles.FindIndex((ConfigurationProfile profile) => profile.Name == Configuration.CurrentProfileName);
				ImGui.SetNextItemWidth(350f);
				if (ImGui.Combo("###CONFIGURATION_CURRENT_PROFILE", ref currentProfileIndex, PROFILES, PROFILES.Length))
				{
					Configuration.CurrentProfileName = Configuration.Profiles[currentProfileIndex].Name;
					app.SetProfile(Configuration.CurrentProfileName);
					Logger.Debug("New profile selected: " + Configuration.CurrentProfileName);
					Configuration.Save();
				}
				ImGui.TableNextColumn();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
				{
					if (Configuration.Profiles.Count <= 1)
					{
						string errorMsg = "You can't delete this profile. At least one profile should exists. Create another one before deleting.";
						Logger.Error(errorMsg);
						_tmp_currentProfile_ErrorMsg = errorMsg;
					}
					else
					{
						Configuration.RemoveProfile(ConfigurationProfile.Name);
						ConfigurationProfile newProfileToUse = Configuration.GetFirstProfile();
						if (newProfileToUse != null)
						{
							app.SetProfile(newProfileToUse.Name);
						}
						Configuration.Save();
					}
				}
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text("Add new profile: ");
				ImGui.TableNextColumn();
				ImGui.SetNextItemWidth(350f);
				if (ImGui.InputText("###CONFIGURATION_NEW_PROFILE_NAME", ref _tmp_currentProfileNameToAdd, 150u))
				{
					_tmp_currentProfile_ErrorMsg = "";
				}
				ImGui.TableNextColumn();
				if (_tmp_currentProfileNameToAdd.Length > 0 && ImGuiComponents.IconButton(FontAwesomeIcon.Plus) && _tmp_currentProfileNameToAdd.Trim() != "")
				{
					if (!Configuration.AddProfile(_tmp_currentProfileNameToAdd))
					{
						string errorMsg = "The current profile name '" + _tmp_currentProfileNameToAdd + "' already exists!";
						Logger.Error(errorMsg);
						_tmp_currentProfile_ErrorMsg = errorMsg;
					}
					else
					{
						app.SetProfile(_tmp_currentProfileNameToAdd);
						Logger.Debug("New profile added " + _tmp_currentProfileNameToAdd);
						_tmp_currentProfileNameToAdd = "";
						Configuration.Save();
					}
				}
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text("Rename current profile");
				ImGui.TableNextColumn();
				ImGui.SetNextItemWidth(350f);
				if (ImGui.InputText("###CONFIGURATION_CURRENT_PROFILE_RENAME", ref ConfigurationProfile.Name, 150u))
				{
					Configuration.CurrentProfileName = ConfigurationProfile.Name;
					Configuration.Save();
				}
				ImGui.EndTable();
			}
			if (_tmp_currentProfile_ErrorMsg != "")
			{
				ImGui.TextColored(ImGuiColors.DalamudRed, _tmp_currentProfile_ErrorMsg);
			}
		}
		else
		{
			ImGui.TextColored(ImGuiColors.DalamudGrey, PremiumFeatureText);
		}
		ImGui.EndChild();
		ImGui.Spacing();
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Triggers Import/Export Settings");
		ImGui.BeginChild("###EXPORT_OPTIONS_ZONE", new Vector2(-1f, 100f), border: true);
		if (Premium != null && Premium.IsPremium())
		{
			if (ImGui.BeginTable("###EXPORT_OPTIONS_TABLE", 2))
			{
				ImGui.TableSetupColumn("###EXPORT_OPTIONS_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, 250f);
				ImGui.TableSetupColumn("###EXPORT_OPTIONS_TABLE_COL2", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableNextColumn();
				ImGui.Text("Trigger Import/Export Directory:");
				ImGui.TableNextColumn();
				if (ImGui.InputText("###EXPORT_DIRECTORY_INPUT", ref ConfigurationProfile.EXPORT_DIR, 200u))
				{
					Configuration.EXPORT_DIR = ConfigurationProfile.EXPORT_DIR;
					Configuration.Save();
				}
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				if (ImGui.Button("Clear Import/Export Directory") && !ConfigurationProfile.EXPORT_DIR.Equals(""))
				{
					try
					{
						string[] files = Directory.GetFiles(ConfigurationProfile.EXPORT_DIR);
						for (int i = 0; i < files.Length; i++)
						{
							File.Delete(files[i]);
						}
					}
					catch
					{
					}
				}
				ImGui.SameLine();
				ImGuiComponents.HelpMarker("Deletes ALL files in the Import/Export Directory.");
				ImGui.EndTable();
			}
			else
			{
				ImGui.TextColored(ImGuiColors.DalamudGrey, PremiumFeatureText);
			}
		}
		ImGui.EndChild();
	}

	public void DrawDevicesTab()
	{
		ImGui.Spacing();
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions");
		ImGui.BeginChild("###DevicesTab_General", new Vector2(-1f, 40f), border: true);
		if (DevicesController.IsScanning())
		{
			if (ImGui.Button("Stop scanning", new Vector2(100f, 24f)))
			{
				DevicesController.StopScanningDevice();
			}
		}
		else if (ImGui.Button("Scan device", new Vector2(100f, 24f)))
		{
			DevicesController.ScanDevice();
		}
		ImGui.SameLine();
		if (ImGui.Button("Update Battery", new Vector2(100f, 24f)))
		{
			DevicesController.UpdateAllBatteryLevel();
		}
		ImGui.SameLine();
		if (ImGui.Button("Stop All", new Vector2(100f, 24f)))
		{
			DevicesController.StopAll();
			simulator_currentAllIntensity = 0;
		}
		ImGui.EndChild();
		if (ImGui.CollapsingHeader("All devices"))
		{
			ImGui.Text("Send to all:");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(200f);
			if (ImGui.SliderInt("###SendVibeAll_Intensity", ref simulator_currentAllIntensity, 0, 100))
			{
				DevicesController.SendVibeToAll(simulator_currentAllIntensity);
			}
		}
		foreach (FFXIV_Vibe_Plugin.Device.Device device in DevicesController.GetDevices())
		{
			if (!ImGui.CollapsingHeader($"[{device.Id}] {device.Name} - Battery: {device.GetBatteryPercentage()}"))
			{
				continue;
			}
			ImGui.TextWrapped(device.ToString());
			if (device.CanVibrate)
			{
				ImGui.TextColored(ImGuiColors.DalamudViolet, "VIBRATE");
				ImGui.Indent(10f);
				for (int i = 0; i < device.VibrateMotors; i++)
				{
					ImGui.Text($"Motor {i + 1}: ");
					ImGui.SameLine();
					ImGui.SetNextItemWidth(200f);
					if (ImGui.SliderInt($"###{device.Id} Intensity Vibrate Motor {i}", ref device.CurrentVibrateIntensity[i], 0, 100))
					{
						DevicesController.SendVibrate(device, device.CurrentVibrateIntensity[i], i);
					}
				}
				ImGui.Unindent(10f);
			}
			if (device.CanRotate)
			{
				ImGui.TextColored(ImGuiColors.DalamudViolet, "ROTATE");
				ImGui.Indent(10f);
				for (int i = 0; i < device.RotateMotors; i++)
				{
					ImGui.Text($"Motor {i + 1}: ");
					ImGui.SameLine();
					ImGui.SetNextItemWidth(200f);
					if (ImGui.SliderInt($"###{device.Id} Intensity Rotate Motor {i}", ref device.CurrentRotateIntensity[i], 0, 100))
					{
						DevicesController.SendRotate(device, device.CurrentRotateIntensity[i], i);
					}
				}
				ImGui.Unindent(10f);
			}
			if (device.CanLinear)
			{
				ImGui.TextColored(ImGuiColors.DalamudViolet, "LINEAR VIBES");
				ImGui.Indent(10f);
				for (int i = 0; i < device.LinearMotors; i++)
				{
					ImGui.Text($"Motor {i + 1}: ");
					ImGui.SameLine();
					ImGui.SetNextItemWidth(200f);
					if (ImGui.SliderInt($"###{device.Id} Intensity Linear Motor {i}", ref device.CurrentLinearIntensity[i], 0, 100))
					{
						DevicesController.SendLinear(device, device.CurrentLinearIntensity[i], 500, i);
					}
				}
				ImGui.Unindent(10f);
			}
			if (!device.CanOscillate)
			{
				continue;
			}
			ImGui.TextColored(ImGuiColors.DalamudViolet, "OSCILLATE VIBES");
			ImGui.Indent(10f);
			for (int i = 0; i < device.OscillateMotors; i++)
			{
				ImGui.Text($"Motor {i + 1}: ");
				ImGui.SameLine();
				ImGui.SetNextItemWidth(200f);
				if (ImGui.SliderInt($"###{device.Id} Intensity Oscillate Motor {i}", ref device.CurrentOscillateIntensity[i], 0, 100))
				{
					DevicesController.SendOscillate(device, device.CurrentOscillateIntensity[i], 500, i);
				}
			}
			ImGui.Unindent(10f);
		}
	}

	public unsafe void DrawTriggersTab()
	{
		List<Trigger> triggers = TriggerController.GetTriggers();
		string selectedId = ((SelectedTrigger != null) ? SelectedTrigger.Id : "");
		if (ImGui.BeginChild("###TriggersSelector", new Vector2(ImGui.GetWindowContentRegionMax().X / 3f, 0f - ImGui.GetFrameHeightWithSpacing()), border: true))
		{
			ImGui.SetNextItemWidth(185f);
			ImGui.InputText("###TriggersSelector_SearchBar", ref CURRENT_TRIGGER_SELECTOR_SEARCHBAR, 200u);
			ImGui.Spacing();
			int maxTrigger = triggers.Count;
			if (Premium == null || !Premium.IsPremium())
			{
				maxTrigger = FreeAccount_MaxTriggers;
			}
			for (int triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
			{
				Trigger trigger = triggers[triggerIndex];
				if (trigger == null)
				{
					continue;
				}
				string enabled = (trigger.Enabled ? "" : "[disabled]");
				string kindStr = Enum.GetName(typeof(KIND), trigger.Kind) ?? "";
				if (kindStr != null)
				{
					kindStr = kindStr.ToUpper();
				}
				string triggerName = $"{enabled}[{kindStr}] {trigger.Name}";
				string triggerNameWithId = triggerName + "###" + trigger.Id;
				if (!Helpers.RegExpMatch(Logger, triggerName, CURRENT_TRIGGER_SELECTOR_SEARCHBAR))
				{
					continue;
				}
				if (triggerIndex < maxTrigger)
				{
					if (ImGui.Selectable(triggerName ?? "", selectedId == trigger.Id))
					{
						SelectedTrigger = trigger;
						triggersViewMode = "edit";
					}
				}
				else
				{
					ImGui.TextColored(ImGuiColors.DalamudGrey, PremiumFeatureText + ": " + triggerName);
				}
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(triggerName ?? "");
				}
				if (ImGui.BeginDragDropSource())
				{
					_tmp_currentDraggingTriggerIndex = triggerIndex;
					ImGui.Text("Dragging: " + triggerName);
					ImGui.SetDragDropPayload(triggerNameWithId ?? "", (nint)(&triggerIndex), 4u);
					ImGui.EndDragDropSource();
				}
				if (ImGui.BeginDragDropTarget())
				{
					if (_tmp_currentDraggingTriggerIndex > -1 && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
					{
						int srcIndex = _tmp_currentDraggingTriggerIndex;
						int targetIndex = triggerIndex;
						List<Trigger> list = triggers;
						int index2 = srcIndex;
						int index3 = targetIndex;
						Trigger value = triggers[targetIndex];
						Trigger value2 = triggers[srcIndex];
						list[index2] = value;
						triggers[index3] = value2;
						_tmp_currentDraggingTriggerIndex = -1;
						Configuration.Save();
					}
					ImGui.EndDragDropTarget();
				}
			}
			ImGui.EndChild();
		}
		ImGui.SameLine();
		if (ImGui.BeginChild("###TriggerViewerPanel", new Vector2(0f, 0f - ImGui.GetFrameHeightWithSpacing()), border: true))
		{
			if (triggersViewMode == "default")
			{
				ImGui.Text("Please select or add a trigger");
			}
			else if (triggersViewMode == "edit")
			{
				if (SelectedTrigger != null)
				{
					if (ImGui.BeginTable("###TRIGGER_FORM_TABLE_GENERAL", 2))
					{
						ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
						ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL2", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableNextColumn();
						ImGui.Text("TriggerID:");
						ImGui.TableNextColumn();
						ImGui.Text(SelectedTrigger.GetShortID() ?? "");
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text("Enabled:");
						ImGui.TableNextColumn();
						if (ImGui.Checkbox("###TRIGGER_ENABLED", ref SelectedTrigger.Enabled))
						{
							Configuration.Save();
						}
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text("Trigger Name:");
						ImGui.TableNextColumn();
						if (ImGui.InputText("###TRIGGER_NAME", ref SelectedTrigger.Name, 99u))
						{
							if (SelectedTrigger.Name == "")
							{
								SelectedTrigger.Name = "no_name";
							}
							Configuration.Save();
						}
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text("Trigger Description:");
						ImGui.TableNextColumn();
						if (ImGui.InputTextMultiline("###TRIGGER_DESCRIPTION", ref SelectedTrigger.Description, 500u, new Vector2(190f, 50f)))
						{
							if (SelectedTrigger.Description == "")
							{
								SelectedTrigger.Description = "no_description";
							}
							Configuration.Save();
						}
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text("Kind:");
						ImGui.TableNextColumn();
						string[] TRIGGER_KIND = Enum.GetNames(typeof(KIND));
						bool isNotPremium = Premium == null || !Premium.IsPremium();
						if (isNotPremium)
						{
							TRIGGER_KIND[3] = "HPChangeOther (PREMIUM ONLY)";
						}
						int currentKind = SelectedTrigger.Kind;
						if (ImGui.Combo("###TRIGGER_FORM_KIND", ref currentKind, TRIGGER_KIND, TRIGGER_KIND.Length))
						{
							if (currentKind == 3 && isNotPremium)
							{
								ImGui.OpenPopup("HpChangeOther Premium Only");
								currentKind = 2;
							}
							SelectedTrigger.Kind = currentKind;
							Configuration.Save();
						}
						bool shouldWork = true;
						if (ImGui.BeginPopupModal("HpChangeOther Premium Only", ref shouldWork, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
						{
							Vector2 buttonSize = new Vector2(40f, 25f);
							ImGui.TextColored(ImGuiColors.DalamudViolet, "The HPChangeOther kind is a premium feature!");
							ImGui.Indent(ImGui.GetWindowWidth() * 0.5f - buttonSize.X);
							if (ImGui.Button("OK", buttonSize))
							{
								ImGui.CloseCurrentPopup();
							}
							ImGui.EndPopup();
						}
						ImGui.TableNextRow();
						if (currentKind != 2)
						{
							ImGui.TableNextColumn();
							ImGui.Text("Player name:");
							ImGui.TableNextColumn();
							if (Premium != null && Premium.IsPremium())
							{
								if (ImGui.InputText("###TRIGGER_CHAT_FROM_PLAYER_NAME", ref SelectedTrigger.FromPlayerName, 100u))
								{
									SelectedTrigger.FromPlayerName = SelectedTrigger.FromPlayerName.Trim();
									Configuration.Save();
								}
								ImGui.SameLine();
								ImGuiComponents.HelpMarker("You can use RegExp. Leave empty for any. Ignored if chat listening to 'Echo' and chat message we through it.");
							}
							else
							{
								ImGui.TextColored(ImGuiColors.DalamudGrey, PremiumFeatureText);
							}
							ImGui.TableNextRow();
						}
						ImGui.TableNextColumn();
						ImGui.Text("Start after");
						ImGui.TableNextColumn();
						ImGui.SetNextItemWidth(185f);
						if (ImGui.SliderFloat("###TRIGGER_FORM_START_AFTER", ref SelectedTrigger.StartAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER))
						{
							SelectedTrigger.StartAfter = Helpers.ClampFloat(SelectedTrigger.StartAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER);
							Configuration.Save();
						}
						ImGui.SameLine();
						ImGui.SetNextItemWidth(45f);
						if (ImGui.InputFloat("###TRIGGER_FORM_START_AFTER_INPUT", ref SelectedTrigger.StartAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER))
						{
							SelectedTrigger.StartAfter = Helpers.ClampFloat(SelectedTrigger.StartAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER);
							Configuration.Save();
						}
						ImGui.SameLine();
						ImGuiComponents.HelpMarker("In seconds");
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text("Stop after");
						ImGui.TableNextColumn();
						ImGui.SetNextItemWidth(185f);
						if (ImGui.SliderFloat("###TRIGGER_FORM_STOP_AFTER", ref SelectedTrigger.StopAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER))
						{
							SelectedTrigger.StopAfter = Helpers.ClampFloat(SelectedTrigger.StopAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER);
							Configuration.Save();
						}
						ImGui.SameLine();
						ImGui.SetNextItemWidth(45f);
						if (ImGui.InputFloat("###TRIGGER_FORM_STOP_AFTER_INPUT", ref SelectedTrigger.StopAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER))
						{
							SelectedTrigger.StopAfter = Helpers.ClampFloat(SelectedTrigger.StopAfter, TRIGGER_MIN_AFTER, TRIGGER_MAX_AFTER);
							Configuration.Save();
						}
						ImGui.SameLine();
						ImGuiComponents.HelpMarker("In seconds. Use zero to avoid stopping.");
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text("Priority");
						ImGui.TableNextColumn();
						if (Premium != null && Premium.IsPremium())
						{
							if (ImGui.InputInt("###TRIGGER_FORM_PRIORITY", ref SelectedTrigger.Priority, 1))
							{
								Configuration.Save();
							}
							ImGui.SameLine();
							ImGuiComponents.HelpMarker("If a trigger have a lower priority, it will be ignored.");
							ImGui.TableNextRow();
						}
						else
						{
							ImGui.TextColored(ImGuiColors.DalamudGrey, PremiumFeatureText);
						}
						ImGui.EndTable();
					}
					ImGui.Separator();
					if (SelectedTrigger.Kind == 0 && ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_CHAT", 2))
					{
						ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
						ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL2", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableNextColumn();
						ImGui.Text("Chat text:");
						ImGui.TableNextColumn();
						string currentChatText = SelectedTrigger.ChatText;
						if (ImGui.InputText("###TRIGGER_CHAT_TEXT", ref currentChatText, 250u))
						{
							SelectedTrigger.ChatText = currentChatText.ToLower();
							Configuration.Save();
						}
						ImGui.SameLine();
						ImGuiComponents.HelpMarker("It is case insensitive. Also, you can use RegExp if you wish to.");
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text("Add chat type:");
						ImGui.TableNextColumn();
						int currentTypeAllowed = 0;
						string[] ChatTypesAllowedStrings = Enum.GetNames(typeof(XivChatType));
						if (ImGui.Combo("###TRIGGER_CHAT_TEXT_TYPE_ALLOWED", ref currentTypeAllowed, ChatTypesAllowedStrings, ChatTypesAllowedStrings.Length))
						{
							if (!SelectedTrigger.AllowedChatTypes.Contains(currentTypeAllowed))
							{
								int XivChatTypeValue = (int)(XivChatType)Enum.Parse(typeof(XivChatType), ChatTypesAllowedStrings[currentTypeAllowed]);
								SelectedTrigger.AllowedChatTypes.Add(XivChatTypeValue);
							}
							Configuration.Save();
						}
						ImGuiComponents.HelpMarker("Select some chats to observe or unselect all to watch every chats.");
						ImGui.TableNextRow();
						if (SelectedTrigger.AllowedChatTypes.Count > 0)
						{
							ImGui.TableNextColumn();
							ImGui.Text("Allowed Type:");
							ImGui.TableNextColumn();
							for (int indexAllowedChatType = 0; indexAllowedChatType < SelectedTrigger.AllowedChatTypes.Count; indexAllowedChatType++)
							{
								int num = SelectedTrigger.AllowedChatTypes[indexAllowedChatType];
								if (ImGuiComponents.IconButton(indexAllowedChatType, FontAwesomeIcon.Minus))
								{
									SelectedTrigger.AllowedChatTypes.RemoveAt(indexAllowedChatType);
									Configuration.Save();
								}
								ImGui.SameLine();
								ImGui.Text(((XivChatType)num).ToString() ?? "");
							}
							ImGui.TableNextRow();
						}
						ImGui.EndTable();
					}
					if (ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_SPELL", 2))
					{
						ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
						ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL2", ImGuiTableColumnFlags.WidthStretch);
						if (SelectedTrigger.Kind == 1)
						{
							ImGui.TableNextColumn();
							ImGui.Text("Type:");
							ImGui.TableNextColumn();
							string[] TRIGGER = Enum.GetNames(typeof(Structures.ActionEffectType));
							int currentEffectType = SelectedTrigger.ActionEffectType;
							if (ImGui.Combo("###TRIGGER_FORM_EVENT", ref currentEffectType, TRIGGER, TRIGGER.Length))
							{
								SelectedTrigger.ActionEffectType = currentEffectType;
								SelectedTrigger.Reset();
								Configuration.Save();
							}
							ImGui.TableNextRow();
							ImGui.TableNextColumn();
							ImGui.Text("Spell Text:");
							ImGui.TableNextColumn();
							if (ImGui.InputText("###TRIGGER_FORM_SPELLNAME", ref SelectedTrigger.SpellText, 100u))
							{
								Configuration.Save();
							}
							ImGui.SameLine();
							ImGuiComponents.HelpMarker("You can use RegExp.");
							ImGui.TableNextRow();
							ImGui.TableNextColumn();
							ImGui.Text("Direction:");
							ImGui.TableNextColumn();
							string[] DIRECTIONS = Enum.GetNames(typeof(DIRECTION));
							int currentDirection = SelectedTrigger.Direction;
							if (ImGui.Combo("###TRIGGER_FORM_DIRECTION", ref currentDirection, DIRECTIONS, DIRECTIONS.Length))
							{
								SelectedTrigger.Direction = currentDirection;
								Configuration.Save();
							}
							ImGui.SameLine();
							ImGuiComponents.HelpMarker("Warning: Hitting no target will result to self as if you cast on yourself");
							ImGui.TableNextRow();
						}
						if (SelectedTrigger.ActionEffectType == 3 || SelectedTrigger.ActionEffectType == 4 || SelectedTrigger.Kind == 2 || SelectedTrigger.Kind == 3)
						{
							string type = "";
							if (SelectedTrigger.ActionEffectType == 3)
							{
								type = "damage";
							}
							if (SelectedTrigger.ActionEffectType == 4)
							{
								type = "heal";
							}
							if (SelectedTrigger.Kind == 2)
							{
								type = "health";
							}
							if (SelectedTrigger.Kind == 3)
							{
								type = "health";
							}
							ImGui.TableNextColumn();
							ImGui.Text("Amount in percentage?");
							ImGui.TableNextColumn();
							if (ImGui.Checkbox("###TRIGGER_AMOUNT_IN_PERCENTAGE", ref SelectedTrigger.AmountInPercentage))
							{
								SelectedTrigger.AmountMinValue = 0;
								SelectedTrigger.AmountMaxValue = 100;
								Configuration.Save();
							}
							ImGui.TableNextColumn();
							ImGui.Text("Min " + type + " value:");
							ImGui.TableNextColumn();
							if (SelectedTrigger.AmountInPercentage)
							{
								if (ImGui.SliderInt("###TRIGGER_FORM_MIN_AMOUNT", ref SelectedTrigger.AmountMinValue, 0, 100))
								{
									Configuration.Save();
								}
							}
							else if (ImGui.InputInt("###TRIGGER_FORM_MIN_AMOUNT", ref SelectedTrigger.AmountMinValue, 100))
							{
								Configuration.Save();
							}
							ImGui.TableNextRow();
							ImGui.TableNextColumn();
							ImGui.Text("Max " + type + " value:");
							ImGui.TableNextColumn();
							if (SelectedTrigger.AmountInPercentage)
							{
								if (ImGui.SliderInt("###TRIGGER_FORM_MAX_AMOUNT", ref SelectedTrigger.AmountMaxValue, 0, 100))
								{
									Configuration.Save();
								}
							}
							else if (ImGui.InputInt("###TRIGGER_FORM_MAX_AMOUNT", ref SelectedTrigger.AmountMaxValue, 100))
							{
								Configuration.Save();
							}
							ImGui.TableNextRow();
						}
						ImGui.TableNextColumn();
						ImGui.Text("Only in combat:");
						ImGui.TableNextColumn();
						if (ImGui.Checkbox("###TRIGGER_FORM_ONLY_IN_COMBAT", ref SelectedTrigger.OnlyInCombat))
						{
							Configuration.Save();
						}
						ImGui.TableNextRow();
						ImGui.EndTable();
					}
					ImGui.Separator();
					ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions");
					ImGui.Separator();
					if (ImGui.Button("Test trigger"))
					{
						DevicesController.SendTrigger(SelectedTrigger);
					}
					ImGui.SameLine();
					if (ImGui.Button("Export"))
					{
						_tmp_exportPatternResponse = export_trigger(SelectedTrigger);
					}
					ImGui.SameLine();
					ImGuiComponents.HelpMarker("Writes this trigger to your export directory.");
					ImGui.SameLine();
					ImGui.Text(_tmp_exportPatternResponse ?? "");
					ImGui.Separator();
					ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions & Devices");
					ImGui.Separator();
					Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> visitedDevice = DevicesController.GetVisitedDevices();
					if (visitedDevice.Count == 0)
					{
						ImGui.TextColored(ImGuiColors.DalamudRed, "Please connect yourself to intiface and add device(s)...");
					}
					else
					{
						string[] devicesStrings = visitedDevice.Keys.ToArray();
						ImGui.Combo("###TRIGGER_FORM_COMBO_DEVICES", ref TRIGGER_CURRENT_SELECTED_DEVICE, devicesStrings, devicesStrings.Length);
						ImGui.SameLine();
						List<TriggerDevice> triggerDevices = SelectedTrigger.Devices;
						if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus) && TRIGGER_CURRENT_SELECTED_DEVICE >= 0)
						{
							TriggerDevice newTriggerDevice = new TriggerDevice(visitedDevice[devicesStrings[TRIGGER_CURRENT_SELECTED_DEVICE]]);
							triggerDevices.Add(newTriggerDevice);
							Configuration.Save();
						}
						string[] patternNames = (from p in Patterns.GetAllPatterns()
							select p.Name).ToArray();
						for (int indexDevice = 0; indexDevice < triggerDevices.Count; indexDevice++)
						{
							string prefixLabel = $"###TRIGGER_FORM_COMBO_DEVICE_${indexDevice}";
							TriggerDevice triggerDevice = triggerDevices[indexDevice];
							if (!ImGui.CollapsingHeader(((triggerDevice.Device != null) ? triggerDevice.Device.Name : "UnknownDevice") ?? ""))
							{
								continue;
							}
							ImGui.Indent(10f);
							if (triggerDevice != null && triggerDevice.Device != null)
							{
								if (triggerDevice.Device.CanVibrate)
								{
									if (ImGui.Checkbox(prefixLabel + "_SHOULD_VIBRATE", ref triggerDevice.ShouldVibrate))
									{
										triggerDevice.ShouldStop = false;
										Configuration.Save();
									}
									ImGui.SameLine();
									ImGui.Text("Should Vibrate");
									if (triggerDevice.ShouldVibrate)
									{
										ImGui.Indent(20f);
										for (int motorId = 0; motorId < triggerDevice.Device.VibrateMotors; motorId++)
										{
											ImGui.Text($"Motor {motorId + 1}");
											ImGui.SameLine();
											if (ImGui.Checkbox($"{prefixLabel}_SHOULD_VIBRATE_MOTOR_{motorId}", ref triggerDevice.VibrateSelectedMotors[motorId]))
											{
												Configuration.Save();
											}
											if (!triggerDevice.VibrateSelectedMotors[motorId])
											{
												continue;
											}
											ImGui.SameLine();
											ImGui.SetNextItemWidth(90f);
											if (ImGui.Combo($"###{prefixLabel}_VIBRATE_PATTERNS_{motorId}", ref triggerDevice.VibrateMotorsPattern[motorId], patternNames, patternNames.Length))
											{
												Configuration.Save();
											}
											_ = triggerDevice.VibrateMotorsPattern[motorId];
											ImGui.SameLine();
											ImGui.SetNextItemWidth(180f);
											if (ImGui.SliderInt($"{prefixLabel}_SHOULD_VIBRATE_MOTOR_{motorId}_THRESHOLD", ref triggerDevice.VibrateMotorsThreshold[motorId], 0, 100))
											{
												if (triggerDevice.VibrateMotorsThreshold[motorId] > 0)
												{
													triggerDevice.VibrateSelectedMotors[motorId] = true;
												}
												Configuration.Save();
											}
										}
										ImGui.Indent(-20f);
									}
								}
								if (triggerDevice.Device.CanRotate)
								{
									if (ImGui.Checkbox(prefixLabel + "_SHOULD_ROTATE", ref triggerDevice.ShouldRotate))
									{
										triggerDevice.ShouldStop = false;
										Configuration.Save();
									}
									ImGui.SameLine();
									ImGui.Text("Should Rotate");
									if (triggerDevice.ShouldRotate)
									{
										ImGui.Indent(20f);
										for (int motorId = 0; motorId < triggerDevice.Device.RotateMotors; motorId++)
										{
											ImGui.Text($"Motor {motorId + 1}");
											ImGui.SameLine();
											if (ImGui.Checkbox($"{prefixLabel}_SHOULD_ROTATE_MOTOR_{motorId}", ref triggerDevice.RotateSelectedMotors[motorId]))
											{
												Configuration.Save();
											}
											if (!triggerDevice.RotateSelectedMotors[motorId])
											{
												continue;
											}
											ImGui.SameLine();
											ImGui.SetNextItemWidth(90f);
											if (ImGui.Combo($"###{prefixLabel}_ROTATE_PATTERNS_{motorId}", ref triggerDevice.RotateMotorsPattern[motorId], patternNames, patternNames.Length))
											{
												Configuration.Save();
											}
											_ = triggerDevice.RotateMotorsPattern[motorId];
											ImGui.SameLine();
											ImGui.SetNextItemWidth(180f);
											if (ImGui.SliderInt($"{prefixLabel}_SHOULD_ROTATE_MOTOR_{motorId}_THRESHOLD", ref triggerDevice.RotateMotorsThreshold[motorId], 0, 100))
											{
												if (triggerDevice.RotateMotorsThreshold[motorId] > 0)
												{
													triggerDevice.RotateSelectedMotors[motorId] = true;
												}
												Configuration.Save();
											}
										}
										ImGui.Indent(-20f);
									}
								}
								if (triggerDevice.Device.CanLinear)
								{
									if (ImGui.Checkbox(prefixLabel + "_SHOULD_LINEAR", ref triggerDevice.ShouldLinear))
									{
										triggerDevice.ShouldStop = false;
										Configuration.Save();
									}
									ImGui.SameLine();
									ImGui.Text("Should Linear");
									if (triggerDevice.ShouldLinear)
									{
										ImGui.Indent(20f);
										for (int motorId = 0; motorId < triggerDevice.Device.LinearMotors; motorId++)
										{
											ImGui.Text($"Motor {motorId + 1}");
											ImGui.SameLine();
											if (ImGui.Checkbox($"{prefixLabel}_SHOULD_LINEAR_MOTOR_{motorId}", ref triggerDevice.LinearSelectedMotors[motorId]))
											{
												Configuration.Save();
											}
											if (!triggerDevice.LinearSelectedMotors[motorId])
											{
												continue;
											}
											ImGui.SameLine();
											ImGui.SetNextItemWidth(90f);
											if (ImGui.Combo($"###{prefixLabel}_LINEAR_PATTERNS_{motorId}", ref triggerDevice.LinearMotorsPattern[motorId], patternNames, patternNames.Length))
											{
												Configuration.Save();
											}
											_ = triggerDevice.LinearMotorsPattern[motorId];
											ImGui.SameLine();
											ImGui.SetNextItemWidth(180f);
											if (ImGui.SliderInt($"{prefixLabel}_SHOULD_LINEAR_MOTOR_{motorId}_THRESHOLD", ref triggerDevice.LinearMotorsThreshold[motorId], 0, 100))
											{
												if (triggerDevice.LinearMotorsThreshold[motorId] > 0)
												{
													triggerDevice.LinearSelectedMotors[motorId] = true;
												}
												Configuration.Save();
											}
										}
										ImGui.Indent(-20f);
									}
								}
								if (triggerDevice.Device.CanOscillate)
								{
									if (ImGui.Checkbox(prefixLabel + "_SHOULD_OSCILLATE", ref triggerDevice.ShouldOscillate))
									{
										triggerDevice.ShouldStop = false;
										Configuration.Save();
									}
									ImGui.SameLine();
									ImGui.Text("Should Oscillate");
									if (triggerDevice.ShouldOscillate)
									{
										ImGui.Indent(20f);
										for (int motorId = 0; motorId < triggerDevice.Device.OscillateMotors; motorId++)
										{
											ImGui.Text($"Motor {motorId + 1}");
											ImGui.SameLine();
											if (ImGui.Checkbox($"{prefixLabel}_SHOULD_OSCILLATE_MOTOR_{motorId}", ref triggerDevice.OscillateSelectedMotors[motorId]))
											{
												Configuration.Save();
											}
											if (!triggerDevice.OscillateSelectedMotors[motorId])
											{
												continue;
											}
											ImGui.SameLine();
											ImGui.SetNextItemWidth(90f);
											if (ImGui.Combo($"###{prefixLabel}_OSCILLATE_PATTERNS_{motorId}", ref triggerDevice.OscillateMotorsPattern[motorId], patternNames, patternNames.Length))
											{
												Configuration.Save();
											}
											_ = triggerDevice.OscillateMotorsPattern[motorId];
											ImGui.SameLine();
											ImGui.SetNextItemWidth(180f);
											if (ImGui.SliderInt($"{prefixLabel}_SHOULD_OSCILLATE_MOTOR_{motorId}_THRESHOLD", ref triggerDevice.OscillateMotorsThreshold[motorId], 0, 100))
											{
												if (triggerDevice.OscillateMotorsThreshold[motorId] > 0)
												{
													triggerDevice.OscillateSelectedMotors[motorId] = true;
												}
												Configuration.Save();
											}
										}
										ImGui.Indent(-20f);
									}
								}
								if (ImGui.Button("Remove###" + prefixLabel + "_REMOVE"))
								{
									triggerDevices.RemoveAt(indexDevice);
									Logger.Log($"DEBUG: removing {indexDevice}");
									Configuration.Save();
								}
							}
							ImGui.Indent(-10f);
						}
					}
				}
				else
				{
					ImGui.TextColored(ImGuiColors.DalamudRed, "Current selected trigger is null");
				}
			}
			else if (triggersViewMode == "delete" && SelectedTrigger != null)
			{
				ImGui.TextColored(ImGuiColors.DalamudRed, "Are you sure you want to delete trigger ID: " + SelectedTrigger.Name + ":" + SelectedTrigger.Id);
				if (ImGui.Button("Yes"))
				{
					if (SelectedTrigger != null)
					{
						TriggerController.RemoveTrigger(SelectedTrigger);
						SelectedTrigger = null;
						Configuration.Save();
					}
					triggersViewMode = "default";
				}
				ImGui.SameLine();
				if (ImGui.Button("No"))
				{
					SelectedTrigger = null;
					triggersViewMode = "default";
				}
			}
			ImGui.EndChild();
		}
		if ((Premium != null && Premium.IsPremium()) || triggers.Count < FreeAccount_MaxTriggers)
		{
			if (ImGui.Button("Add"))
			{
				int index = 0;
				Trigger trigger = new Trigger($"New Trigger {index}");
				while (TriggerController.GetTriggers().Contains(trigger))
				{
					index++;
					trigger = new Trigger($"New Trigger {index}");
				}
				TriggerController.AddTrigger(trigger);
				SelectedTrigger = trigger;
				triggersViewMode = "edit";
				Configuration.Save();
			}
			ImGui.SameLine();
		}
		else
		{
			ImGui.TextColored(ImGuiColors.DalamudRed, "To add more triggers you need a premium account");
			ImGui.SameLine();
		}
		bool num2 = ImGui.Button("Delete");
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Hold shift to avoid confirmation message");
		}
		if (num2)
		{
			if (ImGui.GetIO().KeyShift && SelectedTrigger != null)
			{
				TriggerController.RemoveTrigger(SelectedTrigger);
				SelectedTrigger = null;
				Configuration.Save();
			}
			triggersViewMode = "delete";
		}
		ImGui.SameLine();
		if (Premium != null && Premium.IsPremium())
		{
			if (ImGui.Button("Import Triggers") && !ConfigurationProfile.EXPORT_DIR.Equals(""))
			{
				try
				{
					string[] files = Directory.GetFiles(ConfigurationProfile.EXPORT_DIR);
					for (int index3 = 0; index3 < files.Length; index3++)
					{
						Trigger t = JsonConvert.DeserializeObject<Trigger>(File.ReadAllText(files[index3]));
						TriggerController.RemoveTrigger(t);
						TriggerController.AddTrigger(t);
					}
				}
				catch
				{
				}
			}
			ImGui.SameLine();
			if (!ImGui.Button("Export All") || ConfigurationProfile.EXPORT_DIR.Equals(""))
			{
				return;
			}
			{
				foreach (Trigger t in TriggerController.GetTriggers())
				{
					export_trigger(t);
				}
				return;
			}
		}
		ImGui.TextColored(ImGuiColors.DalamudGrey2, "Import/Export is a " + PremiumFeatureText.ToLower());
	}

	public void DrawPatternsTab()
	{
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Add or edit a new pattern:");
		ImGui.Indent(20f);
		List<Pattern> customPatterns = Patterns.GetCustomPatterns();
		if (ImGui.BeginTable("###PATTERN_ADD_FORM", 3))
		{
			ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL1", ImGuiTableColumnFlags.WidthFixed, 100f);
			ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL2", ImGuiTableColumnFlags.WidthFixed, 300f);
			ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL3", ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableNextColumn();
			ImGui.Text("Pattern Name:");
			ImGui.TableNextColumn();
			ImGui.SetNextItemWidth(300f);
			if (ImGui.InputText("###PATTERNS_CURRENT_PATTERN_NAME_TO_ADD", ref _tmp_currentPatternNameToAdd, 150u))
			{
				_tmp_currentPatternNameToAdd = _tmp_currentPatternNameToAdd.Trim();
			}
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text("Pattern Value:");
			ImGui.TableNextColumn();
			ImGui.SetNextItemWidth(300f);
			if (ImGui.InputText("###PATTERNS_CURRENT_PATTERN_VALUE_TO_ADD", ref _tmp_currentPatternValueToAdd, 500u))
			{
				_tmp_currentPatternValueToAdd = _tmp_currentPatternValueToAdd.Trim();
				if (_tmp_currentPatternValueToAdd.Trim() == "")
				{
					_tmp_currentPatternValueState = "unset";
				}
				else
				{
					_tmp_currentPatternValueState = (Helpers.RegExpMatch(Logger, _tmp_currentPatternValueToAdd, VALID_REGEXP_PATTERN) ? "valid" : "unvalid");
				}
			}
			ImGui.TableNextColumn();
			ImGuiComponents.HelpMarker("Example: 50:1000|100:2000 means 50% for 1000 milliseconds followed by 100% for 2000 milliseconds.");
			if (_tmp_currentPatternNameToAdd.Trim() != "" && _tmp_currentPatternValueState == "valid")
			{
				ImGui.TableNextColumn();
				if (ImGui.Button("Save"))
				{
					Pattern newPattern = new Pattern(_tmp_currentPatternNameToAdd, _tmp_currentPatternValueToAdd);
					Patterns.AddCustomPattern(newPattern);
					ConfigurationProfile.PatternList = Patterns.GetCustomPatterns();
					Configuration.Save();
					_tmp_currentPatternNameToAdd = "";
					_tmp_currentPatternValueToAdd = "";
					_tmp_currentPatternValueState = "unset";
				}
			}
			ImGui.TableNextRow();
			if (_tmp_currentPatternValueState == "unvalid")
			{
				ImGui.TableNextColumn();
				ImGui.TextColored(ImGuiColors.DalamudRed, "WRONG FORMAT!");
				ImGui.TableNextColumn();
				ImGui.TextColored(ImGuiColors.DalamudRed, "Format: <int>:<ms>|<int>:<ms>...");
				ImGui.TableNextColumn();
				ImGui.TextColored(ImGuiColors.DalamudRed, "Eg: 10:500|100:1000|20:500|0:0");
			}
			ImGui.EndTable();
		}
		ImGui.Indent(-20f);
		ImGui.Separator();
		if (customPatterns.Count == 0)
		{
			ImGui.Text("No custom patterns, please add some");
			return;
		}
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Custom Patterns:");
		ImGui.Indent(20f);
		if (ImGui.BeginTable("###PATTERN_CUSTOM_LIST", 3))
		{
			ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL1", ImGuiTableColumnFlags.WidthFixed, 100f);
			ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL2", ImGuiTableColumnFlags.WidthFixed, 430f);
			ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL3", ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableNextColumn();
			ImGui.TextColored(ImGuiColors.DalamudGrey2, "Search name:");
			ImGui.TableNextColumn();
			ImGui.SetNextItemWidth(150f);
			ImGui.InputText("###PATTERN_SEARCH_BAR", ref CURRENT_PATTERN_SEARCHBAR, 200u);
			ImGui.TableNextRow();
			for (int patternIndex = 0; patternIndex < customPatterns.Count; patternIndex++)
			{
				Pattern pattern = customPatterns[patternIndex];
				if (!Helpers.RegExpMatch(Logger, pattern.Name, CURRENT_PATTERN_SEARCHBAR))
				{
					continue;
				}
				ImGui.TableNextColumn();
				ImGui.Text(pattern.Name ?? "");
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(pattern.Name ?? "");
				}
				ImGui.TableNextColumn();
				string valueShort = pattern.Value;
				if (valueShort.Length > 70)
				{
					valueShort = valueShort.Substring(0, 70) + "...";
				}
				ImGui.Text(valueShort);
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(pattern.Value ?? "");
				}
				ImGui.TableNextColumn();
				if (ImGuiComponents.IconButton(patternIndex, FontAwesomeIcon.Trash))
				{
					if (!Patterns.RemoveCustomPattern(pattern))
					{
						Logger.Error("Could not remove pattern " + pattern.Name);
					}
					else
					{
						List<Pattern> newPatternList = Patterns.GetCustomPatterns();
						ConfigurationProfile.PatternList = newPatternList;
						Configuration.Save();
					}
				}
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(patternIndex, FontAwesomeIcon.Pen))
				{
					_tmp_currentPatternNameToAdd = pattern.Name;
					_tmp_currentPatternValueToAdd = pattern.Value;
					_tmp_currentPatternValueState = "valid";
				}
				ImGui.TableNextRow();
			}
			ImGui.EndTable();
		}
		ImGui.Indent(-20f);
	}

	public void DrawHelpTab()
	{
		ImGui.TextWrapped(Main.GetHelp(app.CommandName));
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Plugin information");
		ImGui.Text($"App version: {Assembly.GetExecutingAssembly().GetName().Version}");
		ImGui.Text($"Config version: {Configuration.Version}");
		ImGui.TextColored(ImGuiColors.DalamudViolet, "Pattern information");
		ImGui.TextWrapped("You should use a string separated by the | (pipe) symbol with a pair of <Intensity> and <Duration in milliseconds>.");
		ImGui.TextWrapped("Below is an example of a pattern that would vibe 1sec at 50pct intensity and 2sec at 100pct:");
		ImGui.TextWrapped("Pattern example:");
		_tmp_void = "50:1000|100:2000";
		ImGui.InputText("###HELP_PATTERN_EXAMPLE", ref _tmp_void, 50u);
	}

	public string export_trigger(Trigger trigger)
	{
		if (ConfigurationProfile.EXPORT_DIR.Equals(""))
		{
			return "No export directory has been set! Set one in Options.";
		}
		try
		{
			File.WriteAllText(Path.Join(ConfigurationProfile.EXPORT_DIR, trigger.Name + ".json"), JsonConvert.SerializeObject(trigger, Formatting.Indented));
			return "Successfully exported trigger!";
		}
		catch
		{
			return "Something went wrong while exporting!";
		}
	}

	static PluginUI()
	{
		MinSize = new Vector2(700f, 600f);
	}
}
