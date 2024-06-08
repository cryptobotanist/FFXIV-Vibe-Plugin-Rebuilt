using System;
using System.Collections.Generic;
using System.Threading;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Triggers;

namespace FFXIV_Vibe_Plugin.Device;

public class DevicesController
{
	private readonly Logger Logger;

	private readonly Configuration Configuration;

	private ConfigurationProfile Profile;

	private readonly Patterns Patterns;

	private Trigger? CurrentPlayingTrigger;

	public bool isConnected;

	public bool shouldExit;

	private readonly Dictionary<string, int> CurrentDeviceAndMotorPlaying = new Dictionary<string, int>();

	private ButtplugClient? BPClient;

	private readonly List<Device> Devices = new List<Device>();

	private bool isScanning;

	private static readonly Mutex mut = new Mutex();

	public DevicesController(Logger logger, Configuration configuration, ConfigurationProfile profile, Patterns patterns)
	{
		Logger = logger;
		Configuration = configuration;
		Profile = profile;
		Patterns = patterns;
	}

	public void Dispose()
	{
		shouldExit = true;
		Disconnect();
	}

	public void SetProfile(ConfigurationProfile profile)
	{
		Profile = profile;
	}

	public async void Connect(string host, int port)
	{
		Thread.Sleep(2000);
		Logger.Log("Connecting to Intiface...");
		isConnected = false;
		shouldExit = false;
		BPClient = new ButtplugClient("FFXIV_Vibe_Plugin");
		string hostandport = host;
		if (port > 0)
		{
			hostandport = hostandport + ":" + port;
		}
		ButtplugWebsocketConnector connector = null;
		try
		{
			string proto = "ws";
			if (Profile.BUTTPLUG_SERVER_SHOULD_WSS)
			{
				proto = "wss";
			}
			connector = new ButtplugWebsocketConnector(new Uri(proto + "://" + hostandport));
		}
		catch (Exception e)
		{
			Logger.Error("DeviceController.Connect: ButtplugWebsocketConnector error: " + e.Message);
		}
		BPClient.DeviceAdded += BPClient_DeviceAdded;
		BPClient.DeviceRemoved += BPClient_DeviceRemoved;
		try
		{
			await BPClient.ConnectAsync(connector);
		}
		catch (Exception ex)
		{
			Logger.Warn("Can't connect, exiting!");
			Logger.Warn("Message: " + ex.InnerException?.Message);
			return;
		}
		isConnected = true;
		Logger.Log("Connected!");
		try
		{
			Logger.Log("Fast scanning!");
			ScanDevice();
			Thread.Sleep(1000);
			StopScanningDevice();
			BPClient.StopScanningAsync();
		}
		catch (Exception e)
		{
			Logger.Error("DeviceController fast scanning: " + e.Message);
		}
		Logger.Log("Scanning done!");
		StartBatteryUpdaterThread();
	}

	private void BPClient_ServerDisconnected(object? sender, EventArgs e)
	{
		Logger.Debug("Server disconnected");
		Disconnect();
	}

	public bool IsConnected()
	{
		refreshIsConnected();
		return isConnected;
	}

	public void refreshIsConnected()
	{
		if (BPClient != null)
		{
			isConnected = BPClient.Connected;
		}
	}

	public async void ScanDevice()
	{
		if (BPClient == null)
		{
			return;
		}
		Logger.Debug("Scanning for devices...");
		if (!IsConnected())
		{
			return;
		}
		try
		{
			isScanning = true;
			await BPClient.StartScanningAsync();
		}
		catch (Exception e)
		{
			isScanning = false;
			Logger.Error("Scanning issue. No 'Device Comm Managers' enabled on Intiface?");
			Logger.Error(e.Message);
		}
	}

	public bool IsScanning()
	{
		return isScanning;
	}

	public async void StopScanningDevice()
	{
		if (BPClient != null && IsConnected())
		{
			try
			{
				Logger.Debug("Sending stop scanning command!");
				BPClient.StopScanningAsync();
			}
			catch (Exception)
			{
				Logger.Debug("StopScanningDevice ignored: already stopped");
			}
		}
		isScanning = false;
	}

	private void BPClient_OnScanComplete(object? sender, EventArgs e)
	{
		Logger.Debug("Stop scanning...");
		isScanning = false;
	}

	private void BPClient_DeviceAdded(object? sender, DeviceAddedEventArgs arg)
	{
		try
		{
			mut.WaitOne();
			Device device = new Device(arg.Device, Logger);
			device.IsConnected = true;
			Devices.Add(device);
			if (!Profile.VISITED_DEVICES.ContainsKey(device.Name))
			{
				Profile.VISITED_DEVICES[device.Name] = device;
				Configuration.Save();
				Logger.Debug($"Adding device to visited list {device})");
			}
			Logger.Debug($"Added {device})");
		}
		catch (Exception e)
		{
			Logger.Error("DeviceController.BPClient_DeviceAdded: " + e.Message);
		}
		finally
		{
			mut.ReleaseMutex();
		}
	}

	private void BPClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs arg)
	{
		try
		{
			mut.WaitOne();
			int index = Devices.FindIndex((Device device) => device.Id == arg.Device.Index);
			if (index > -1)
			{
				Logger.Debug($"Removed {Devices[index]}");
				Device device2 = Devices[index];
				Devices.RemoveAt(index);
				device2.IsConnected = false;
			}
		}
		catch (Exception e)
		{
			Logger.Error("DeviceController.BPClient_DeviceRemoved: " + e.Message);
		}
		finally
		{
			mut.ReleaseMutex();
		}
	}

	public async void Disconnect()
	{
		Logger.Debug("Disconnecting DeviceController");
		try
		{
			Devices.Clear();
		}
		catch (Exception e)
		{
			Logger.Error("DeviceController.Disconnect: " + e.Message);
		}
		if (BPClient == null || !IsConnected())
		{
			return;
		}
		try
		{
			Thread.Sleep(100);
			if (BPClient != null)
			{
				await BPClient.DisconnectAsync();
				Logger.Log("Disconnecting! Bye... Waiting 2sec...");
			}
		}
		catch (Exception e)
		{
			Logger.Error("Error while disconnecting client", e);
		}
		try
		{
			Logger.Debug("Disposing BPClient.");
			BPClient.Dispose();
		}
		catch (Exception e)
		{
			Logger.Error("Error while disposing BPClient", e);
		}
		BPClient = null;
		isConnected = false;
	}

	public List<Device> GetDevices()
	{
		return Devices;
	}

	public Dictionary<string, Device> GetVisitedDevices()
	{
		return Profile.VISITED_DEVICES;
	}

	private void StartBatteryUpdaterThread()
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			while (!shouldExit)
			{
				Thread.Sleep(5000);
				if (IsConnected())
				{
					Logger.Verbose("Updating battery levels!");
					UpdateAllBatteryLevel();
				}
			}
		});
		thread.Name = "batteryUpdaterThread";
		thread.Start();
	}

	public void UpdateAllBatteryLevel()
	{
		try
		{
			foreach (Device device in GetDevices())
			{
				device.UpdateBatteryLevel();
			}
		}
		catch (Exception e)
		{
			Logger.Error("DeviceController.UpdateAllBatteryLevel: " + e.Message);
		}
	}

	public void StopAll()
	{
		foreach (Device device in GetDevices())
		{
			try
			{
				device.Stop();
			}
			catch (Exception e)
			{
				Logger.Error("DeviceContoller.StopAll: " + e.Message);
			}
		}
	}

	public void SendTrigger(Trigger trigger, int threshold = 100)
	{
		if (!IsConnected())
		{
			Logger.Debug($"Not connected, cannot send ${trigger}");
			return;
		}
		Logger.Debug($"Sending trigger {trigger} (priority={trigger.Priority})");
		if (CurrentPlayingTrigger == null)
		{
			CurrentPlayingTrigger = trigger;
		}
		if (trigger.Priority < CurrentPlayingTrigger.Priority)
		{
			Logger.Debug($"Ignoring trigger because lower priority => {trigger} < {CurrentPlayingTrigger}");
			return;
		}
		CurrentPlayingTrigger = trigger;
		foreach (TriggerDevice triggerDevice in trigger.Devices)
		{
			Device device = FindDevice(triggerDevice.Name);
			if (device == null || triggerDevice == null)
			{
				continue;
			}
			if (triggerDevice.ShouldVibrate)
			{
				for (int motorId = 0; motorId < triggerDevice.VibrateSelectedMotors?.Length; motorId++)
				{
					if (triggerDevice.VibrateSelectedMotors != null && triggerDevice.VibrateMotorsThreshold != null)
					{
						bool num = triggerDevice.VibrateSelectedMotors[motorId];
						int motorThreshold = triggerDevice.VibrateMotorsThreshold[motorId] * threshold / 100;
						int motorPatternId = triggerDevice.VibrateMotorsPattern[motorId];
						float startAfter = trigger.StartAfter;
						float stopAfter = trigger.StopAfter;
						if (num)
						{
							Logger.Debug($"Sending {device.Name} vibration to motor: {motorId} patternId={motorPatternId} with threshold: {motorThreshold}!");
							Send("vibrate", device, motorThreshold, motorId, motorPatternId, startAfter, stopAfter);
						}
					}
				}
			}
			if (triggerDevice.ShouldRotate)
			{
				for (int motorId = 0; motorId < triggerDevice.RotateSelectedMotors?.Length; motorId++)
				{
					if (triggerDevice.RotateSelectedMotors != null && triggerDevice.RotateMotorsThreshold != null)
					{
						bool num2 = triggerDevice.RotateSelectedMotors[motorId];
						int motorThreshold = triggerDevice.RotateMotorsThreshold[motorId] * threshold / 100;
						int motorPatternId = triggerDevice.RotateMotorsPattern[motorId];
						float startAfter = trigger.StartAfter;
						float stopAfter = trigger.StopAfter;
						if (num2)
						{
							Logger.Debug($"Sending {device.Name} rotation to motor: {motorId} patternId={motorPatternId} with threshold: {motorThreshold}!");
							Send("rotate", device, motorThreshold, motorId, motorPatternId, startAfter, stopAfter);
						}
					}
				}
			}
			if (triggerDevice.ShouldLinear)
			{
				for (int motorId = 0; motorId < triggerDevice.LinearSelectedMotors?.Length; motorId++)
				{
					if (triggerDevice.LinearSelectedMotors != null && triggerDevice.LinearMotorsThreshold != null)
					{
						bool num3 = triggerDevice.LinearSelectedMotors[motorId];
						int motorThreshold = triggerDevice.LinearMotorsThreshold[motorId] * threshold / 100;
						int motorPatternId = triggerDevice.LinearMotorsPattern[motorId];
						float startAfter = trigger.StartAfter;
						float stopAfter = trigger.StopAfter;
						if (num3)
						{
							Logger.Debug($"Sending {device.Name} linear to motor: {motorId} patternId={motorPatternId} with threshold: {motorThreshold}!");
							Send("linear", device, motorThreshold, motorId, motorPatternId, startAfter, stopAfter);
						}
					}
				}
			}
			if (triggerDevice.ShouldOscillate)
			{
				for (int motorId = 0; motorId < triggerDevice.OscillateSelectedMotors?.Length; motorId++)
				{
					if (triggerDevice.OscillateSelectedMotors != null && triggerDevice.OscillateMotorsThreshold != null)
					{
						bool num4 = triggerDevice.OscillateSelectedMotors[motorId];
						int motorThreshold = triggerDevice.OscillateMotorsThreshold[motorId] * threshold / 100;
						int motorPatternId = triggerDevice.OscillateMotorsPattern[motorId];
						float startAfter = trigger.StartAfter;
						float stopAfter = trigger.StopAfter;
						if (num4)
						{
							Logger.Debug($"Sending {device.Name} oscillate to motor: {motorId} patternId={motorPatternId} with threshold: {motorThreshold}!");
							Send("oscillate", device, motorThreshold, motorId, motorPatternId, startAfter, stopAfter);
						}
					}
				}
			}
			if (triggerDevice.ShouldStop)
			{
				Logger.Debug("Sending stop to " + device.Name + "!");
				SendStop(device);
			}
		}
	}

	public Device? FindDevice(string text)
	{
		Device foundDevice = null;
		try
		{
			foreach (Device device in Devices)
			{
				if (device.Name.Contains(text) && device != null)
				{
					foundDevice = device;
				}
			}
		}
		catch (Exception e)
		{
			Logger.Error(e.ToString());
		}
		return foundDevice;
	}

	public void SendVibeToAll(int intensity)
	{
		if (!IsConnected() || BPClient == null)
		{
			return;
		}
		foreach (Device device in Devices)
		{
			device.SendVibrate(intensity, -1, Profile.MAX_VIBE_THRESHOLD);
			device.SendRotate(intensity, clockWise: true, -1, Profile.MAX_VIBE_THRESHOLD);
			device.SendLinear(intensity, 500, -1, Profile.MAX_VIBE_THRESHOLD);
			device.SendOscillate(intensity, 500, -1, Profile.MAX_VIBE_THRESHOLD);
		}
	}

	public void Send(string command, Device device, int threshold, int motorId = -1, int patternId = 0, float StartAfter = 0f, float StopAfter = 0f)
	{
		string deviceAndMotorId = $"{device.Name}:{motorId}";
		SaveCurrentMotorAndDevicePlayingState(device, motorId);
		Pattern pattern = Patterns.GetPatternById(patternId);
		string[] patternSegments = pattern.Value.Split("|");
		Logger.Log($"SendPattern '{command}' pattern={pattern.Name} ({patternSegments.Length} segments) to {device} motor={motorId} startAfter={StartAfter} stopAfter={StopAfter} threshold={threshold}");
		int startedUnixTime = CurrentDeviceAndMotorPlaying[deviceAndMotorId];
		bool forceStop = false;
		new Thread((ThreadStart)delegate
		{
			if ((double)StopAfter != 0.0)
			{
				Thread.Sleep((int)(StopAfter * 1000f));
				if (startedUnixTime == CurrentDeviceAndMotorPlaying[deviceAndMotorId])
				{
					forceStop = true;
					Logger.Debug($"Force stopping {deviceAndMotorId} because of StopAfter={StopAfter}");
					SendCommand(command, device, 0, motorId);
					CurrentPlayingTrigger = null;
				}
			}
		}).Start();
		new Thread((ThreadStart)delegate
		{
			Thread.Sleep((int)((double)StartAfter * 1000.0));
			if (startedUnixTime == CurrentDeviceAndMotorPlaying[deviceAndMotorId])
			{
				for (int i = 0; i < patternSegments.Length; i++)
				{
					if (startedUnixTime != CurrentDeviceAndMotorPlaying[deviceAndMotorId])
					{
						break;
					}
					string[] array = patternSegments[i].Split(":");
					int num = Helpers.ClampIntensity(int.Parse(array[0]), threshold);
					int num2 = int.Parse(array[1]);
					Logger.Debug($"SENDING SEGMENT: command={command} intensity={num} duration={num2} motorId={motorId}");
					SendCommand(command, device, num, motorId, num2);
					if (forceStop || (StopAfter > 0f && StopAfter * 1000f + (float)startedUnixTime < (float)Helpers.GetUnix()))
					{
						Logger.Debug($"SENDING SEGMENT ZERO: command={command} intensity={num} duration={num2} motorId={motorId}");
						SendCommand(command, device, 0, motorId, num2);
						break;
					}
					Thread.Sleep(num2);
				}
			}
		}).Start();
	}

	public void SendCommand(string command, Device device, int intensity, int motorId, int duration = 500)
	{
		switch (command)
		{
		case "vibrate":
			SendVibrate(device, intensity, motorId);
			break;
		case "rotate":
			SendRotate(device, intensity, motorId);
			break;
		case "linear":
			SendLinear(device, intensity, motorId, duration);
			break;
		case "oscillate":
			SendOscillate(device, intensity, motorId, duration);
			break;
		}
	}

	public void SendVibrate(Device device, int intensity, int motorId = -1)
	{
		device.SendVibrate(intensity, motorId, Profile.MAX_VIBE_THRESHOLD);
	}

	public void SendRotate(Device device, int intensity, int motorId = -1, bool clockwise = true)
	{
		device.SendRotate(intensity, clockwise, motorId, Profile.MAX_VIBE_THRESHOLD);
	}

	public void SendLinear(Device device, int intensity, int motorId = -1, int duration = 500)
	{
		device.SendLinear(intensity, duration, motorId, Profile.MAX_VIBE_THRESHOLD);
	}

	public void SendOscillate(Device device, int intensity, int motorId = -1, int duration = 500)
	{
		device.SendOscillate(intensity, duration, motorId, Profile.MAX_VIBE_THRESHOLD);
	}

	public static void SendStop(Device device)
	{
		device.Stop();
	}

	private void SaveCurrentMotorAndDevicePlayingState(Device device, int motorId)
	{
		string deviceAndMotorId = $"{device.Name}:{motorId}";
		CurrentDeviceAndMotorPlaying[deviceAndMotorId] = Helpers.GetUnix();
	}
}
