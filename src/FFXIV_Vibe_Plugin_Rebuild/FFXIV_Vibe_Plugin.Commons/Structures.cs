using System;
using System.Collections.Generic;

namespace FFXIV_Vibe_Plugin.Commons;

public class Structures
{
	public enum ActionEffectType : byte
	{
		Any = 0,
		Miss = 1,
		FullResist = 2,
		Damage = 3,
		Heal = 4,
		BlockedDamage = 5,
		ParriedDamage = 6,
		Invulnerable = 7,
		NoEffectText = 8,
		Unknown_0 = 9,
		MpLoss = 10,
		MpGain = 11,
		TpLoss = 12,
		TpGain = 13,
		GpGain = 14,
		ApplyStatusEffectTarget = 15,
		ApplyStatusEffectSource = 16,
		StatusNoEffect = 20,
		Taunt = 24,
		StartActionCombo = 27,
		ComboSucceed = 28,
		Knockback = 33,
		Mount = 40,
		MountJapaneseVersion = 240,
		VFX = 59,
		Transport = 60
	}

	public enum DamageType
	{
		Unknown = 0,
		Slashing = 1,
		Piercing = 2,
		Blunt = 3,
		Magic = 5,
		Darkness = 6,
		Physical = 7,
		LimitBreak = 8
	}

	public struct EffectEntry
	{
		public ActionEffectType type;

		public byte param0;

		public byte param1;

		public byte param2;

		public byte mult;

		public byte flags;

		public ushort value;

		public EffectEntry(ActionEffectType type, byte param0, byte param1, byte param2, byte mult, byte flags, ushort value)
		{
			this.type = ActionEffectType.Any;
			this.param0 = 0;
			this.param1 = 0;
			this.param2 = 0;
			this.mult = 0;
			this.flags = 0;
			this.value = 0;
			this.type = type;
			this.param0 = param0;
			this.param1 = param1;
			this.param2 = param2;
			this.mult = mult;
			this.flags = flags;
			this.value = value;
		}

		public override string ToString()
		{
			return $"type: {type}, p0: {param0}, p1: {param1}, p2: {param2}, mult: {mult}, flags: {flags} | {Convert.ToString(flags, 2)}, value: {value}";
		}
	}

	public struct Player
	{
		public int Id;

		public string Name;

		public string? Info;

		public Player(int id, string name, string? info = null)
		{
			Id = id;
			Name = name;
			Info = info;
		}

		public override string ToString()
		{
			if (Info == null)
			{
				return $"{Name}({Id})";
			}
			return $"{Name}({Id}) [info:{Info}]";
		}
	}

	public struct Spell
	{
		public int Id;

		public string Name;

		public Player Player;

		public int[]? Amounts;

		public float AmountAverage;

		public List<Player>? Targets;

		public DamageType DamageType;

		public ActionEffectType ActionEffectType;

		public Spell(int id, string name, Player player, int[]? amounts, float amountAverage, List<Player>? targets, DamageType damageType, ActionEffectType actionEffectType)
		{
			Name = "Undefined_Spell_Name";
			DamageType = DamageType.Unknown;
			ActionEffectType = ActionEffectType.Any;
			Id = id;
			Name = name;
			Player = player;
			Amounts = amounts;
			AmountAverage = amountAverage;
			Targets = targets;
			DamageType = damageType;
			ActionEffectType = actionEffectType;
		}

		public override string ToString()
		{
			string targetsString = "";
			if (Targets != null)
			{
				targetsString = ((Targets.Count <= 0) ? "*no target*" : string.Join(",", Targets));
			}
			return $"{Player} casts {Name}#{ActionEffectType} on: {targetsString}. Avg: {AmountAverage}";
		}
	}
}
