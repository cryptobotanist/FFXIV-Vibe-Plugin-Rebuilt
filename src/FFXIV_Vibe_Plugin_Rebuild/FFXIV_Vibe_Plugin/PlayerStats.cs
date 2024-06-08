using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin;

public class PlayerStats
{
	private readonly Logger Logger;

	private IClientState _clientState;

	private float _CurrentHp;

	private float _prevCurrentHp = -1f;

	private float _MaxHp;

	private float _prevMaxHp = -1f;

	public string PlayerName = "*unknown*";

	public event EventHandler? Event_CurrentHpChanged;

	public event EventHandler? Event_MaxHpChanged;

	public PlayerStats(Logger logger, IClientState clientState)
	{
		Logger = logger;
		UpdatePlayerState(clientState);
		_clientState = clientState;
	}

	public void Update(IClientState clientState)
	{
		if (clientState != null && !(clientState.LocalPlayer == null))
		{
			UpdatePlayerState(clientState);
			UpdatePlayerName(clientState);
			UpdateCurrentHp(clientState);
		}
	}

	public void UpdatePlayerState(IClientState clientState)
	{
		if (clientState != null && clientState.LocalPlayer != null && (_CurrentHp == -1f || _MaxHp == -1f))
		{
			Logger.Debug($"UpdatePlayerState {_CurrentHp} {_MaxHp}");
			_CurrentHp = (_prevCurrentHp = clientState.LocalPlayer.CurrentHp);
			_MaxHp = (_prevMaxHp = clientState.LocalPlayer.MaxHp);
			Logger.Debug($"UpdatePlayerState {_CurrentHp} {_MaxHp}");
		}
	}

	public string UpdatePlayerName(IClientState clientState)
	{
		if (clientState != null && clientState.LocalPlayer != null)
		{
			PlayerName = clientState.LocalPlayer.Name.TextValue;
		}
		return PlayerName;
	}

	public string GetPlayerName()
	{
		return PlayerName;
	}

	private void UpdateCurrentHp(IClientState clientState)
	{
		if (clientState != null && clientState.LocalPlayer != null)
		{
			_CurrentHp = clientState.LocalPlayer.CurrentHp;
			_MaxHp = clientState.LocalPlayer.MaxHp;
		}
		if (_CurrentHp != _prevCurrentHp)
		{
			this.Event_CurrentHpChanged?.Invoke(this, EventArgs.Empty);
		}
		if (_MaxHp != _prevMaxHp)
		{
			this.Event_MaxHpChanged?.Invoke(this, EventArgs.Empty);
		}
		_prevCurrentHp = _CurrentHp;
		_prevMaxHp = _MaxHp;
	}

	public float GetCurrentHP()
	{
		return _CurrentHp;
	}

	public float GetMaxHP()
	{
		return _MaxHp;
	}

	public bool IsInCombat()
	{
		if (_clientState != null && _clientState.LocalPlayer != null)
		{
			return _clientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.InCombat);
		}
		return false;
	}
}
