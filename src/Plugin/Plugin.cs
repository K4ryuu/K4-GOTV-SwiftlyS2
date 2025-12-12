using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace K4GOTV;

[PluginMetadata(
	Id = "k4.gotv",
	Version = "1.0.1",
	Name = "K4 - GOTV",
	Author = "K4ryuu",
	Description = "Advanced GOTV handler with Discord, database, FTP, SFTP and Mega integration.")]
public sealed partial class Plugin(ISwiftlyCore core) : BasePlugin(core)
{
	private const string ConfigFileName = "k4-gotv.jsonc";
	private const string ConfigSection = "K4GOTV";

	public static IOptionsMonitor<PluginConfig> Config { get; private set; } = null!;

	private DatabaseService? _database;

	private CancellationTokenSource? _cleanupTimerCts;
	private CancellationTokenSource? _ftpRetentionTimerCts;
	private CancellationTokenSource? _megaRetentionTimerCts;

	private string DemoDirectory => Path.Combine(Core.CSGODirectory, Config.CurrentValue.General.DemoDirectory);
	private string RetentionFilePath => Path.Combine(Core.PluginDataDirectory, "uploads_retention.json");
	private string PayloadTemplatePath => Path.Combine(Core.PluginPath, "resources", "payload.json");

	private int MaxDiscordFileSizeMB => Config.CurrentValue.Discord.ServerBoost switch { 2 => 50, 3 => 100, _ => 25 };

	public override void Load(bool hotReload)
	{
		Core.Configuration
			.InitializeJsonWithModel<PluginConfig>(ConfigFileName, ConfigSection)
			.Configure(builder =>
			{
				builder.AddJsonFile(ConfigFileName, optional: false, reloadOnChange: true);
			});

		ServiceCollection services = new();
		services.AddSwiftly(Core)
			.AddOptions<PluginConfig>()
			.BindConfiguration(ConfigFileName);

		var provider = services.BuildServiceProvider();
		Config = provider.GetRequiredService<IOptionsMonitor<PluginConfig>>();

		Directory.CreateDirectory(DemoDirectory);
		Core.Logger.LogInformation("Demo directory: {Path} (exists: {Exists})", DemoDirectory, Directory.Exists(DemoDirectory));

		InitializeDatabase();
		RegisterEvents();
		RegisterCommands();
		StartTimers();

		if (hotReload && Config.CurrentValue.AutoRecord.Enabled && GetRealPlayerCount() > 0)
			StartRecording("autodemo");
	}

	public override void Unload()
	{
		StopRecording();

		_cleanupTimerCts?.Cancel();
		_ftpRetentionTimerCts?.Cancel();
		_megaRetentionTimerCts?.Cancel();
	}

	private void InitializeDatabase()
	{
		if (!string.IsNullOrEmpty(Config.CurrentValue.DatabaseConnection))
		{
			_database = new DatabaseService(Core, Config.CurrentValue.DatabaseConnection);
			Task.Run(_database.InitializeAsync);
		}
	}

	private void StartTimers()
	{
		if (Config.CurrentValue.General.AutoCleanupEnabled)
			_cleanupTimerCts = Core.Scheduler.RepeatBySeconds(Config.CurrentValue.General.AutoCleanupIntervalMinutes * 60f, () => Task.Run(CleanupOldFiles));

		if (Config.CurrentValue.Ftp.RetentionEnabled)
			_ftpRetentionTimerCts = Core.Scheduler.RepeatBySeconds(3600f, () => Task.Run(CleanFtpRetentionAsync));

		if (Config.CurrentValue.Mega.RetentionEnabled)
			_megaRetentionTimerCts = Core.Scheduler.RepeatBySeconds(3600f, () => Task.Run(CleanMegaRetentionAsync));
	}

	private int GetRealPlayerCount() =>
		Core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient && p.Controller?.IsHLTV != true);
}
