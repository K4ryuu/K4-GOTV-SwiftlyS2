using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace K4GOTV;

[PluginMetadata(
	Id = "k4.gotv",
	Version = "1.0.0",
	Name = "K4 - GOTV",
	Author = "K4ryuu",
	Description = "Advanced GOTV handler with Discord, database, FTP, SFTP and Mega integration.")]
public sealed partial class Plugin(ISwiftlyCore core) : BasePlugin(core)
{
	private PluginConfig _config = null!;
	private DatabaseService? _database;

	private CancellationTokenSource? _cleanupTimerCts;
	private CancellationTokenSource? _ftpRetentionTimerCts;
	private CancellationTokenSource? _megaRetentionTimerCts;

	private string DemoDirectory => Path.Combine(Core.CSGODirectory, _config.General.DemoDirectory);
	private string RetentionFilePath => Path.Combine(Core.PluginDataDirectory, "uploads_retention.json");
	private string PayloadTemplatePath => Path.Combine(Core.PluginPath, "resources", "payload.json");

	private int MaxDiscordFileSizeMB => _config.Discord.ServerBoost switch { 2 => 50, 3 => 100, _ => 25 };

	public override void Load(bool hotReload)
	{
		_config = LoadConfiguration();

		Directory.CreateDirectory(DemoDirectory);
		Core.Logger.LogInformation("Demo directory: {Path} (exists: {Exists})", DemoDirectory, Directory.Exists(DemoDirectory));

		InitializeDatabase();
		RegisterEvents();
		RegisterCommands();
		StartTimers();

		if (hotReload && _config.AutoRecord.Enabled && GetRealPlayerCount() > 0)
			StartRecording("autodemo");
	}

	public override void Unload()
	{
		StopRecording();

		_cleanupTimerCts?.Cancel();
		_ftpRetentionTimerCts?.Cancel();
		_megaRetentionTimerCts?.Cancel();
	}

	private PluginConfig LoadConfiguration()
	{
		const string ConfigFileName = "config.json";
		const string ConfigSection = "K4GOTV";

		Core.Configuration
			.InitializeJsonWithModel<PluginConfig>(ConfigFileName, ConfigSection)
			.Configure(cfg => cfg.AddJsonFile(Core.Configuration.GetConfigPath(ConfigFileName), optional: false, reloadOnChange: true));

		ServiceCollection services = new();
		services.AddSwiftly(Core)
			.AddOptionsWithValidateOnStart<PluginConfig>()
			.BindConfiguration(ConfigSection);

		var config = services.BuildServiceProvider().GetRequiredService<IOptions<PluginConfig>>().Value;

		if (string.IsNullOrWhiteSpace(config.General.DemoDirectory))
			config.General.DemoDirectory = "demos";

		if (config.DemoRequest.Enabled)
		{
			config.AutoRecord.Enabled = true;
			config.AutoRecord.CropRounds = true;
		}

		return config;
	}

	private void InitializeDatabase()
	{
		if (!string.IsNullOrEmpty(_config.DatabaseConnection))
		{
			_database = new DatabaseService(Core, _config.DatabaseConnection);
			Task.Run(_database.InitializeAsync);
		}
	}

	private void StartTimers()
	{
		if (_config.General.AutoCleanupEnabled)
			_cleanupTimerCts = Core.Scheduler.RepeatBySeconds(_config.General.AutoCleanupIntervalMinutes * 60f, () => Task.Run(CleanupOldFiles));

		if (_config.Ftp.RetentionEnabled)
			_ftpRetentionTimerCts = Core.Scheduler.RepeatBySeconds(3600f, () => Task.Run(CleanFtpRetentionAsync));

		if (_config.Mega.RetentionEnabled)
			_megaRetentionTimerCts = Core.Scheduler.RepeatBySeconds(3600f, () => Task.Run(CleanMegaRetentionAsync));
	}

	private int GetRealPlayerCount() =>
		Core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient && p.Controller?.IsHLTV != true);
}
