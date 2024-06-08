using System;

namespace FFXIV_Vibe_Plugin;

internal class OpCodes
{
	public enum ServerLobbyIpcType : ushort
	{
		LobbyError = 2,
		LobbyServiceAccountList = 12,
		LobbyCharList = 13,
		LobbyCharCreate = 14,
		LobbyEnterWorld = 15,
		LobbyServerList = 21,
		LobbyRetainerList = 23
	}

	public enum ClientLobbyIpcType : ushort
	{
		ReqCharList = 3,
		ReqEnterWorld = 4,
		ClientVersionInfo = 5,
		ReqCharDelete = 10,
		ReqCharCreate = 11
	}

	public enum ServerZoneIpcType : ushort
	{
		PlayerSetup = 139,
		UpdateHpMpTp = 662,
		PlayerStats = 909,
		ActorControl = 382,
		ActorControlSelf = 742,
		ActorControlTarget = 360,
		Playtime = 963,
		Examine = 283,
		MarketBoardSearchResult = 513,
		MarketBoardItemListingCount = 572,
		MarketBoardItemListingHistory = 402,
		MarketBoardItemListing = 803,
		MarketBoardPurchase = 157,
		ActorMove = 565,
		ResultDialog = 175,
		RetainerInformation = 297,
		NpcSpawn = 814,
		ItemMarketBoardInfo = 138,
		PlayerSpawn = 307,
		ContainerInfo = 238,
		ItemInfo = 371,
		UpdateClassInfo = 933,
		ActorCast = 264,
		CurrencyCrystalInfo = 600,
		InitZone = 708,
		EffectResult = 406,
		EventStart = 820,
		EventFinish = 440,
		SomeDirectorUnk4 = 356,
		UpdateInventorySlot = 694,
		DesynthResult = 725,
		InventoryActionAck = 252,
		InventoryTransaction = 143,
		InventoryTransactionFinish = 923,
		CFNotify = 791,
		PrepareZoning = 144,
		ActorSetPos = 409,
		PlaceFieldMarker = 893,
		PlaceFieldMarkerPreset = 463,
		ObjectSpawn = 793,
		Effect = 858,
		StatusEffectList = 709,
		ActorGauge = 643,
		FreeCompanyInfo = 796,
		FreeCompanyDialog = 878,
		AirshipTimers = 237,
		SubmarineTimers = 245,
		AirshipStatusList = 575,
		AirshipStatus = 483,
		AirshipExplorationResult = 180,
		SubmarineProgressionStatus = 779,
		SubmarineStatusList = 756,
		SubmarineExplorationResult = 387,
		EventPlay = 165,
		EventPlay4 = 558,
		EventPlay8 = 395,
		EventPlay16 = 500,
		EventPlay32 = 101,
		EventPlay64 = 936,
		EventPlay128 = 366,
		EventPlay255 = 870,
		WeatherChange = 509,
		Logout = 748
	}

	public enum ClientZoneIpcType : ushort
	{
		UpdatePositionHandler = 838,
		ClientTrigger = 940,
		ChatHandler = 460,
		SetSearchInfoHandler = 945,
		MarketBoardPurchaseHandler = 220,
		InventoryModifyHandler = 163,
		UpdatePositionInstance = 355
	}

	public enum ServerChatIpcType : ushort
	{

	}

	public enum ClientChatIpcType : ushort
	{

	}

	public static string? GetName(ushort opCode)
	{
		string name = "?Unknow?";
		if (Enum.IsDefined(typeof(ServerLobbyIpcType), opCode))
		{
			name = "ServerLobbyIpcType-" + Enum.GetName(typeof(ServerLobbyIpcType), opCode);
		}
		if (Enum.IsDefined(typeof(ClientLobbyIpcType), opCode))
		{
			name = "ClientLobbyIpcType-" + Enum.GetName(typeof(ClientLobbyIpcType), opCode);
		}
		if (Enum.IsDefined(typeof(ServerZoneIpcType), opCode))
		{
			name = "ServerZoneIpcType-" + Enum.GetName(typeof(ServerZoneIpcType), opCode);
		}
		if (Enum.IsDefined(typeof(ClientZoneIpcType), opCode))
		{
			name = "ClientZoneIpcType-" + Enum.GetName(typeof(ClientZoneIpcType), opCode);
		}
		if (Enum.IsDefined(typeof(ServerChatIpcType), opCode))
		{
			name = "ServerChatIpcType-" + Enum.GetName(typeof(ServerChatIpcType), opCode);
		}
		if (Enum.IsDefined(typeof(ClientChatIpcType), opCode))
		{
			name = "ClientChatIpcType-" + Enum.GetName(typeof(ClientChatIpcType), opCode);
		}
		return name;
	}
}
