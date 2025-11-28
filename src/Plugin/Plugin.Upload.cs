using System.IO.Compression;
using System.Text;
using CG.Web.MegaApiClient;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace K4GOTV;

public sealed partial class Plugin
{
	private async Task ProcessDemoAsync(string fileName, string demoPath, List<(string Name, ulong SteamId)> requesters, TimeSpan duration, int round, int playerCount)
	{
		var zipPath = Path.Combine(DemoDirectory, $"{fileName}.zip");

		try
		{
			// Zip
			if (!await ZipFileAsync(demoPath, zipPath))
				return;

			var fileSizeBytes = new FileInfo(zipPath).Length;
			string? megaLink = null, ftpLink = null;

			// FTP upload
			if (_config.Ftp.Enabled && !string.IsNullOrEmpty(_config.Ftp.Host))
			{
				var remotePath = Path.Combine(_config.Ftp.RemoteDirectory, Path.GetFileName(zipPath)).Replace("\\", "/");
				ftpLink = await UploadToFtpAsync(zipPath, remotePath);

				if (_config.Ftp.RetentionEnabled)
					AddRetentionRecord("ftp", remotePath);
			}

			// Mega upload
			if (_config.Mega.Enabled && !string.IsNullOrEmpty(_config.Mega.Email))
			{
				var (link, nodeId) = await UploadToMegaAsync(zipPath);
				megaLink = link;

				if (_config.Mega.RetentionEnabled && !string.IsNullOrEmpty(nodeId))
					AddRetentionRecord("mega", nodeId);
			}

			// Discord
			await SendToDiscordAsync(fileName, zipPath, fileSizeBytes, megaLink, ftpLink, requesters, duration, round, playerCount);

			// Database
			if (_database?.IsEnabled == true && (megaLink != null || ftpLink != null))
				await _database.StoreDemoRecordAsync(fileName, megaLink, ftpLink, requesters, duration, round, playerCount);

			// Cleanup
			if (_config.General.DeleteDemoAfterUpload)
				await DeleteFileAsync(demoPath);

			if (_config.General.DeleteZippedDemoAfterUpload)
				await DeleteFileAsync(zipPath);
		}
		catch (Exception ex)
		{
			Core.Logger.LogError("Demo processing failed: {Message}", ex.Message);
		}
	}

	private async Task<bool> ZipFileAsync(string sourcePath, string zipPath)
	{
		try
		{
			if (File.Exists(zipPath))
				File.Delete(zipPath);

			await Task.Run(() =>
			{
				using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
				zip.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
			});

			Core.Logger.LogInformation("Demo zipped: {Path}", zipPath);
			return true;
		}
		catch (Exception ex)
		{
			Core.Logger.LogError("Zip failed: {Message}", ex.Message);
			return false;
		}
	}

	private async Task<string> UploadToFtpAsync(string filePath, string remotePath)
	{
		var cfg = _config.Ftp;
		using var client = new AsyncFtpClient(cfg.Host, cfg.Username, cfg.Password, cfg.Port);

		try
		{
			client.Config.EncryptionMode = cfg.UseSftp ? FtpEncryptionMode.Implicit : FtpEncryptionMode.None;
			client.Config.ValidateAnyCertificate = true;

			await client.AutoConnect();
			await client.UploadFile(filePath, remotePath);

			var protocol = cfg.UseSftp ? "sftp" : "ftp";
			return $"{protocol}://{cfg.Host}/{remotePath}";
		}
		finally
		{
			await client.Disconnect();
		}
	}

	private async Task<(string Link, string NodeId)> UploadToMegaAsync(string filePath)
	{
		var client = new MegaApiClient();
		try
		{
			await client.LoginAsync(_config.Mega.Email, _config.Mega.Password);

			var rootNode = (await client.GetNodesAsync()).Single(x => x.Type == NodeType.Root);
			var uploadedNode = await client.UploadFileAsync(filePath, rootNode);
			var downloadLink = await client.GetDownloadLinkAsync(uploadedNode);

			return (downloadLink.ToString(), uploadedNode.Id.ToString());
		}
		catch (Exception ex)
		{
			Core.Logger.LogError("Mega upload error: {Message}", ex.Message);
			return ("Not uploaded to Mega.", string.Empty);
		}
		finally
		{
			if (client.IsLoggedIn)
				await client.LogoutAsync();
		}
	}

	private async Task SendToDiscordAsync(string fileName, string zipPath, long fileSizeBytes, string? megaLink, string? ftpLink, List<(string Name, ulong SteamId)> requesters, TimeSpan duration, int round, int playerCount)
	{
		if (string.IsNullOrWhiteSpace(_config.Discord.WebhookURL))
		{
			if (_config.General.LogUploads)
				Core.Logger.LogInformation("Demo processed (no Discord webhook): {FileName}", fileName);

			return;
		}

		if (!File.Exists(PayloadTemplatePath))
		{
			Core.Logger.LogError("Payload template not found: {Path}", PayloadTemplatePath);
			return;
		}

		var template = await File.ReadAllTextAsync(PayloadTemplatePath);
		var fileSizeMB = fileSizeBytes / (1024 * 1024);

		var downloadLinks = new List<string>();
		if (!string.IsNullOrEmpty(megaLink))
			downloadLinks.Add($"**Mega:** [Click Here]({megaLink})");
		if (!string.IsNullOrEmpty(ftpLink))
			downloadLinks.Add($"**FTP:** [Click Here]({ftpLink})");

		var payload = template
			.Replace("{webhook_name}", _config.Discord.WebhookName)
			.Replace("{webhook_avatar}", _config.Discord.WebhookAvatar)
			.Replace("{message_text}", _config.Discord.MessageText)
			.Replace("{embed_title}", _config.Discord.EmbedTitle)
			.Replace("{map}", Core.Engine.GlobalVars.MapName)
			.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))
			.Replace("{time}", DateTime.Now.ToString("HH:mm:ss"))
			.Replace("{timedate}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
			.Replace("{length}", duration.ToString(@"mm\:ss"))
			.Replace("{round}", round.ToString())
			.Replace("{download_links}", downloadLinks.Count > 0 ? string.Join("\\n", downloadLinks) : "No external uploads")
			.Replace("{requester_name}", string.Join(", ", requesters.Select(r => r.Name)))
			.Replace("{requester_steamid}", string.Join(", ", requesters.Select(r => r.SteamId)))
			.Replace("{requester_both}", string.Join("\\n", requesters.Select(r => $"{r.Name} ({r.SteamId})")))
			.Replace("{requester_count}", requesters.Count.ToString())
			.Replace("{player_count}", playerCount.ToString())
			.Replace("{server_name}", Core.ConVar.Find<string>("hostname")?.Value ?? "Unknown Server")
			.Replace("{fileName}", fileName)
			.Replace("{iso_timestamp}", DateTime.UtcNow.ToString("o"))
			.Replace("{file_size_warning}", fileSizeMB > MaxDiscordFileSizeMB ? $"⚠️ File size ({fileSizeMB}MB) exceeds Discord limit." : "")
			.Replace("{fileSizeInKB}", (fileSizeBytes / 1024).ToString());

		using var httpClient = new HttpClient();
		using var content = new MultipartFormDataContent
		{
			{ new StringContent(payload, Encoding.UTF8, "application/json"), "payload_json" }
		};

		if (File.Exists(zipPath) && fileSizeMB <= MaxDiscordFileSizeMB && _config.Discord.WebhookUploadFile)
			content.Add(new ByteArrayContent(await File.ReadAllBytesAsync(zipPath)), "file", $"{fileName}.zip");

		var response = await httpClient.PostAsync(_config.Discord.WebhookURL, content);
		response.EnsureSuccessStatusCode();

		if (_config.General.LogUploads)
			Core.Logger.LogInformation("Demo uploaded to Discord: {FileName}", fileName);
	}
}
