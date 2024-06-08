using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.Commons;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace FFXIV_Vibe_Plugin.Hooks;

internal class ActionEffect
{
	private delegate void HOOK_ReceiveActionEffectDelegate(int sourceId, nint sourceCharacter, nint pos, nint effectHeader, nint effectArray, nint effectTrail);

	private readonly IDataManager? DataManager;

	private readonly Logger Logger;

	private readonly SigScanner Scanner;

	private readonly IClientState ClientState;

	private readonly IObjectTable GameObjects;

	private readonly IGameInteropProvider InteropProvider;

	private readonly ExcelSheet<Lumina.Excel.GeneratedSheets.Action>? LuminaActionSheet;

	private Hook<HOOK_ReceiveActionEffectDelegate> receiveActionEffectHook;

	public event EventHandler<HookActionEffects_ReceivedEventArgs>? ReceivedEvent;

	public ActionEffect(IDataManager dataManager, Logger logger, SigScanner scanner, IClientState clientState, IObjectTable gameObjects, IGameInteropProvider interopProvider)
	{
		DataManager = dataManager;
		Logger = logger;
		Scanner = scanner;
		ClientState = clientState;
		GameObjects = gameObjects;
		InteropProvider = interopProvider;
		InitHook();
		if (DataManager != null)
		{
			LuminaActionSheet = DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>();
		}
	}

	public void Dispose()
	{
		receiveActionEffectHook?.Disable();
		receiveActionEffectHook?.Dispose();
	}

	private void InitHook()
	{
		try
		{
			nint receiveActionEffectFuncPtr = Scanner.ScanText("40 55 53 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70");
			receiveActionEffectHook = InteropProvider.HookFromAddress<HOOK_ReceiveActionEffectDelegate>(receiveActionEffectFuncPtr, ReceiveActionEffect);
			receiveActionEffectHook.Enable();
		}
		catch (Exception e)
		{
			Dispose();
			Logger.Warn("Encountered an error loading HookActionEffect: " + e.Message + ". Disabling it...");
			throw;
		}
		Logger.Log("HookActionEffect was correctly enabled!");
	}

	private unsafe void ReceiveActionEffect(int sourceId, nint sourceCharacter, nint pos, nint effectHeader, nint effectArray, nint effectTrail)
	{
		Structures.Spell spell = default(Structures.Spell);
		try
		{
			uint ptr_id = *(uint*)((byte*)((IntPtr)effectHeader).ToPointer() + (nint)2 * (nint)4);
			_ = *(ushort*)((byte*)((IntPtr)effectHeader).ToPointer() + (nint)14 * (nint)2);
			_ = *(ushort*)((byte*)((IntPtr)effectHeader).ToPointer() - (nint)7 * (nint)2);
			byte ptr_targetCount = *(byte*)(effectHeader + 33);
			Structures.EffectEntry effect = *(Structures.EffectEntry*)effectArray;
			string playerName = GetCharacterNameFromSourceId(sourceId);
			string spellName = GetSpellName(ptr_id, withId: true);
			int[] amounts = GetAmounts(ptr_targetCount, effectArray);
			float amountAverage = ComputeAverageAmount(amounts);
			List<Structures.Player> targets = GetAllTarget(ptr_targetCount, effectTrail, amounts);
			spell.Id = (int)ptr_id;
			spell.Name = spellName;
			spell.Player = new Structures.Player(sourceId, playerName);
			spell.Amounts = amounts;
			spell.AmountAverage = amountAverage;
			spell.Targets = targets;
			spell.DamageType = Structures.DamageType.Unknown;
			if (targets.Count == 0)
			{
				spell.ActionEffectType = Structures.ActionEffectType.Any;
			}
			else
			{
				spell.ActionEffectType = effect.type;
			}
			DispatchReceivedEvent(spell);
		}
		catch (Exception e)
		{
			Logger.Log(e.Message + " " + e.StackTrace);
		}
		RestoreOriginalHook(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
	}

	private void RestoreOriginalHook(int sourceId, nint sourceCharacter, nint pos, nint effectHeader, nint effectArray, nint effectTrail)
	{
		if (receiveActionEffectHook != null)
		{
			receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
		}
	}

	private unsafe int[] GetAmounts(byte count, nint effectArray)
	{
		int[] RESULT = new int[count];
		int effectsEntries = 0;
		if (count == 0)
		{
			effectsEntries = 0;
		}
		else if (count == 1)
		{
			effectsEntries = 8;
		}
		else if (count <= 8)
		{
			effectsEntries = 64;
		}
		else if (count <= 16)
		{
			effectsEntries = 128;
		}
		else if (count <= 24)
		{
			effectsEntries = 192;
		}
		else if (count <= 32)
		{
			effectsEntries = 256;
		}
		List<Structures.EffectEntry> entries = new List<Structures.EffectEntry>(effectsEntries);
		for (int i = 0; i < effectsEntries; i++)
		{
			entries.Add(*(Structures.EffectEntry*)(effectArray + i * 8));
		}
		int counterValueFound = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			if (i % 8 == 0)
			{
				uint tDmg = entries[i].value;
				if (entries[i].mult != 0)
				{
					tDmg += (uint)(65536 * entries[i].mult);
				}
				if (counterValueFound < count)
				{
					RESULT[counterValueFound] = (int)tDmg;
				}
				counterValueFound++;
			}
		}
		return RESULT;
	}

	private static int ComputeAverageAmount(int[] amounts)
	{
		int result = 0;
		for (int i = 0; i < amounts.Length; i++)
		{
			result += amounts[i];
		}
		return (result != 0) ? (result / amounts.Length) : result;
	}

	private unsafe List<Structures.Player> GetAllTarget(byte count, nint effectTrail, int[] amounts)
	{
		List<Structures.Player> names = new List<Structures.Player>();
		if (count >= 1)
		{
			ulong[] targets = new ulong[count];
			for (int i = 0; i < count; i++)
			{
				targets[i] = *(ulong*)(effectTrail + i * 8);
				int targetId = (int)targets[i];
				string targetName = GetCharacterNameFromSourceId(targetId);
				Structures.Player targetPlayer = new Structures.Player(targetId, targetName, $"{amounts[i]}");
				names.Add(targetPlayer);
			}
		}
		return names;
	}

	private string GetSpellName(uint actionId, bool withId)
	{
		if (LuminaActionSheet == null)
		{
			Logger.Warn("HookActionEffect.GetSpellName: LuminaActionSheet is null");
			return "***LUMINA ACTION SHEET NOT LOADED***";
		}
		Lumina.Excel.GeneratedSheets.Action row = LuminaActionSheet.GetRow(actionId);
		string spellName = "";
		if (row != null)
		{
			if (withId)
			{
				spellName = $"{row.RowId}:";
			}
			if (row.Name != null)
			{
				spellName += $"{row.Name}";
			}
		}
		else
		{
			spellName = "!Unknown Spell Name!";
		}
		return spellName;
	}

	private string GetCharacterNameFromSourceId(int sourceId)
	{
		GameObject character = GameObjects.SearchById((uint)sourceId);
		string characterName = "";
		if (character != null)
		{
			characterName = character.Name.TextValue;
		}
		return characterName;
	}

	protected virtual void DispatchReceivedEvent(Structures.Spell spell)
	{
		HookActionEffects_ReceivedEventArgs args = new HookActionEffects_ReceivedEventArgs();
		args.Spell = spell;
		this.ReceivedEvent?.Invoke(this, args);
	}
}
