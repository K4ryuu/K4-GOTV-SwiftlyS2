using Dapper;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace K4GOTV;

public sealed partial class Plugin
{
	private sealed class DatabaseService(ISwiftlyCore Core, string connectionName)
	{
		private readonly string _connectionName = connectionName;

		public bool IsEnabled { get; private set; }

		public async Task InitializeAsync()
		{
			if (string.IsNullOrEmpty(_connectionName)) return;

			try
			{
				const string sql = $"""
					CREATE TABLE IF NOT EXISTS `k4-gotv` (
						id BIGINT AUTO_INCREMENT PRIMARY KEY,
						map VARCHAR(255) NOT NULL,
						date DATE NOT NULL,
						time TIME NOT NULL,
						length VARCHAR(8) NOT NULL,
						round INT NOT NULL,
						mega_link TEXT NOT NULL,
						ftp_link TEXT NOT NULL,
						requester_name TEXT NOT NULL,
						requester_steamid TEXT NOT NULL,
						requester_count INT NOT NULL,
						player_count INT NOT NULL,
						server_name VARCHAR(255) NOT NULL,
						file_name VARCHAR(255) NOT NULL,
						KEY idx_map_date (map, date)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
					""";

				using var conn = Core.Database.GetConnection(_connectionName);
				conn.Open();
				await conn.ExecuteAsync(sql);
				IsEnabled = true;
			}
			catch (Exception ex)
			{
				Core.Logger.LogError("Database init failed: {Message}", ex.Message);
			}
		}

		public async Task StoreDemoRecordAsync(string fileName, string? megaLink, string? ftpLink, List<(string Name, ulong SteamId)> requesters, TimeSpan duration, int round, int playerCount)
		{
			if (!IsEnabled) return;

			try
			{
				const string sql = """
					INSERT INTO `k4-gotv` (
						map, date, time, length, round, mega_link, ftp_link,
						requester_name, requester_steamid, requester_count,
						player_count, server_name, file_name
					) VALUES (
						@Map, @Date, @Time, @Length, @Round, @MegaLink, @FtpLink,
						@RequesterName, @RequesterSteamId, @RequesterCount,
						@PlayerCount, @ServerName, @FileName
					);
					""";

				using var conn = Core.Database.GetConnection(_connectionName);
				conn.Open();

				await conn.ExecuteAsync(sql, new
				{
					Map = Core.Engine.GlobalVars.MapName,
					Date = DateTime.Now.ToString("yyyy-MM-dd"),
					Time = DateTime.Now.ToString("HH:mm:ss"),
					Length = duration.ToString(@"mm\:ss"),
					Round = round,
					MegaLink = megaLink ?? "Not uploaded",
					FtpLink = ftpLink ?? "Not uploaded",
					RequesterName = string.Join(", ", requesters.Select(r => r.Name)),
					RequesterSteamId = string.Join(", ", requesters.Select(r => r.SteamId)),
					RequesterCount = requesters.Count,
					PlayerCount = playerCount,
					ServerName = Core.ConVar.Find<string>("hostname")?.Value ?? "Unknown",
					FileName = fileName
				});
			}
			catch (Exception ex)
			{
				Core.Logger.LogError("Database write failed: {Message}", ex.Message);
			}
		}
	}
}
