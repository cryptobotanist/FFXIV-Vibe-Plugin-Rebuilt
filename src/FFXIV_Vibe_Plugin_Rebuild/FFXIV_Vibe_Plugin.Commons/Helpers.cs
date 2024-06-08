using System;
using System.Text.RegularExpressions;

namespace FFXIV_Vibe_Plugin.Commons;

internal class Helpers
{
	public static int GetUnix()
	{
		return (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
	}

	public static int ClampInt(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static float ClampFloat(float value, float min, float max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static int ClampIntensity(int intensity, int threshold)
	{
		intensity = ClampInt(intensity, 0, 100);
		return (int)((float)intensity / (100f / (float)threshold));
	}

	public static bool RegExpMatch(Logger Logger, string text, string regexp)
	{
		bool found = false;
		if (regexp.Trim() == "")
		{
			found = true;
		}
		else
		{
			string patternCheck = "" + regexp;
			try
			{
				if (Regex.Match(text, patternCheck, RegexOptions.IgnoreCase).Success)
				{
					found = true;
				}
			}
			catch (Exception)
			{
				Logger.Error("Probably a wrong REGEXP for " + regexp);
			}
		}
		return found;
	}
}
