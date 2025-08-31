using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

class TrayApp : Form
{
    static string configFile = "fg_config.txt";
    static string freegatePath = "";
    static NotifyIcon trayIcon;
    static bool lastStatus = false;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (File.Exists(configFile))
            freegatePath = File.ReadAllText(configFile).Trim();

        if (string.IsNullOrEmpty(freegatePath) || !File.Exists(freegatePath))
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Executable (*.exe)|*.exe";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                freegatePath = ofd.FileName;
                File.WriteAllText(configFile, freegatePath);
            }
            else return;
        }

        trayIcon = new NotifyIcon();
        trayIcon.Icon = System.Drawing.SystemIcons.Information;
        trayIcon.Visible = true;

        var contextMenu = new ContextMenu();
        contextMenu.MenuItems.Add("تغییر مسیر Freegate", (s, e) => ChangePath());
        contextMenu.MenuItems.Add("خروج", (s, e) => { trayIcon.Visible = false; Application.Exit(); });
        trayIcon.ContextMenu = contextMenu;

        Thread watcher = new Thread(() => RunWatcher());
        watcher.IsBackground = true;
        watcher.Start();

        Application.Run();
    }

    static void RunWatcher()
    {
        while (true)
        {
            bool online = CheckInternet();

            if (online && !lastStatus)
                StartFreegate();
            else if (!online && lastStatus)
                StopFreegate();

            lastStatus = online;

            string status = online ? "اینترنت: وصل" : "اینترنت: قطع";
            status += Environment.NewLine + (IsRunning("fg805p") ? "Freegate: اجرا" : "Freegate: بسته");
            trayIcon.Text = status;

            trayIcon.Icon = online
                ? (IsRunning("fg805p") ? System.Drawing.SystemIcons.Shield : System.Drawing.SystemIcons.Information)
                : System.Drawing.SystemIcons.Error;

            Thread.Sleep(online ? 5000 : 2000);
        }
    }

    static bool CheckInternet()
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(1);
                var result = client.GetAsync("http://clients3.google.com/generate_204").Result;
                return result.StatusCode == System.Net.HttpStatusCode.NoContent;
            }
        }
        catch { return false; }
    }

    static void StartFreegate()
    {
        if (!IsRunning("fg805p"))
            Process.Start(freegatePath);
    }

    static void StopFreegate()
    {
        foreach (var proc in Process.GetProcessesByName("fg805p"))
            proc.Kill();
    }

    static bool IsRunning(string name)
    {
        return Process.GetProcessesByName(name).Length > 0;
    }

    static void ChangePath()
    {
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "Executable (*.exe)|*.exe";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            freegatePath = ofd.FileName;
            File.WriteAllText(configFile, freegatePath);
        }
    }

    static void EnableStartup()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
        {
            key.SetValue("FGWatcher", Application.ExecutablePath);
        }
    }
}
