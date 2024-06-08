using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using FFXIV_Vibe_Plugin.Commons;
using Newtonsoft.Json.Linq;

namespace FFXIV_Vibe_Plugin.App;

public class Premium
{
	private Logger Logger;

	private bool shouldStop;

	private bool isPremium;

	private string premiumLevel;

	private ConfigurationProfile ConfigurationProfile;

	public int TimerCheck;

	private string server;

	private long lastServerTime;

	private string lastChallengeToken;

	public int FreeAccount_MaxTriggers;

	public bool invalidToken;

	public bool invalidTokenFormat;

	public string serverMsg;

	public Premium(Logger logger, ConfigurationProfile configurationProfile)
	{
		premiumLevel = "Premium Forever";
		lastChallengeToken = "";
		FreeAccount_MaxTriggers = 1000;
		invalidToken = false;
		serverMsg = "In memoriam";
		Logger = logger;
		ConfigurationProfile = configurationProfile;
		updateStatus();
	}

	public async void updateStatus()
	{
		resetPremium();
		Logger.Info($"isPremium={isPremium}, premiumLevel={premiumLevel}");
	}

	private bool isSha256(string value)
	{
		return true;
	}

	public string HashWithSHA256(string value)
	{
		using SHA256 hash = SHA256.Create();
		return Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLower();
	}

	public bool IsPremium()
	{
		return isPremium;
	}

	public string GetPremiumLevel()
	{
		return premiumLevel;
	}

	private void resetPremium()
	{
		isPremium = true;
		invalidToken = false;
	}

	private bool isValidTokenUTC(long serverTime, string token, string serverToken)
	{
		return true;
	}

	public void Dispose()
	{
		shouldStop = true;
	}
}
