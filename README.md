<a name="readme-top"></a>

![GitHub tag (with filter)](https://img.shields.io/github/v/tag/K4ryuu/K4-GOTV-SwiftlyS2?style=for-the-badge&label=Version)
![GitHub Repo stars](https://img.shields.io/github/stars/K4ryuu/K4-GOTV-SwiftlyS2?style=for-the-badge)
![GitHub issues](https://img.shields.io/github/issues/K4ryuu/K4-GOTV-SwiftlyS2?style=for-the-badge)
![GitHub](https://img.shields.io/github/license/K4ryuu/K4-GOTV-SwiftlyS2?style=for-the-badge)
![GitHub all releases](https://img.shields.io/github/downloads/K4ryuu/K4-GOTV-SwiftlyS2/total?style=for-the-badge)
[![Discord](https://img.shields.io/badge/Discord-Join%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/Dwq8hncnxX)

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">KitsuneLab©</h1>
  <h3 align="center">K4 - GOTV</h3>
  <a align="center">Advanced GOTV demo recording plugin for Counter-Strike 2 with Discord webhook notifications, FTP/SFTP uploads, Mega.nz cloud storage, and database integration.</a>

  <p align="center">
    <br />
    <a href="https://github.com/K4ryuu/K4-GOTV-SwiftlyS2/releases/latest">Download</a>
    ·
    <a href="https://github.com/K4ryuu/K4-GOTV-SwiftlyS2/issues/new?assignees=K4ryuu&labels=bug&projects=&template=bug_report.md&title=%5BBUG%5D">Report Bug</a>
    ·
    <a href="https://github.com/K4ryuu/K4-GOTV-SwiftlyS2/issues/new?assignees=K4ryuu&labels=enhancement&projects=&template=feature_request.md&title=%5BREQ%5D">Request Feature</a>
  </p>
</div>

## Features

- **Automatic GOTV Recording** - Automatically record demos with configurable triggers
- **Round-based Recording** - Option to crop demos per round for easier navigation
- **Discord Integration** - Send demo notifications with customizable embeds and file attachments
- **Mega.nz Upload** - Automatically upload demos to Mega cloud storage
- **FTP/SFTP Upload** - Upload demos to your own FTP or SFTP server
- **Database Logging** - Store demo metadata in MySQL database
- **Demo Request System** - Players can request demos with `!demo` command
- **Auto Cleanup** - Automatic cleanup of old demo files
- **Retention Management** - Auto-delete uploaded files after configurable time period
- **Idle Detection** - Stop recording when server is idle

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Support My Work

I create free, open-source projects for the community. While not required, donations help me dedicate more time to development and support. Thank you!

<p align="center">
  <a href="https://paypal.me/k4ryuu"><img src="https://img.shields.io/badge/PayPal-00457C?style=for-the-badge&logo=paypal&logoColor=white" /></a>
  <a href="https://revolut.me/k4ryuu"><img src="https://img.shields.io/badge/Revolut-0075EB?style=for-the-badge&logo=revolut&logoColor=white" /></a>
</p>

### Dependencies

To use this server addon, you'll need the following dependencies installed:

- [**SwiftlyS2**](https://github.com/swiftly-solution/swiftlys2): SwiftlyS2 is a server plugin framework for Counter-Strike 2

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- REQUIREMENTS -->

## Requirements

- GOTV must be enabled on your server (`tv_enable 1`)
- For Discord file uploads: Server boost level affects max file size (25MB/50MB/100MB)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- INSTALLATION -->

## Installation

1. Install [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) on your server
2. [Download the latest release](https://github.com/K4ryuu/K4-GOTV-SwiftlyS2/releases/latest)
3. Extract to your server's `swiftlys2/plugins/` directory
4. Enable GOTV: `tv_enable 1` in your server config

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONFIGURATION -->

## Configuration

### General Settings

| Option                                              | Description                                  | Default                                         |
| --------------------------------------------------- | -------------------------------------------- | ----------------------------------------------- |
| `DatabaseConnection`                                | Database connection name for logging         | `""`                                            |
| `General.MinimumDemoDuration`                       | Minimum demo duration in seconds to save     | `5.0`                                           |
| `General.DeleteDemoAfterUpload`                     | Delete original .dem file after upload       | `true`                                          |
| `General.DeleteZippedDemoAfterUpload`               | Delete .zip file after upload                | `true`                                          |
| `General.DeleteEveryDemoFromServerAfterServerStart` | Clean all demos on map load                  | `false`                                         |
| `General.LogUploads`                                | Log upload activities                        | `true`                                          |
| `General.LogDeletions`                              | Log file deletions                           | `true`                                          |
| `General.DemoDirectory`                             | Directory for demo files (relative to csgo/) | `"demos"`                                       |
| `General.RegularFileNamingPattern`                  | File naming pattern for regular demos        | `"{fileName}_{map}_{date}_{time}"`              |
| `General.CropRoundsFileNamingPattern`               | File naming pattern for round demos          | `"{fileName}_{map}_round{round}_{date}_{time}"` |
| `General.AutoCleanupEnabled`                        | Enable automatic file cleanup                | `false`                                         |
| `General.AutoCleanupIntervalMinutes`                | Cleanup check interval                       | `60`                                            |
| `General.AutoCleanupFileAgeHours`                   | Delete files older than this                 | `48`                                            |

### Auto Record Settings

| Option                                | Description                        | Default |
| ------------------------------------- | ---------------------------------- | ------- |
| `AutoRecord.Enabled`                  | Enable automatic recording         | `false` |
| `AutoRecord.CropRounds`               | Record separate demo per round     | `false` |
| `AutoRecord.StopOnIdle`               | Stop recording when server is idle | `false` |
| `AutoRecord.RecordWarmup`             | Record during warmup               | `true`  |
| `AutoRecord.IdlePlayerCountThreshold` | Player count threshold for idle    | `0`     |
| `AutoRecord.IdleTimeSeconds`          | Seconds before considered idle     | `300`   |

### Discord Settings

| Option                      | Description                                     | Default                                |
| --------------------------- | ----------------------------------------------- | -------------------------------------- |
| `Discord.WebhookURL`        | Discord webhook URL                             | `""`                                   |
| `Discord.WebhookName`       | Webhook display name                            | `"CSGO Demo Bot"`                      |
| `Discord.WebhookAvatar`     | Webhook avatar URL                              | `""`                                   |
| `Discord.WebhookUploadFile` | Attach demo file to message                     | `true`                                 |
| `Discord.EmbedTitle`        | Embed title                                     | `"New CSGO Demo Available"`            |
| `Discord.MessageText`       | Message content                                 | `"@everyone New CSGO Demo Available!"` |
| `Discord.ServerBoost`       | Server boost level (0/2/3) for file size limits | `0`                                    |

### Mega.nz Settings

| Option                  | Description                | Default |
| ----------------------- | -------------------------- | ------- |
| `Mega.Enabled`          | Enable Mega uploads        | `false` |
| `Mega.Email`            | Mega account email         | `""`    |
| `Mega.Password`         | Mega account password      | `""`    |
| `Mega.RetentionEnabled` | Auto-delete old uploads    | `false` |
| `Mega.RetentionHours`   | Delete uploads after hours | `72`    |

### FTP/SFTP Settings

| Option                 | Description                | Default |
| ---------------------- | -------------------------- | ------- |
| `Ftp.Enabled`          | Enable FTP uploads         | `false` |
| `Ftp.Host`             | FTP server hostname        | `""`    |
| `Ftp.Port`             | FTP server port            | `21`    |
| `Ftp.Username`         | FTP username               | `""`    |
| `Ftp.Password`         | FTP password               | `""`    |
| `Ftp.RemoteDirectory`  | Remote upload directory    | `"/"`   |
| `Ftp.UseSftp`          | Use SFTP instead of FTP    | `false` |
| `Ftp.RetentionEnabled` | Auto-delete old uploads    | `false` |
| `Ftp.RetentionHours`   | Delete uploads after hours | `72`    |

### Demo Request Settings

| Option                     | Description                      | Default |
| -------------------------- | -------------------------------- | ------- |
| `DemoRequest.Enabled`      | Enable demo request system       | `false` |
| `DemoRequest.PrintAll`     | Announce requests to all players | `true`  |
| `DemoRequest.DeleteUnused` | Delete demos no one requested    | `true`  |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- COMMANDS -->

## Commands

| Command | Description                                             |
| ------- | ------------------------------------------------------- |
| `!demo` | Request current round's demo (when DemoRequest enabled) |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- FILE NAMING PATTERNS -->

## File Naming Patterns

Available placeholders for file naming:

| Placeholder     | Description                      |
| --------------- | -------------------------------- |
| `{fileName}`    | Base filename (e.g., "autodemo") |
| `{map}`         | Current map name                 |
| `{date}`        | Date (yyyy-MM-dd)                |
| `{time}`        | Time (HH-mm-ss)                  |
| `{timestamp}`   | Full timestamp (yyyyMMdd_HHmmss) |
| `{round}`       | Current round number             |
| `{playerCount}` | Number of players                |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- LICENSE -->

## License

Distributed under the GPL-3.0 License. See [`LICENSE.md`](LICENSE.md) for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
