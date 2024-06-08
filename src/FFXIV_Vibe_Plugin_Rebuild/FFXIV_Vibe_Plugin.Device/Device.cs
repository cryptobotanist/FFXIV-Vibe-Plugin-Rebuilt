using System;
using System.Collections.Generic;
using Buttplug.Client;
using Buttplug.Core.Messages;
using DebounceThrottle;
using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin.Device;

public class Device
{
	private readonly Logger Logger;

	private readonly ButtplugClientDevice? ButtplugClientDevice;

	public int Id = -1;

	public string Name = "UnsetDevice";

	public bool CanVibrate;

	public int VibrateMotors = -1;

	private List<GenericDeviceMessageAttributes> vibrateAttributes = new List<GenericDeviceMessageAttributes>();

	public bool CanRotate;

	public int RotateMotors = -1;

	private List<GenericDeviceMessageAttributes> rotateAttributes = new List<GenericDeviceMessageAttributes>();

	public bool CanLinear;

	public int LinearMotors = -1;

	private List<GenericDeviceMessageAttributes> linearAttribute = new List<GenericDeviceMessageAttributes>();

	public bool CanOscillate;

	public int OscillateMotors = -1;

	private List<GenericDeviceMessageAttributes> oscillateAttribute = new List<GenericDeviceMessageAttributes>();

	public bool CanBattery;

	public double BatteryLevel = -1.0;

	public bool CanStop = true;

	public bool IsConnected;

	public List<UsableCommand> UsableCommands = new List<UsableCommand>();

	public int[] CurrentVibrateIntensity = Array.Empty<int>();

	public int[] CurrentRotateIntensity = Array.Empty<int>();

	public int[] CurrentOscillateIntensity = Array.Empty<int>();

	public int[] CurrentLinearIntensity = Array.Empty<int>();

	public DebounceDispatcher VibrateDebouncer = new DebounceDispatcher(25);

	public DebounceDispatcher RotateDebouncer = new DebounceDispatcher(25);

	public DebounceDispatcher OscillateDebouncer = new DebounceDispatcher(25);

	public DebounceDispatcher LinearDebouncer = new DebounceDispatcher(25);

	public Device(ButtplugClientDevice buttplugClientDevice, Logger logger)
	{
		Logger = logger;
		if (buttplugClientDevice != null)
		{
			ButtplugClientDevice = buttplugClientDevice;
			Id = (int)buttplugClientDevice.Index;
			Name = buttplugClientDevice.Name;
			SetCommands();
			ResetMotors();
			UpdateBatteryLevel();
		}
	}

	public override string ToString()
	{
		List<string> commands = GetCommandsInfo();
		return $"Device: {Id}:{Name} (connected={IsConnected}, battery={GetBatteryPercentage()}, commands={string.Join(",", commands)})";
	}

	private void SetCommands()
	{
		if (ButtplugClientDevice == null)
		{
			Logger.Error($"Device {Id}:{Name} has ClientDevice to null!");
			return;
		}
		vibrateAttributes = ButtplugClientDevice.VibrateAttributes;
		if (vibrateAttributes.Count > 0)
		{
			CanVibrate = true;
			VibrateMotors = vibrateAttributes.Count;
			UsableCommands.Add(UsableCommand.Vibrate);
		}
		rotateAttributes = ButtplugClientDevice.RotateAttributes;
		if (rotateAttributes.Count > 0)
		{
			CanRotate = true;
			RotateMotors = rotateAttributes.Count;
			UsableCommands.Add(UsableCommand.Rotate);
		}
		linearAttribute = ButtplugClientDevice.LinearAttributes;
		if (linearAttribute.Count > 0)
		{
			CanLinear = true;
			LinearMotors = linearAttribute.Count;
			UsableCommands.Add(UsableCommand.Linear);
		}
		oscillateAttribute = ButtplugClientDevice.OscillateAttributes;
		if (oscillateAttribute.Count > 0)
		{
			CanOscillate = true;
			OscillateMotors = oscillateAttribute.Count;
			UsableCommands.Add(UsableCommand.Oscillate);
		}
		if (ButtplugClientDevice.HasBattery)
		{
			CanBattery = true;
			UpdateBatteryLevel();
		}
	}

	private void ResetMotors()
	{
		if (CanVibrate)
		{
			CurrentVibrateIntensity = new int[VibrateMotors];
			for (int i = 0; i < VibrateMotors; i++)
			{
				CurrentVibrateIntensity[i] = 0;
			}
		}
		if (CanRotate)
		{
			CurrentRotateIntensity = new int[RotateMotors];
			for (int i = 0; i < RotateMotors; i++)
			{
				CurrentRotateIntensity[i] = 0;
			}
		}
		if (CanOscillate)
		{
			CurrentOscillateIntensity = new int[OscillateMotors];
			for (int i = 0; i < OscillateMotors; i++)
			{
				CurrentOscillateIntensity[i] = 0;
			}
		}
		if (CanLinear)
		{
			CurrentLinearIntensity = new int[LinearMotors];
			for (int i = 0; i < LinearMotors; i++)
			{
				CurrentLinearIntensity[i] = 0;
			}
		}
	}

	public List<UsableCommand> GetUsableCommands()
	{
		return UsableCommands;
	}

	public List<string> GetCommandsInfo()
	{
		List<string> commands = new List<string>();
		if (CanVibrate)
		{
			commands.Add($"vibrate motors={VibrateMotors}");
		}
		if (CanRotate)
		{
			commands.Add($"rotate motors={RotateMotors} ");
		}
		if (CanLinear)
		{
			commands.Add($"rotate motors={LinearMotors}");
		}
		if (CanOscillate)
		{
			commands.Add($"oscillate motors={OscillateMotors}");
		}
		if (CanBattery)
		{
			commands.Add("battery");
		}
		if (CanStop)
		{
			commands.Add("stop");
		}
		return commands;
	}

	public async void UpdateBatteryLevel()
	{
		if (ButtplugClientDevice == null || !CanBattery)
		{
			return;
		}
		try
		{
			BatteryLevel = await ButtplugClientDevice.BatteryAsync();
		}
		catch (Exception e)
		{
			Logger.Warn("Device.UpdateBatteryLevel: " + e.Message);
		}
	}

	public string GetBatteryPercentage()
	{
		if (BatteryLevel == -1.0)
		{
			return "Unknown";
		}
		return $"{BatteryLevel * 100.0}%";
	}

	public async void Stop()
	{
		if (ButtplugClientDevice == null)
		{
			return;
		}
		try
		{
			if (CanVibrate)
			{
				await ButtplugClientDevice.VibrateAsync(0.0);
			}
			if (CanRotate)
			{
				await ButtplugClientDevice.RotateAsync(0.0, clockwise: true);
			}
			if (CanOscillate)
			{
				await ButtplugClientDevice.OscillateAsync(0.0);
			}
			if (CanStop)
			{
				await ButtplugClientDevice.Stop();
			}
		}
		catch (Exception e)
		{
			Logger.Error("Device.Stop: " + e.Message);
		}
		ResetMotors();
	}

	public async void SendVibrate(int intensity, int motorId = -1, int threshold = 100, int timer = 2000)
	{
		if (ButtplugClientDevice == null || !CanVibrate || !IsConnected)
		{
			return;
		}
		int nbrMotors = VibrateMotors;
		try
		{
			if (motorId != -1)
			{
				CurrentVibrateIntensity[motorId] = intensity;
			}
			else
			{
				for (int i = 0; i < nbrMotors; i++)
				{
					CurrentVibrateIntensity[i] = intensity;
				}
			}
			double[] motorIntensity = new double[nbrMotors];
			for (int i = 0; i < nbrMotors; i++)
			{
				double clampedIntensity = (double)Helpers.ClampIntensity(CurrentVibrateIntensity[i], threshold) / 100.0;
				motorIntensity[i] = clampedIntensity;
			}
			VibrateDebouncer.Debounce(delegate
			{
				ButtplugClientDevice.VibrateAsync(motorIntensity);
			});
		}
		catch (Exception e)
		{
			Logger.Error("Device.SendVibrate: " + e.Message);
		}
	}

	public void SendRotate(int intensity, bool clockWise = true, int motorId = -1, int threshold = 100)
	{
		if (ButtplugClientDevice == null || !CanRotate || !IsConnected)
		{
			return;
		}
		int nbrMotors = RotateMotors;
		try
		{
			if (motorId != -1)
			{
				CurrentRotateIntensity[motorId] = intensity;
			}
			else
			{
				for (int i = 0; i < nbrMotors; i++)
				{
					CurrentRotateIntensity[i] = intensity;
				}
			}
			List<(double, bool)> motorIntensity = new List<(double, bool)>();
			for (int i = 0; i < nbrMotors; i++)
			{
				double clampedIntensity = (double)Helpers.ClampIntensity(CurrentRotateIntensity[i], threshold) / 100.0;
				motorIntensity.Add((clampedIntensity, clockWise));
			}
			RotateDebouncer.Debounce(delegate
			{
				for (int j = 0; j < nbrMotors; j++)
				{
					Logger.Warn(j + " MotorIntensity: " + motorIntensity[j].ToString());
				}
				ButtplugClientDevice.RotateAsync(motorIntensity);
			});
		}
		catch (Exception e)
		{
			Logger.Error("Device.SendRotate: " + e.Message);
		}
	}

	public void SendOscillate(int intensity, int duration = 500, int motorId = -1, int threshold = 100)
	{
		if (ButtplugClientDevice == null || !CanOscillate || !IsConnected)
		{
			return;
		}
		int nbrMotors = OscillateMotors;
		try
		{
			if (motorId != -1)
			{
				CurrentOscillateIntensity[motorId] = intensity;
			}
			else
			{
				for (int i = 0; i < nbrMotors; i++)
				{
					CurrentOscillateIntensity[i] = intensity;
				}
			}
			double[] motorIntensity = new double[nbrMotors];
			for (int i = 0; i < nbrMotors; i++)
			{
				double clampedIntensity = (double)Helpers.ClampIntensity(CurrentOscillateIntensity[i], threshold) / 100.0;
				motorIntensity[i] = clampedIntensity;
			}
			OscillateDebouncer.Debounce(delegate
			{
				for (int j = 0; j < nbrMotors; j++)
				{
					Logger.Warn(j + " MotorIntensity: " + motorIntensity[j]);
				}
				ButtplugClientDevice.OscillateAsync(motorIntensity);
			});
		}
		catch (Exception e)
		{
			Logger.Error("Device.SendOscillate: " + e.Message);
		}
	}

	public void SendLinear(int intensity, int duration = 500, int motorId = -1, int threshold = 100)
	{
		if (ButtplugClientDevice == null || !CanLinear || !IsConnected)
		{
			return;
		}
		int nbrMotors = RotateMotors;
		try
		{
			if (motorId != -1)
			{
				CurrentLinearIntensity[motorId] = intensity;
			}
			else
			{
				for (int i = 0; i < nbrMotors; i++)
				{
					CurrentLinearIntensity[i] = intensity;
				}
			}
			List<(uint, double)> motorIntensity = new List<(uint, double)>();
			for (int i = 0; i < nbrMotors; i++)
			{
				double clampedIntensity = (double)Helpers.ClampIntensity(CurrentLinearIntensity[i], threshold) / 100.0;
				motorIntensity.Add(((uint)i, clampedIntensity));
			}
			LinearDebouncer.Debounce(delegate
			{
				for (int j = 0; j < nbrMotors; j++)
				{
					Logger.Warn(j + " MotorIntensity: " + motorIntensity[j].ToString());
				}
				ButtplugClientDevice.LinearAsync(motorIntensity);
			});
		}
		catch (Exception e)
		{
			Logger.Error("Device.SendRotate: " + e.Message);
		}
	}
}
