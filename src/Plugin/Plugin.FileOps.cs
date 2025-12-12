using System.Text.Json;
using CG.Web.MegaApiClient;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace K4GOTV;

public sealed partial class Plugin
{
	private record RetentionRecord(string Service, string Identifier, DateTime UploadedAt);

	private async Task DeleteFileAsync(string path)
	{
		for (int i = 0; i < 3; i++)
		{
			try
			{
				if (!File.Exists(path))
					return;

				File.Delete(path);

				if (Config.CurrentValue.General.LogDeletions)
					Core.Logger.LogInformation("Deleted: {Path}", path);

				return;
			}
			catch (IOException) when (i < 2)
			{
				await Task.Delay(100 * (i + 1));
			}
			catch (Exception ex)
			{
				Core.Logger.LogError("Delete failed: {Message}", ex.Message);
				return;
			}
		}
	}

	private void CleanupOldFiles()
	{
		var cutoff = DateTime.Now.AddHours(-Config.CurrentValue.General.AutoCleanupFileAgeHours);

		foreach (var file in Directory.GetFiles(DemoDirectory, "*.dem").Concat(Directory.GetFiles(DemoDirectory, "*.zip")))
		{
			if (File.GetCreationTime(file) < cutoff)
				Task.Run(() => DeleteFileAsync(file));
		}
	}

	private List<RetentionRecord> LoadRetentionRecords()
	{
		if (!File.Exists(RetentionFilePath))
			return [];

		var json = File.ReadAllText(RetentionFilePath);
		return JsonSerializer.Deserialize<List<RetentionRecord>>(json) ?? [];
	}

	private void SaveRetentionRecords(List<RetentionRecord> records)
	{
		var dir = Path.GetDirectoryName(RetentionFilePath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);

		File.WriteAllText(RetentionFilePath, JsonSerializer.Serialize(records));
	}

	private void AddRetentionRecord(string service, string identifier)
	{
		var records = LoadRetentionRecords();
		records.Add(new RetentionRecord(service, identifier, DateTime.Now));
		SaveRetentionRecords(records);
	}

	private async Task CleanFtpRetentionAsync()
	{
		if (!Config.CurrentValue.Ftp.RetentionEnabled)
			return;

		var records = LoadRetentionRecords();
		var expired = records.Where(r => r.Service == "ftp" && (DateTime.Now - r.UploadedAt).TotalHours >= Config.CurrentValue.Ftp.RetentionHours).ToList();
		if (expired.Count == 0)
			return;

		var cfg = Config.CurrentValue.Ftp;
		using var client = new AsyncFtpClient(cfg.Host, cfg.Username, cfg.Password, cfg.Port);
		client.Config.EncryptionMode = cfg.UseSftp ? FtpEncryptionMode.Implicit : FtpEncryptionMode.None;
		client.Config.ValidateAnyCertificate = true;
		await client.AutoConnect();

		var removed = new List<RetentionRecord>();
		foreach (var record in expired)
		{
			try
			{
				await client.DeleteFile(record.Identifier);
				removed.Add(record);
				Core.Logger.LogInformation("Deleted FTP file: {Id}", record.Identifier);
			}
			catch (Exception ex)
			{
				Core.Logger.LogError("FTP delete failed: {Message}", ex.Message);
			}
		}

		await client.Disconnect();
		if (removed.Count > 0) SaveRetentionRecords(records.Except(removed).ToList());
	}

	private async Task CleanMegaRetentionAsync()
	{
		if (!Config.CurrentValue.Mega.RetentionEnabled)
			return;

		var records = LoadRetentionRecords();
		var expired = records.Where(r => r.Service == "mega" && (DateTime.Now - r.UploadedAt).TotalHours >= Config.CurrentValue.Mega.RetentionHours).ToList();
		if (expired.Count == 0)
			return;

		var client = new MegaApiClient();
		try
		{
			await client.LoginAsync(Config.CurrentValue.Mega.Email, Config.CurrentValue.Mega.Password);
			var nodes = await client.GetNodesAsync();

			var removed = new List<RetentionRecord>();
			foreach (var record in expired)
			{
				try
				{
					var node = nodes.SingleOrDefault(n => n.Id.ToString() == record.Identifier);
					if (node != null)
					{
						await client.DeleteAsync(node, moveToTrash: false);
						Core.Logger.LogInformation("Deleted Mega node: {Id}", record.Identifier);
					}
					removed.Add(record);
				}
				catch (Exception ex)
				{
					Core.Logger.LogError("Mega delete failed: {Message}", ex.Message);
				}
			}

			if (removed.Count > 0) SaveRetentionRecords(records.Except(removed).ToList());
		}
		finally
		{
			if (client.IsLoggedIn)
				await client.LogoutAsync();
		}
	}
}
