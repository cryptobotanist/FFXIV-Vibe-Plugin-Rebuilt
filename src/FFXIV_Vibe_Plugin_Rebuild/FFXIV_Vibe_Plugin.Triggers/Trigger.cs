using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FFXIV_Vibe_Plugin.Triggers;

public class Trigger : IComparable<Trigger>, IEquatable<Trigger>
{
	private static readonly int _initAmountMinValue = -1;

	private static readonly int _initAmountMaxValue = 10000000;

	public bool Enabled = true;

	public int SortOder = -1;

	public readonly string Id = "";

	public string Name = "";

	public string Description = "";

	public int Kind;

	public int ActionEffectType;

	public bool OnlyInCombat;

	public int Direction;

	public string ChatText = "hello world";

	public string SpellText = "";

	public int AmountMinValue = _initAmountMinValue;

	public int AmountMaxValue = _initAmountMaxValue;

	public bool AmountInPercentage;

	public string FromPlayerName = "";

	public float StartAfter;

	public float StopAfter;

	public int Priority;

	public readonly List<int> AllowedChatTypes = new List<int>();

	public List<TriggerDevice> Devices = new List<TriggerDevice>();

	public Trigger(string name)
	{
		Name = name;
		byte[] textBytes = Encoding.UTF8.GetBytes(name);
		byte[] hashed = SHA256.Create().ComputeHash(textBytes);
		Id = BitConverter.ToString(hashed).Replace("-", string.Empty);
	}

	public override string ToString()
	{
		return $"Trigger(name={Name}, id={GetShortID()})";
	}

	public int CompareTo(Trigger? other)
	{
		return other?.Name.CompareTo(Name) ?? 1;
	}

	public bool Equals(Trigger? other)
	{
		if (other == null)
		{
			return false;
		}
		return Name.Equals(other.Name);
	}

	public string GetShortID()
	{
		return Id.Substring(0, 5);
	}

	public void Reset()
	{
		AmountMaxValue = _initAmountMaxValue;
		AmountMinValue = _initAmountMinValue;
	}
}
