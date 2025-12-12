namespace K4GOTV;

public sealed class PluginConfig
{
	public string DatabaseConnection { get; set; } = "host";
	public GeneralSettings General { get; set; } = new();
	public DiscordSettings Discord { get; set; } = new();

	private AutoRecordSettings _autoRecord = new();
	public AutoRecordSettings AutoRecord
	{
		get
		{
			if (DemoRequest.Enabled)
			{
				_autoRecord.Enabled = true;
				_autoRecord.CropRounds = true;
			}
			return _autoRecord;
		}
		set => _autoRecord = value;
	}

	public MegaSettings Mega { get; set; } = new();
	public DemoRequestSettings DemoRequest { get; set; } = new();
	public FtpSettings Ftp { get; set; } = new();
}

public sealed class GeneralSettings
{
	public float MinimumDemoDuration { get; set; } = 5.0f;
	public bool DeleteDemoAfterUpload { get; set; } = true;
	public bool DeleteZippedDemoAfterUpload { get; set; } = true;
	public bool DeleteEveryDemoFromServerAfterServerStart { get; set; } = false;
	public bool LogUploads { get; set; } = true;
	public bool LogDeletions { get; set; } = true;
	public string RegularFileNamingPattern { get; set; } = "{fileName}_{map}_{date}_{time}";
	public string CropRoundsFileNamingPattern { get; set; } = "{fileName}_{map}_round{round}_{date}_{time}";
	public string DemoDirectory { get; set; } = "demos";
	public bool AutoCleanupEnabled { get; set; } = false;
	public int AutoCleanupIntervalMinutes { get; set; } = 60;
	public int AutoCleanupFileAgeHours { get; set; } = 48;
}

public sealed class DiscordSettings
{
	public string WebhookURL { get; set; } = "";
	public string WebhookAvatar { get; set; } = "";
	public bool WebhookUploadFile { get; set; } = true;
	public string WebhookName { get; set; } = "CSGO Demo Bot";
	public string EmbedTitle { get; set; } = "New CSGO Demo Available";
	public string MessageText { get; set; } = "@everyone New CSGO Demo Available!";
	public int ServerBoost { get; set; } = 0;
}

public sealed class AutoRecordSettings
{
	public bool Enabled { get; set; } = false;
	public bool CropRounds { get; set; } = false;
	public bool StopOnIdle { get; set; } = false;
	public bool RecordWarmup { get; set; } = true;
	public int IdlePlayerCountThreshold { get; set; } = 0;
	public int IdleTimeSeconds { get; set; } = 300;
}

public sealed class MegaSettings
{
	public bool Enabled { get; set; } = false;
	public string Email { get; set; } = "";
	public string Password { get; set; } = "";
	public bool RetentionEnabled { get; set; } = false;
	public int RetentionHours { get; set; } = 72;
}

public sealed class DemoRequestSettings
{
	public bool Enabled { get; set; } = false;
	public bool PrintAll { get; set; } = true;
	public bool DeleteUnused { get; set; } = true;
}

public sealed class FtpSettings
{
	public bool Enabled { get; set; } = false;
	public string Host { get; set; } = "";
	public int Port { get; set; } = 21;
	public string Username { get; set; } = "";
	public string Password { get; set; } = "";
	public string RemoteDirectory { get; set; } = "/";
	public bool UseSftp { get; set; } = false;
	public bool RetentionEnabled { get; set; } = false;
	public int RetentionHours { get; set; } = 72;
}
