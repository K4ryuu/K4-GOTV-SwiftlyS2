using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace K4GOTV;

public sealed partial class Plugin
{
	private void RegisterEvents()
	{
		Core.Event.OnMapLoad += OnMapLoad;
		Core.Event.OnMapUnload += OnMapUnload;

		Core.GameEvent.HookPost<EventCsWinPanelMatch>(OnMatchEnd);
		Core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
		Core.GameEvent.HookPost<EventPlayerActivate>(OnPlayerActivate);
	}

	private void RegisterCommands()
	{
		if (Config.CurrentValue.DemoRequest.Enabled)
			Core.Command.RegisterCommand("demo", OnDemoRequest);
	}

	private void OnMapLoad(IOnMapLoadEvent @event)
	{
		Core.Scheduler.DelayBySeconds(0.1f, () =>
		{
			Directory.CreateDirectory(DemoDirectory);

			if (Config.CurrentValue.General.DeleteEveryDemoFromServerAfterServerStart)
			{
				Task.Run(async () =>
				{
					foreach (var file in Directory.GetFiles(DemoDirectory, "*.dem").Concat(Directory.GetFiles(DemoDirectory, "*.zip")))
						await DeleteFileAsync(file);
				});
			}
		});
	}

	private void OnMapUnload(IOnMapUnloadEvent @event)
	{
		StopRecording();
	}

	private HookResult OnMatchEnd(EventCsWinPanelMatch @event)
	{
		StopRecording();
		return HookResult.Continue;
	}

	private HookResult OnRoundStart(EventRoundStart @event)
	{
		if (!Config.CurrentValue.AutoRecord.Enabled) return HookResult.Continue;

		if (Config.CurrentValue.AutoRecord.CropRounds && _isRecording)
			StopRecording();

		if (Config.CurrentValue.AutoRecord.CropRounds)
			_requesters.Clear();

		if (!_isRecording && GetRealPlayerCount() > 0)
			Core.Scheduler.NextWorldUpdate(() => StartRecording("autodemo"));

		return HookResult.Continue;
	}

	private HookResult OnPlayerActivate(EventPlayerActivate @event)
	{
		var player = Core.PlayerManager.GetPlayer(@event.UserId);
		if (player?.IsValid != true || player.IsFakeClient || player.Controller?.IsHLTV == true)
			return HookResult.Continue;

		_lastPlayerCheckTime = Core.Engine.GlobalVars.CurrentTime;

		if (!_isRecording && Config.CurrentValue.AutoRecord.Enabled)
			StartRecording("autodemo");

		return HookResult.Continue;
	}

	private void OnDemoRequest(ICommandContext ctx)
	{
		var player = ctx.Sender;
		if (player?.IsValid != true) return;

		var localizer = Core.Translation.GetPlayerLocalizer(player);

		if (Config.CurrentValue.DemoRequest.PrintAll && !_demoRequestedThisRound)
		{
			foreach (var p in Core.PlayerManager.GetAllPlayers().Where(p => p.IsValid && !p.IsFakeClient))
			{
				var loc = Core.Translation.GetPlayerLocalizer(p);
				p.SendChat($" {loc["k4.general.prefix"]} {loc["k4.chat.demo.request.all", player.Controller?.PlayerName ?? "Unknown"]}");
			}
		}
		else
		{
			player.SendChat($" {localizer["k4.general.prefix"]} {localizer["k4.chat.demo.request.self"]}");
		}

		var steamId = player.Controller?.SteamID ?? 0;
		var name = player.Controller?.PlayerName ?? "Unknown";

		if (!_requesters.Any(r => r.SteamId == steamId))
			_requesters.Add((name, steamId));

		_demoRequestedThisRound = true;
	}
}
