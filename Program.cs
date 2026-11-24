// Wallet Inventory — detection core demo
//
// Purpose: list crypto wallet software INSTALLED on this Windows PC.
// Scope (deliberate, and enforced by the design):
//   * Reads install metadata ONLY:
//       - Registry uninstall entries (display name / install date)
//       - Browser profile folders: the NAMES of extension directories
//         (an extension's ID), never their contents
//   * NEVER opens wallet software.
//   * NEVER reads wallet data stores (no Local Extension Settings,
//     no IndexedDB contents, no wallet files).
//   * NEVER touches seed phrases, private keys, or balances.
//   * No network calls. No telemetry. Local console output only.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

internal static class Program
{
    // Known Chromium extension IDs of major wallet extensions.
    // The full project ships this as an editable catalog file.
    private static readonly Dictionary<string, string> ExtensionCatalog =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["nkbihfbeogaeaoehlefnkodbefgpgknn"] = "MetaMask",
            ["bfnaelmomeimhlpmgjnjophhpkkoljpa"] = "Phantom",
            ["acmacodkjbdgmoleebolmdjonilkdbch"] = "Rabby Wallet",
            ["hnfanknocfeofbddgcijnmhnfnkdnaad"] = "Coinbase Wallet",
            ["mcohilncbfahbmgdjkbpemcciiolgcge"] = "OKX Wallet",
            ["egjidjbpglichdcondbcbdnbeeppgdph"] = "Trust Wallet",
            ["dmkamcknogkgcdfhhbddcghachkejeap"] = "Keplr",
            ["aflkmfhebedbjioipglgcbcmnbpgliof"] = "Backpack",
        };

    // Desktop wallet applications, matched against registry DisplayName.
    private static readonly string[] DesktopWalletNames =
    {
        "Exodus", "Electrum", "Atomic Wallet", "Ledger Live",
        "Trezor Suite", "Wasabi Wallet", "Sparrow", "Bitcoin Core",
        "MetaMask", "Phantom", "Trust Wallet",
    };

    private sealed record Finding(
        string Wallet, string Kind, string Where, string Profile, DateTime? Installed);

    private static void Main()
    {
        var findings = new List<Finding>();
        findings.AddRange(ScanDesktopApps());
        findings.AddRange(ScanBrowserExtensions());

        Console.WriteLine("Crypto wallet software installed on this PC");
        Console.WriteLine("(install metadata only — no wallet data is read)");
        Console.WriteLine(new string('-', 72));
        if (findings.Count == 0)
        {
            Console.WriteLine("No known wallet software detected.");
            return;
        }
        foreach (var f in findings)
        {
            Console.WriteLine(
                $"{f.Wallet,-16} {f.Kind,-9} {f.Where,-8} profile: {f.Profile,-12} installed: " +
                (f.Installed?.ToString("yyyy-MM-dd") ?? "unknown"));
        }
    }

    // --- Desktop applications: registry uninstall entries -------------------
    private static IEnumerable<Finding> ScanDesktopApps()
    {
        var roots = new[]
        {
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        };
        foreach (var (hive, path) in roots)
        {
            using var key = hive.OpenSubKey(path);
            if (key is null) continue;
            foreach (var subName in key.GetSubKeyNames())
            {
                using var sub = key.OpenSubKey(subName);
                var display = sub?.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(display)) continue;
                foreach (var known in DesktopWalletNames)
                {
                    if (!display.Contains(known, StringComparison.OrdinalIgnoreCase)) continue;
                    DateTime? installed = null;
                    if (sub!.GetValue("InstallDate") is string raw &&
                        DateTime.TryParseExact(raw, "yyyyMMdd", null,
                            System.Globalization.DateTimeStyles.None, out var d))
                        installed = d;
                    yield return new Finding(display, "Desktop", "Windows", "-", installed);
                    break;
                }
            }
        }
    }

    // --- Browser extensions: profile folder names only ----------------------
    private static IEnumerable<Finding> ScanBrowserExtensions()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var browsers = new[]
        {
            ("Chrome", Path.Combine(local, @"Google\Chrome\User Data")),
            ("Brave",  Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data")),
            ("Edge",   Path.Combine(local, @"Microsoft\Edge\User Data")),
        };
        foreach (var (browser, root) in browsers)
        {
            if (!Directory.Exists(root)) continue;
            foreach (var profileDir in Directory.EnumerateDirectories(root))
            {
                var extRoot = Path.Combine(profileDir, "Extensions");
                if (!Directory.Exists(extRoot)) continue;
                foreach (var idDir in Directory.EnumerateDirectories(extRoot))
                {
                    var id = Path.GetFileName(idDir);
                    if (!ExtensionCatalog.TryGetValue(id, out var wallet)) continue;
                    // Install date = creation time of the extension's folder.
                    var created = Directory.GetCreationTime(idDir);
                    yield return new Finding(
                        wallet, "Extension", browser,
                        Path.GetFileName(profileDir), created);
                }
            }
        }
    }
}
