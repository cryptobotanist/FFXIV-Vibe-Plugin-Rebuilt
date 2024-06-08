using System;
using Dalamud.Logging;
using Dalamud.Plugin.Services;

namespace FFXIV_Vibe_Plugin.Commons;

public class Logger
{
	public enum LogLevel
	{
		VERBOSE,
		DEBUG,
		LOG,
		INFO,
		WARN,
		ERROR,
		FATAL
	}

	private readonly IChatGui? DalamudChatGui;

	private readonly string name = "";

	private readonly LogLevel log_level = LogLevel.DEBUG;

	private readonly string prefix = ">";

	public Logger(IChatGui? DalamudChatGui, string name, LogLevel log_level)
	{
		this.DalamudChatGui = DalamudChatGui;
		this.name = name;
		this.log_level = log_level;
	}

	public void Chat(string msg)
	{
		if (DalamudChatGui != null)
		{
			string m = FormatMessage(LogLevel.LOG, msg);
			DalamudChatGui.Print(m);
		}
		else
		{
			PluginLog.LogError("No gui chat");
		}
	}

	public void ChatError(string msg)
	{
		string m = FormatMessage(LogLevel.ERROR, msg);
		DalamudChatGui?.PrintError(m);
		Error(msg);
	}

	public void ChatError(string msg, Exception e)
	{
		string m = FormatMessage(LogLevel.ERROR, msg, e);
		DalamudChatGui?.PrintError(m);
		Error(m);
	}

	public void Verbose(string msg)
	{
		if (log_level <= LogLevel.VERBOSE)
		{
			PluginLog.LogVerbose(FormatMessage(LogLevel.VERBOSE, msg));
		}
	}

	public void Debug(string msg)
	{
		if (log_level <= LogLevel.DEBUG)
		{
			PluginLog.LogDebug(FormatMessage(LogLevel.DEBUG, msg));
		}
	}

	public void Log(string msg)
	{
		if (log_level <= LogLevel.LOG)
		{
			PluginLog.Log(FormatMessage(LogLevel.LOG, msg));
		}
	}

	public void Info(string msg)
	{
		if (log_level <= LogLevel.INFO)
		{
			PluginLog.Information(FormatMessage(LogLevel.INFO, msg));
		}
	}

	public void Warn(string msg)
	{
		if (log_level <= LogLevel.WARN)
		{
			PluginLog.Warning(FormatMessage(LogLevel.WARN, msg));
		}
	}

	public void Error(string msg)
	{
		if (log_level <= LogLevel.ERROR)
		{
			PluginLog.Error(FormatMessage(LogLevel.ERROR, msg));
		}
	}

	public void Error(string msg, Exception e)
	{
		if (log_level <= LogLevel.ERROR)
		{
			PluginLog.Error(FormatMessage(LogLevel.ERROR, msg, e));
		}
	}

	public void Fatal(string msg)
	{
		if (log_level <= LogLevel.FATAL)
		{
			PluginLog.Fatal(FormatMessage(LogLevel.FATAL, msg));
		}
	}

	public void Fatal(string msg, Exception e)
	{
		if (log_level <= LogLevel.FATAL)
		{
			PluginLog.Fatal(FormatMessage(LogLevel.FATAL, msg, e));
		}
	}

	private string FormatMessage(LogLevel type, string msg)
	{
		return $"{((name != "") ? (name + " ") : "")}{type} {prefix} {msg}";
	}

	private string FormatMessage(LogLevel type, string msg, Exception e)
	{
		return $"{((name != "") ? (name + " ") : "")}{type} {prefix} {e.Message}\\n{msg}";
	}
}
