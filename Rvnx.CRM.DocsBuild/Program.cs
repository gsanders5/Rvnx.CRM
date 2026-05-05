using Microsoft.Playwright;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Rvnx.CRM.DocsBuild;

internal static partial class Program
{
    private const string ApiPort = "5443";
    private const string WebPort = "5444";
    private const string LocalSettingsBackupSuffix = ".docsbuild-bak";
    private static readonly string ApiUrlBase = $"https://localhost:{ApiPort}";
    private static readonly string WebUrlBase = $"https://localhost:{WebPort}";

    private enum ScreenshotTarget { Api, Web }
    private enum ScreenshotMode { Viewport, FullPage }
    private enum Theme { Light, Dark }

    private static async Task<int> Main()
    {
        string repoRoot = LocateRepoRoot();
        string readmePath = Path.Combine(repoRoot, "README.md");

        // Recover from a prior crashed run that left the developer's settings file renamed.
        RestoreLocalSettingsIfStranded(repoRoot);

        List<ScreenshotDirective> directives = ParseDirectives(readmePath);
        if (directives.Count == 0)
        {
            Console.Error.WriteLine("No SCREENSHOT directives found in README.md.");
            return 1;
        }

        Console.WriteLine($"Parsed {directives.Count} screenshot directive(s).");

        EnsurePlaywrightBrowsers();

        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        List<ScreenshotDirective> apiDirectives = [.. directives.Where(d => d.Target == ScreenshotTarget.Api)];
        if (apiDirectives.Count > 0)
        {
            await RunApiTargetAsync(browser, repoRoot, apiDirectives);
        }

        List<ScreenshotDirective> webDirectives = [.. directives.Where(d => d.Target == ScreenshotTarget.Web)];
        if (webDirectives.Count > 0)
        {
            await RunWebTargetAsync(browser, repoRoot, webDirectives);
        }

        Console.WriteLine("Done.");
        return 0;
    }

    private static async Task RunApiTargetAsync(IBrowser browser, string repoRoot, List<ScreenshotDirective> apiDirectives)
    {
        Process api = StartApi(repoRoot);
        try
        {
            await WaitForReadyAsync($"{ApiUrlBase}/swagger/v1/swagger.json", TimeSpan.FromSeconds(60));
            foreach (ScreenshotDirective d in apiDirectives)
            {
                await CaptureAsync(browser, d, ApiUrlBase, repoRoot);
            }
        }
        finally
        {
            StopProcess(api);
        }
    }

    private static async Task RunWebTargetAsync(IBrowser browser, string repoRoot, List<ScreenshotDirective> webDirectives)
    {
        // Web/Program.cs adds appsettings.Local.json AFTER the default config chain, so the
        // developer's local file would override --Authentication:Enabled=false and the
        // connection-string CLI args. Hide it for the run, restore in finally below.
        string sourceDbPath = Path.Combine(repoRoot, ".demo", "db", "rvnx-crm-demo.db");
        string tempDbPath = Path.Combine(Path.GetTempPath(), $"rvnx-crm-demo-{Guid.NewGuid():N}.db");
        string localSettingsPath = Path.Combine(repoRoot, "Rvnx.CRM.Web", "appsettings.Local.json");
        string hiddenSettingsPath = localSettingsPath + LocalSettingsBackupSuffix;
        bool localSettingsHidden = false;
        Process? web = null;

        try
        {
            File.Copy(sourceDbPath, tempDbPath);
            Console.WriteLine($"Copied demo DB → {tempDbPath}");

            if (File.Exists(localSettingsPath))
            {
                // Refuse to clobber an existing backup: it would be a stranded copy from a prior
                // crashed run, and overwriting it permanently loses the original Local.json.
                if (File.Exists(hiddenSettingsPath))
                {
                    throw new InvalidOperationException(
                        $"Refusing to overwrite stranded backup at {hiddenSettingsPath}. " +
                        $"Inspect both files and delete or rename one manually before re-running.");
                }

                File.Move(localSettingsPath, hiddenSettingsPath);
                localSettingsHidden = true;
                Console.WriteLine($"Hid {localSettingsPath} → {hiddenSettingsPath}");
            }

            web = StartWeb(repoRoot, tempDbPath);
            await WaitForReadyAsync($"{WebUrlBase}/Home/Index", TimeSpan.FromSeconds(60));
            foreach (ScreenshotDirective d in webDirectives)
            {
                await CaptureAsync(browser, d, WebUrlBase, repoRoot);
            }
        }
        finally
        {
            // Stop the Web process before deleting the temp DB so SQLite releases its lock.
            if (web != null)
            {
                StopProcess(web);
            }
            TryDelete(tempDbPath);
            if (localSettingsHidden)
            {
                File.Move(hiddenSettingsPath, localSettingsPath);
                Console.WriteLine($"Restored {localSettingsPath}");
            }
        }
    }

    private static async Task CaptureAsync(IBrowser browser, ScreenshotDirective directive, string urlBase, string repoRoot)
    {
        Console.WriteLine($"Capturing {directive.OutputPath} from {directive.Path}");

        await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = directive.Width, Height = directive.Height },
            DeviceScaleFactor = 2,
            IgnoreHTTPSErrors = true
        });

        if (directive.Theme == Theme.Dark)
        {
            // _Layout.cshtml reads localStorage.theme on first script execution and applies
            // data-mdb-theme to <html>. Seed the value before any page script runs so the
            // page renders dark from first paint.
            await context.AddInitScriptAsync("localStorage.setItem('theme', 'dark');");
        }

        IPage page = await context.NewPageAsync();

        string url = urlBase + directive.Path;
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        if (!string.IsNullOrEmpty(directive.Click))
        {
            await page.Locator(directive.Click).ClickAsync();
        }

        if (directive.WaitMs > 0)
        {
            await page.WaitForTimeoutAsync(directive.WaitMs);
        }

        string absoluteOut = Path.GetFullPath(Path.Combine(repoRoot, directive.OutputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteOut)!);

        if (!string.IsNullOrEmpty(directive.Selector))
        {
            ILocator element = page.Locator(directive.Selector);
            await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await element.ScreenshotAsync(new LocatorScreenshotOptions
            {
                Path = absoluteOut,
                Type = ScreenshotType.Png
            });
        }
        else
        {
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = absoluteOut,
                FullPage = directive.Mode == ScreenshotMode.FullPage,
                Type = ScreenshotType.Png
            });
        }

        Console.WriteLine($"  → wrote {absoluteOut}");
    }

    private static Process StartApi(string repoRoot)
    {
        return StartDotnetProject(repoRoot, project: "Rvnx.CRM.API", urls: ApiUrlBase, extraArgs: []);
    }

    private static Process StartWeb(string repoRoot, string tempDbPath)
    {
        // Auth must be off: the demo DB has GroupId=NULL on every row, but an authenticated
        // user has a non-null GroupId, so the global query filter would drop everything.
        return StartDotnetProject(
            repoRoot,
            project: "Rvnx.CRM.Web",
            urls: WebUrlBase,
            extraArgs:
            [
                $"--ConnectionStrings:DefaultConnection=Data Source={tempDbPath}",
                "--Authentication:Enabled=false",
                "--Immich:Enabled=false",
                "--DatabaseProvider=SQLite"
            ]);
    }

    private static Process StartDotnetProject(string repoRoot, string project, string urls, string[] extraArgs)
    {
        // --no-launch-profile is required so launchSettings.json doesn't override --urls.
        List<string> args = ["run", "--project", project, "--no-build", "--no-launch-profile", "--urls", urls];
        if (extraArgs.Length > 0)
        {
            args.Add("--");
            args.AddRange(extraArgs);
        }

        ProcessStartInfo psi = new()
        {
            FileName = "dotnet",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            Environment = { ["ASPNETCORE_ENVIRONMENT"] = "Development" }
        };
        foreach (string a in args)
        {
            psi.ArgumentList.Add(a);
        }

        Process p = new() { StartInfo = psi };
        p.OutputDataReceived += (_, e) => { if (e.Data != null) { Console.WriteLine($"[{project}] {e.Data}"); } };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) { Console.Error.WriteLine($"[{project}] {e.Data}"); } };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        return p;
    }

    private static void StopProcess(Process p)
    {
        if (p.HasExited)
        {
            return;
        }

        Console.WriteLine($"Stopping process {p.Id}...");
        try
        {
            p.Kill(entireProcessTree: true);
            p.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to stop process: {ex.Message}");
        }
    }

    private static void TryDelete(string path)
    {
        try
        { File.Delete(path); }
        catch (Exception ex) { Console.Error.WriteLine($"Failed to delete {path}: {ex.Message}"); }
    }

    private static void RestoreLocalSettingsIfStranded(string repoRoot)
    {
        string localSettingsPath = Path.Combine(repoRoot, "Rvnx.CRM.Web", "appsettings.Local.json");
        string strandedBackup = localSettingsPath + LocalSettingsBackupSuffix;

        if (!File.Exists(strandedBackup))
        {
            return;
        }

        if (File.Exists(localSettingsPath))
        {
            // Both present means the dev re-created Local.json after a crash. Don't auto-merge —
            // leave both files in place so the dev can reconcile, but warn loudly. The Web target
            // will refuse to start (see RunWebTargetAsync) until one of them is gone.
            Console.Error.WriteLine(
                $"WARNING: stranded backup {strandedBackup} exists alongside {localSettingsPath}. " +
                $"Inspect both files and delete or rename one before running the Web target.");
            return;
        }

        File.Move(strandedBackup, localSettingsPath);
        Console.WriteLine($"Recovered stranded {localSettingsPath} from prior crashed run.");
    }

    private static async Task WaitForReadyAsync(string probeUrl, TimeSpan timeout)
    {
        // Dev cert is self-signed; trust it for the readiness probe only.
        using HttpClientHandler handler = new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using HttpClient http = new(handler) { Timeout = TimeSpan.FromSeconds(2) };
        DateTime deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                HttpResponseMessage response = await http.GetAsync(probeUrl);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ready: {probeUrl}");
                    return;
                }
            }
            catch
            {
                // Server not yet listening; keep polling.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Service did not become ready within {timeout.TotalSeconds}s: {probeUrl}");
    }

    private static void EnsurePlaywrightBrowsers()
    {
        // Skip the install entrypoint when Chromium is already cached. The install
        // entrypoint spawns a child process and on Linux can attempt sudo for --with-deps,
        // so running it on every invocation costs seconds and may prompt for credentials.
        // PLAYWRIGHT_BROWSERS_PATH lets users override the cache location.
        string cacheRoot = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH") is { Length: > 0 } overridePath
            ? overridePath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright");
        if (Directory.Exists(cacheRoot) && Directory.EnumerateDirectories(cacheRoot, "chromium-*").Any())
        {
            return;
        }

        int exitCode = Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"playwright install failed with exit code {exitCode}");
        }
    }

    private static List<ScreenshotDirective> ParseDirectives(string markdownPath)
    {
        string content = File.ReadAllText(markdownPath);
        List<ScreenshotDirective> result = [];

        foreach (Match m in DirectiveRegex().Matches(content))
        {
            string body = m.Groups[1].Value;
            Dictionary<string, string> kv = body
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1]);

            if (!kv.TryGetValue("target", out string? targetRaw) ||
                !kv.TryGetValue("path", out string? path) ||
                !kv.TryGetValue("out", out string? output))
            {
                Console.Error.WriteLine($"Skipping malformed directive (missing target/path/out): {m.Value}");
                continue;
            }

            if (!TryParseTarget(targetRaw, out ScreenshotTarget target))
            {
                Console.Error.WriteLine($"Skipping directive with unknown target='{targetRaw}': {m.Value}");
                continue;
            }

            string modeRaw = kv.GetValueOrDefault("mode", "viewport");
            if (!TryParseMode(modeRaw, out ScreenshotMode mode))
            {
                Console.Error.WriteLine($"Skipping directive with unknown mode='{modeRaw}': {m.Value}");
                continue;
            }

            string themeRaw = kv.GetValueOrDefault("theme", "light");
            if (!TryParseTheme(themeRaw, out Theme theme))
            {
                Console.Error.WriteLine($"Skipping directive with unknown theme='{themeRaw}': {m.Value}");
                continue;
            }

            int waitMs = int.TryParse(kv.GetValueOrDefault("wait"), out int w) ? w : 0;
            string? selector = kv.GetValueOrDefault("selector");
            string? click = kv.GetValueOrDefault("click");
            int width = int.TryParse(kv.GetValueOrDefault("width"), out int wd) ? wd : 1440;
            int height = int.TryParse(kv.GetValueOrDefault("height"), out int ht) ? ht : 900;

            result.Add(new ScreenshotDirective(target, path, mode, theme, waitMs, output, selector, click, width, height));
        }

        return result;
    }

    private static bool TryParseTarget(string raw, out ScreenshotTarget target)
    {
        switch (raw)
        {
            case "api":
                target = ScreenshotTarget.Api;
                return true;
            case "web":
                target = ScreenshotTarget.Web;
                return true;
            default:
                target = default;
                return false;
        }
    }

    private static bool TryParseMode(string raw, out ScreenshotMode mode)
    {
        switch (raw)
        {
            case "viewport":
                mode = ScreenshotMode.Viewport;
                return true;
            case "full_page":
                mode = ScreenshotMode.FullPage;
                return true;
            default:
                mode = default;
                return false;
        }
    }

    private static bool TryParseTheme(string raw, out Theme theme)
    {
        switch (raw)
        {
            case "light":
                theme = Theme.Light;
                return true;
            case "dark":
                theme = Theme.Dark;
                return true;
            default:
                theme = default;
                return false;
        }
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Rvnx.CRM.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (missing Rvnx.CRM.slnx).");
    }

    [GeneratedRegex(@"<!--\s*SCREENSHOT:\s*(.+?)\s*-->", RegexOptions.Compiled)]
    private static partial Regex DirectiveRegex();

    private sealed record ScreenshotDirective(
        ScreenshotTarget Target,
        string Path,
        ScreenshotMode Mode,
        Theme Theme,
        int WaitMs,
        string OutputPath,
        string? Selector,
        string? Click,
        int Width,
        int Height);
}
