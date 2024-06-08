using System;
using Dalamud.Game.Network;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin.Experimental;

internal class NetworkCapture
{
	private readonly Logger Logger;

	private readonly IGameNetwork? GameNetwork;

	private bool ExperimentalNetworkCaptureStarted;

	public NetworkCapture(Logger logger, IGameNetwork gameNetwork)
	{
		Logger = logger;
		GameNetwork = gameNetwork;
	}

	public void Dispose()
	{
		StopNetworkCapture();
	}

	public void StartNetworkCapture()
	{
	}

	public void StopNetworkCapture()
	{
		if (ExperimentalNetworkCaptureStarted)
		{
			Logger.Debug("STOPPING EXPERIMENTAL");
			if (GameNetwork != null)
			{
				GameNetwork.NetworkMessage -= OnNetworkReceived;
			}
			ExperimentalNetworkCaptureStarted = false;
		}
	}

	private unsafe void OnNetworkReceived(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
	{
		int vOut = Convert.ToInt32(opCode);
		string name = OpCodes.GetName(opCode);
		uint actionId = 111111111u;
		if (direction == NetworkMessageDirection.ZoneUp)
		{
			actionId = *(uint*)(dataPtr + 4);
		}
		Logger.Log($"Hex: {vOut:X} Decimal: {opCode} ActionId: {actionId} SOURCE_ID: {sourceActorId} TARGET_ID: {targetActorId} DIRECTION: {direction} DATA_PTR: {dataPtr} NAME: {name}");
		if (name == "ClientZoneIpcType-ClientTrigger")
		{
			ushort commandId = *(ushort*)dataPtr;
			byte unk_1 = *(byte*)(dataPtr + 2);
			byte unk_2 = *(byte*)(dataPtr + 3);
			uint param11 = *(uint*)(dataPtr + 4);
			uint param12 = *(uint*)(dataPtr + 8);
			uint param2 = *(uint*)(dataPtr + 12);
			uint param4 = *(uint*)(dataPtr + 16);
			uint param5 = *(uint*)(dataPtr + 20);
			ulong param3 = *(ulong*)(dataPtr + 24);
			string extra = "";
			switch (param11)
			{
			case 0u:
				extra += "WeaponIn";
				break;
			case 1u:
				extra += "WeaponOut";
				break;
			}
			Logger.Log($"{name} {direction} {extra} {commandId} {unk_1} {unk_2} {param11} {param12} {param2} {param2} {param4} {param5} {param3}");
		}
	}
}
