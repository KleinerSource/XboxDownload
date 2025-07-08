using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    internal class UpdateFile
    {
        public const string website = "https://xbox.skydevil.xyz";
        public const string project = "https://github.com/skydevil88/XboxDownload";
        public static readonly string[] proxies1 = { "https://gh-proxy.com/", "https://ghproxy.net/" };
        private static readonly string[] proxies2 = { "https://pxy1.skydevil.xyz/", "https://pxy2.skydevil.xyz/", "" };

        public static async void Start(bool autoupdate, Form1 parentForm)
        {
            Properties.Settings.Default.NextUpdate = DateTime.Now.AddDays(7).Ticks;
            Properties.Settings.Default.Save();

            string tag_name = "", version = "";
            using var cts = new CancellationTokenSource();
            var regex = new Regex(@"/releases/tag/(?<tag_name>[^\d]*(?<version>\d+(\.\d+){2,3}))$", RegexOptions.Compiled);
            object lockObj = new();
            var tasks = proxies2.Select(proxy =>
                Task.Run(() =>
                {
                    string url = $"{proxy}{project}/releases/latest";
                    using var response = ClassWeb.HttpResponseMessage(url, "HEAD", null, null, null, 3000, null, cts.Token);
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        string? finalUrl = response.RequestMessage?.RequestUri?.ToString();
                        if (!string.IsNullOrEmpty(finalUrl))
                        {
                            Match result = regex.Match(finalUrl);
                            if (result.Success)
                            {
                                lock (lockObj)
                                {
                                    if (string.IsNullOrEmpty(tag_name))
                                    {
                                        tag_name = result.Groups["tag_name"].Value;
                                        version = result.Groups["version"].Value;
                                        cts.Cancel();
                                    }
                                }
                            }
                        }
                    }
                }, cts.Token)
            ).ToArray();
            await Task.WhenAll(tasks);

            if (string.IsNullOrEmpty(tag_name))
            {
                if (!autoupdate)
                {
                    parentForm.Invoke(new Action(() =>
                    {
                        MessageBox.Show("检查更新出错，请稍候再试。", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        parentForm.tsmUpdate.Enabled = true;
                    }));
                }
                return;
            }

            Version version_latest = new(version);
            if (version_latest.Major >= 3)
            {
                if (!autoupdate)
                {
                    parentForm.Invoke(new Action(() =>
                    {
                        MessageBox.Show("已有 v3 版本，请前往 GitHub 下载更新。", "软件更新", MessageBoxButtons.OK, MessageBoxIcon.None);
                    }));
                }
                return;
            }
            Version version_current = new(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version!);
            if (version_latest > version_current)
            {
                bool isUpdate = false;
                parentForm.Invoke(new Action(() =>
                {
                    isUpdate = MessageBox.Show($"已检测到新版本 {version_latest}，是否立即更新？", "软件更新", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
                    if (!isUpdate) parentForm.tsmUpdate.Enabled = true;
                }));
                if (!isUpdate) return;
            }
            else
            {
                parentForm.Invoke(new Action(() =>
                {
                    if (!autoupdate) MessageBox.Show($"软件已经是最新版本 {version_current}。", "软件更新", MessageBoxButtons.OK, MessageBoxIcon.None);
                    parentForm.tsmUpdate.Enabled = true;
                }));
                return;
            }

            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("Update", "正在下载更新包，请稍候...", "localhost", 0x008000);
            string? fastestUrl = await ClassWeb.GetFastestProxy(proxies1.Concat(proxies2).ToArray(), $"{project}/releases/download/{tag_name}/XboxDownload.zip", new() { { "Range", "bytes=0-10239" } }, 3000);
            if (fastestUrl != null)
            {
                using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(fastestUrl);
                if (response != null && response.IsSuccessStatusCode && string.Equals(response.Content.Headers.ContentType?.MediaType, "application/octet-stream") && string.Equals(response.Content.Headers.ContentDisposition?.FileName, "XboxDownload.zip", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(Form1.resourceDirectory))
                        Directory.CreateDirectory(Form1.resourceDirectory);
                    byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                    if (buffer.Length > 0)
                    {
                        string saveFilepath = Path.Combine(Form1.resourceDirectory, "XboxDownload.zip");
                        using (FileStream fs = new(saveFilepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            fs.Write(buffer, 0, buffer.Length);
                            fs.Flush();
                            fs.Close();
                        }
                        string tempDir = Path.Combine(Form1.resourceDirectory, ".Temp");
                        if (Directory.Exists(tempDir))
                            Directory.Delete(tempDir, true);
                        try
                        {
                            ZipFile.ExtractToDirectory(saveFilepath, tempDir, Encoding.GetEncoding("GBK"), true);
                        }
                        catch { }
                        if (Directory.Exists(tempDir))
                        {
                            foreach (DirectoryInfo di in new DirectoryInfo(tempDir).GetDirectories())
                            {
                                if (File.Exists(Path.Combine(di.FullName, "XboxDownload.exe")))
                                {
                                    parentForm.Invoke(new Action(() =>
                                    {
                                        if (Form1.bServiceFlag) parentForm.ButStart_Click(null, null);
                                        parentForm.notifyIcon1.Visible = false;
                                    }));
                                    string cmd = "chcp 65001\r\nchoice /t 3 /d y /n >nul\r\nxcopy \"" + di.FullName + "\" \"" + Path.GetDirectoryName(Application.ExecutablePath) + "\" /s /e /y\r\ndel /a/f/q " + saveFilepath + "\r\n\"" + Application.ExecutablePath + "\"\r\nrd /s/q " + tempDir;
                                    File.WriteAllText(Path.Combine(tempDir, "update.cmd"), cmd);
                                    using (Process p = new())
                                    {
                                        p.StartInfo.FileName = "cmd.exe";
                                        p.StartInfo.UseShellExecute = false;
                                        p.StartInfo.CreateNoWindow = true;
                                        p.StartInfo.Arguments = "/c \"" + tempDir + "\\update.cmd\"";
                                        p.Start();
                                    }
                                    Process.GetCurrentProcess().Kill();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            parentForm.Invoke(new Action(() =>
            {
                if (!autoupdate) MessageBox.Show("下载更新包出错。请稍后再试。", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                parentForm.tsmUpdate.Enabled = true;
            }));
            return;
        }

        public static async Task DownloadIP(FileInfo fi)
        {
            string url = project.Replace("github.com", "raw.githubusercontent.com") + "/refs/heads/master/IP/" + fi.Name, keyword = fi.Name[3..^4];
            using var cts = new CancellationTokenSource();
            var tasks = proxies2.Select(async proxy =>
            {
                using var response = await ClassWeb.HttpResponseMessageAsync(proxy + url, "GET", null, null, null, 6000, null, cts.Token);
                if (response != null && response.IsSuccessStatusCode)
                {
                    try
                    {
                        string html = await response.Content.ReadAsStringAsync(cts.Token);
                        return html;
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception) { }
                }
                return string.Empty;
            }).ToList();

            bool isOK = false;
            while (tasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);
                string html = await completedTask;
                if (html.StartsWith(keyword))
                {
                    cts.Cancel();
                    if (fi.DirectoryName != null && !Directory.Exists(fi.DirectoryName))
                        Directory.CreateDirectory(fi.DirectoryName);
                    using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using StreamWriter sw = new(fs);
                        sw.Write(html);
                    }
                    fi.Refresh();
                    isOK = true;
                }
            }

            if (!isOK)
            {
                string html = string.Empty;
                using var response = await ClassWeb.HttpResponseMessageAsync($"{project.Replace("github.com", "testingcf.jsdelivr.net/gh")}/IP/{fi.Name}");
                if (response != null && response.IsSuccessStatusCode)
                {
                    try
                    {
                        html = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception) { }
                }
                if (html.StartsWith(keyword))
                {
                    if (fi.DirectoryName != null && !Directory.Exists(fi.DirectoryName))
                        Directory.CreateDirectory(fi.DirectoryName);
                    using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using StreamWriter sw = new(fs);
                        sw.Write(html);
                    }
                    fi.Refresh();
                }
            }
        }
    }
}
