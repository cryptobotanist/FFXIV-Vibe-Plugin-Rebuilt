using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin.Triggers;

public class TriggersController
{
	private readonly Logger Logger;

	private readonly PlayerStats PlayerStats;

	private ConfigurationProfile Profile;

	private List<Trigger> Triggers = new List<Trigger>();

	public TriggersController(Logger logger, PlayerStats playerStats, ConfigurationProfile profile)
	{
		Logger = logger;
		PlayerStats = playerStats;
		Profile = profile;
	}

	public void SetProfile(ConfigurationProfile profile)
	{
		Profile = profile;
		Triggers = profile.TRIGGERS;
	}

	public List<Trigger> GetTriggers()
	{
		return Triggers;
	}

	public void AddTrigger(Trigger trigger)
	{
		Triggers.Add(trigger);
	}

	public void RemoveTrigger(Trigger trigger)
	{
		Triggers.Remove(trigger);
	}

	public List<Trigger> CheckTrigger_Chat(XivChatType chatType, string ChatFromPlayerName, string ChatMsg)
	{
		List<Trigger> triggers = new List<Trigger>();
		ChatFromPlayerName = ChatFromPlayerName.Trim().ToLower();
		for (int triggerIndex = 0; triggerIndex < Triggers.Count; triggerIndex++)
		{
			Trigger trigger = Triggers[triggerIndex];
			if (trigger.Enabled && (chatType == XivChatType.Echo || (Helpers.RegExpMatch(Logger, ChatFromPlayerName, trigger.FromPlayerName) && (trigger.AllowedChatTypes.Count <= 0 || trigger.AllowedChatTypes.Any((int ct) => ct == (int)chatType)))) && (!trigger.OnlyInCombat || PlayerStats.IsInCombat()) && trigger.Kind == 0 && Helpers.RegExpMatch(Logger, ChatMsg, trigger.ChatText))
			{
				if (Profile.VERBOSE_CHAT)
				{
					Logger.Debug($"ChatTrigger matched {trigger.ChatText}<>{ChatMsg}, adding {trigger}");
				}
				triggers.Add(trigger);
			}
		}
		return triggers;
	}

	public List<Trigger> CheckTrigger_Spell(Structures.Spell spell)
	{
		List<Trigger> triggers = new List<Trigger>();
		string spellName = ((spell.Name != null) ? spell.Name.Trim() : "");
		for (int triggerIndex = 0; triggerIndex < Triggers.Count; triggerIndex++)
		{
			Trigger trigger = Triggers[triggerIndex];
			if (!trigger.Enabled || !Helpers.RegExpMatch(Logger, spell.Player.Name, trigger.FromPlayerName) || trigger.Kind != 1 || !Helpers.RegExpMatch(Logger, spellName, trigger.SpellText) || (trigger.ActionEffectType != 0 && trigger.ActionEffectType != (int)spell.ActionEffectType) || ((trigger.ActionEffectType == 3 || trigger.ActionEffectType == 4) && ((float)trigger.AmountMinValue >= spell.AmountAverage || (float)trigger.AmountMaxValue <= spell.AmountAverage)))
			{
				continue;
			}
			DIRECTION direction = GetSpellDirection(spell);
			if ((trigger.Direction == 0 || direction == (DIRECTION)trigger.Direction) && (!trigger.OnlyInCombat || PlayerStats.IsInCombat()))
			{
				if (Profile.VERBOSE_SPELL)
				{
					Logger.Debug($"SpellTrigger matched {spell}, adding {trigger}");
				}
				triggers.Add(trigger);
			}
		}
		return triggers;
	}

	public List<Trigger> CheckTrigger_HPChanged(int currentHP, float percentageHP)
	{
		List<Trigger> triggers = new List<Trigger>();
		for (int triggerIndex = 0; triggerIndex < Triggers.Count; triggerIndex++)
		{
			Trigger trigger = Triggers[triggerIndex];
			if (!trigger.Enabled || trigger.Kind != 2)
			{
				continue;
			}
			if (trigger.AmountInPercentage)
			{
				if (percentageHP < (float)trigger.AmountMinValue || percentageHP > (float)trigger.AmountMaxValue)
				{
					continue;
				}
			}
			else if (trigger.AmountMinValue >= currentHP || trigger.AmountMaxValue <= currentHP)
			{
				continue;
			}
			if (!trigger.OnlyInCombat || PlayerStats.IsInCombat())
			{
				if (trigger.AmountInPercentage)
				{
					Logger.Debug($"HPChanged Triggers (in percentage): {percentageHP}%, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");
				}
				else
				{
					Logger.Debug($"HPChanged Triggers: {currentHP}, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");
				}
				triggers.Add(trigger);
			}
		}
		return triggers;
	}

	public List<Trigger> CheckTrigger_HPChangedOther(IPartyList partyList)
	{
		List<Trigger> triggers = new List<Trigger>();
		if (partyList == null)
		{
			return triggers;
		}
		for (int triggerIndex = 0; triggerIndex < Triggers.Count; triggerIndex++)
		{
			Trigger trigger = Triggers[triggerIndex];
			if (!trigger.Enabled || trigger.Kind != 3)
			{
				continue;
			}
			int partyLength = partyList.Length;
			for (int i = 0; i < partyLength; i++)
			{
				PartyMember partyMember = partyList[i];
				if (partyMember == null)
				{
					continue;
				}
				string name = partyMember.Name.ToString();
				if (!Helpers.RegExpMatch(Logger, name, trigger.FromPlayerName))
				{
					continue;
				}
				uint maxHP = partyMember.MaxHP;
				uint currentHP = partyMember.CurrentHP;
				if (maxHP == 0)
				{
					continue;
				}
				uint percentageHP = currentHP * 100 / maxHP;
				if (trigger.AmountInPercentage)
				{
					if (percentageHP < trigger.AmountMinValue || percentageHP > trigger.AmountMaxValue)
					{
						continue;
					}
				}
				else if (trigger.AmountMinValue >= currentHP || trigger.AmountMaxValue <= currentHP)
				{
					continue;
				}
				if (!trigger.OnlyInCombat || PlayerStats.IsInCombat())
				{
					if (trigger.AmountInPercentage)
					{
						Logger.Debug($"HPChangedOther for {name} Triggers (in percentage): {percentageHP}%, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");
					}
					else
					{
						Logger.Debug($"HPChangedOther for {name} Triggers: {currentHP}, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");
					}
					triggers.Add(trigger);
				}
			}
		}
		return triggers;
	}

	public DIRECTION GetSpellDirection(Structures.Spell spell)
	{
		string myName = PlayerStats.GetPlayerName();
		List<Structures.Player> targets = new List<Structures.Player>();
		if (spell.Targets != null)
		{
			targets = spell.Targets;
		}
		if (targets.Count >= 1 && targets[0].Name != myName)
		{
			return DIRECTION.Outgoing;
		}
		if (spell.Player.Name != myName)
		{
			return DIRECTION.Incoming;
		}
		return DIRECTION.Self;
	}
}
