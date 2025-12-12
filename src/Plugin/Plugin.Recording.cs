using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace K4GOTV;

public sealed partial class Plugin
{
	private CancellationTokenSource? _idleTimerCts;

	private bool _isRecording;
	private string? _fileName;
	private double _demoStartTime;
	private double _lastPlayerCheckTime;
	private bool _demoRequestedThisRound;
	private readonly List<(string Name, ulong SteamId)> _requesters = [];

	private void StartRecording(string baseName)
	{
		if (_isRecording)
			return;

		var gameRules = Core.EntitySystem.GetGameRules();
		if (!Config.CurrentValue.AutoRecord.RecordWarmup && gameRules?.WarmupPeriod == true)
			return;

		var pattern = Config.CurrentValue.AutoRecord.CropRounds
			? Config.CurrentValue.General.CropRoundsFileNamingPattern
			: Config.CurrentValue.General.RegularFileNamingPattern;

		_fileName = BuildFileName(pattern, baseName);
		var fullPath = Path.Combine(DemoDirectory, $"{_fileName}.dem");

		var counter = 1;
		while (File.Exists(fullPath))
		{
			_fileName = $"{_fileName}_{counter++}";
			fullPath = Path.Combine(DemoDirectory, $"{_fileName}.dem");
		}

		Core.Engine.ExecuteCommand($"tv_record \"{fullPath}\"");

		_isRecording = true;
		_demoStartTime = Core.Engine.GlobalVars.CurrentTime;
		_lastPlayerCheckTime = _demoStartTime;

		Core.Logger.LogInformation("Recording started: {FileName}", _fileName);

		if (Config.CurrentValue.AutoRecord.StopOnIdle)
		{
			_idleTimerCts?.Cancel();
			_idleTimerCts = Core.Scheduler.RepeatBySeconds(1f, CheckIdleState);
		}
	}

	private void StopRecording()
	{
		_idleTimerCts?.Cancel();
		_idleTimerCts = null;

		if (!_isRecording || string.IsNullOrEmpty(_fileName))
		{
			ResetRecordingState();
			return;
		}

		Core.Engine.ExecuteCommand("tv_stoprecord");
		Core.Logger.LogInformation("Recording stopped: {FileName}", _fileName);

		var duration = Core.Engine.GlobalVars.CurrentTime - _demoStartTime;
		if (duration < Config.CurrentValue.General.MinimumDemoDuration)
		{
			ResetRecordingState();
			return;
		}

		var demoPath = Path.Combine(DemoDirectory, $"{_fileName}.dem");
		var fileName = _fileName;
		var requesters = _requesters.ToList();
		var wasRequested = _demoRequestedThisRound;

		// Capture main thread values before background task
		var gameRules = Core.EntitySystem.GetGameRules();
		var round = (gameRules?.TotalRoundsPlayed ?? 0) + 1;
		var playerCount = GetRealPlayerCount();

		ResetRecordingState();

		Core.Scheduler.DelayBySeconds(2f, () =>
		{
			if (!File.Exists(demoPath))
			{
				Core.Logger.LogError("Demo file not found: {Path}", demoPath);
				return;
			}

			if (Config.CurrentValue.DemoRequest.Enabled && !wasRequested)
			{
				if (Config.CurrentValue.DemoRequest.DeleteUnused)
					Task.Run(() => DeleteFileAsync(demoPath));
				return;
			}

			Task.Run(() => ProcessDemoAsync(fileName, demoPath, requesters, TimeSpan.FromSeconds(duration), round, playerCount));
		});
	}

	private void ResetRecordingState()
	{
		_isRecording = false;
		_fileName = null;
		_demoStartTime = 0;
		_demoRequestedThisRound = false;
	}

	private void CheckIdleState()
	{
		if (!_isRecording) return;

		var playerCount = GetRealPlayerCount();
		if (playerCount < Config.CurrentValue.AutoRecord.IdlePlayerCountThreshold)
		{
			var idleTime = Core.Engine.GlobalVars.CurrentTime - _lastPlayerCheckTime;
			if (idleTime > Config.CurrentValue.AutoRecord.IdleTimeSeconds)
			{
				Core.Logger.LogInformation("Stopping recording due to idle.");
				StopRecording();
			}
		}
		else
		{
			_lastPlayerCheckTime = Core.Engine.GlobalVars.CurrentTime;
		}
	}

	private string BuildFileName(string pattern, string baseName)
	{
		var gameRules = Core.EntitySystem.GetGameRules();
		return pattern
			.Replace("{fileName}", baseName)
			.Replace("{map}", Core.Engine.GlobalVars.MapName)
			.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))
			.Replace("{time}", DateTime.Now.ToString("HH-mm-ss"))
			.Replace("{timestamp}", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
			.Replace("{round}", ((gameRules?.TotalRoundsPlayed ?? 0) + 1).ToString())
			.Replace("{playerCount}", GetRealPlayerCount().ToString());
	}
}
