using System.Data;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Management;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NetFwTypeLib;

namespace XboxDownload
{
    public partial class Form1 : Form
    {
        internal static bool bServiceFlag = false, bAutoStartup = false, bIPv6Support = false;
        internal readonly static string resourceDirectory = Path.Combine(Application.StartupPath, "Resource");
        internal static List<Market> lsMarket = new();
        internal static float dpiFactor = 1;
        internal static JsonSerializerOptions jsOptions = new() { PropertyNameCaseInsensitive = true };
        internal static DataTable dtHosts = new("Hosts"), dtDoHServer = new("DoH");
        private readonly DnsListen dnsListen;
        private readonly HttpListen httpListen;
        private readonly HttpsListen httpsListen;
        private readonly ToolTip toolTip1 = new()
        {
            AutoPopDelay = 30000,
            IsBalloon = true
        };

        public Form1()
        {
            InitializeComponent();

            Form1.dpiFactor = Environment.OSVersion.Version.Major >= 10 ? CreateGraphics().DpiX / 96f : Program.Utility.DpiX / 96f;
            if (Form1.dpiFactor > 1)
            {
                foreach (ColumnHeader col in lvLog.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                dgvIpList.RowHeadersWidth = (int)(dgvIpList.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dgvIpList.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                dgvHosts.RowHeadersWidth = (int)(dgvHosts.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dgvHosts.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                dgvDevice.RowHeadersWidth = (int)(dgvDevice.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dgvDevice.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                foreach (ColumnHeader col in lvGame.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
            }

            ClassWeb.HttpClientFactory();
            dnsListen = new DnsListen(this);
            httpListen = new HttpListen(this);
            httpsListen = new HttpsListen(this);

            toolTip1.SetToolTip(this.labelDNS, "���� DNS ������\n114.114.114.114 (114)\n180.76.76.76 (�ٶ�)\n223.5.5.5 (����)\n119.29.29.29 (��Ѷ)\n208.67.220.220 (OpenDns)\n8.8.8.8 (Google)\n168.126.63.1 (����)");
            toolTip1.SetToolTip(this.labelCom, "��������com��Ϸ��������\nxvcf1.xboxlive.com\nxvcf2.xboxlive.com\nassets1.xboxlive.com\nassets2.xboxlive.com\nd1.xboxlive.com\nd2.xboxlive.com\ndlassets.xboxlive.com\ndlassets2.xboxlive.com\n\n������������ʹ�� cn IP");
            toolTip1.SetToolTip(this.labelCn, "��������cn��Ϸ��������\nassets1.xboxlive.cn\nassets2.xboxlive.cn\nd1.xboxlive.cn\nd2.xboxlive.cn");
            toolTip1.SetToolTip(this.labelCn2, "��������cn��Ϸ��������\ndlassets.xboxlive.cn\ndlassets2.xboxlive.cn\n\nע��XboxOne��������Ϸ����������\nPC����������Ϸ������ʹ�ô�������");
            toolTip1.SetToolTip(this.labelApp, "��������Ӧ����������\ndl.delivery.mp.microsoft.com\ntlu.dl.delivery.mp.microsoft.com\n*.dl.delivery.mp.microsoft.com");
            toolTip1.SetToolTip(this.labelPS, "����������Ϸ��������\ngst.prod.dl.playstation.net\ngs2.ww.prod.dl.playstation.net\nzeus.dl.playstation.net\nares.dl.playstation.net");
            toolTip1.SetToolTip(this.labelNS, "����������Ϸ��������\natum.hac.lp1.d4c.nintendo.net\nnemof.p01.lp1.nemo.srv.nintendo.net\nnemof.hac.lp1.nemo.srv.nintendo.net\nctest-dl.p01.lp1.ctest.srv.nintendo.net\nctest-ul.p01.lp1.ctest.srv.nintendo.net\nctest-dl-lp1.cdn.nintendo.net\nctest-ul-lp1.cdn.nintendo.net");
            toolTip1.SetToolTip(this.labelEA, "����������Ϸ��������\norigin-a.akamaihd.net");
            toolTip1.SetToolTip(this.labelBattle, "����������Ϸ��������\ndownloader.battle.net\nblzddist1-a.akamaihd.net\nus.cdn.blizzard.com\neu.cdn.blizzard.com\nkr.cdn.blizzard.com\nlevel3.blizzard.com\nblizzard.gcdn.cloudn.co.kr\n\n#���׹���(У԰����ָ��Akamai IPv6��������)\n*.necdn.leihuo.netease.com");
            toolTip1.SetToolTip(this.labelEpic, "����������Ϸ��������\nepicgames-download1-1251447533.file.myqcloud.com\nepicgames-download1.akamaized.net\ndownload.epicgames.com\nfastly-download.epicgames.com\ncloudflare.epicgamescdn.com\n\n��������ʹ�ù���CDN���ٶȲ�������ѡ�� Akamai CDN");
            toolTip1.SetToolTip(this.labelUbi, "����������Ϸ��������\nuplaypc-s-ubisoft.cdn.ubionline.com.cn\nuplaypc-s-ubisoft.cdn.ubi.com\nubisoftconnect.cdn.ubi.com\n\nע��XDefiant(�������)��֧��ʹ�ù���CDN��\n�ɹ�ѡ\"�Զ���ѡ Akamai IP\"ʹ�ù���CDN��");
            toolTip1.SetToolTip(this.ckbDoH, "Ĭ��ʹ�� ������DoH(����DNS) ��������IP��\n��ֹ����DNS���������ٳ���Ⱦ��\nPC�û�ʹ�ô˹��ܣ���Ҫ��ѡ�����ñ��� DNS��\n\nע�������������Բ���ѡ��");
            toolTip1.SetToolTip(this.ckbSetDns, "��ʼ�������ѵ���DNS����Ϊ����IP��ֹͣ������ָ�Ĭ�����ã�\nPC�û����鹴ѡ�������û��������á�\n\nע������˳�Xbox�������ֺ�û���磬�����Աߡ��޸�����");
            toolTip1.SetToolTip(this.ckbBetterAkamaiIP, "�Զ��� Akamai ��ѡ IP �б����ҳ������ٶ����� IPv4 �ڵ�\n֧�� Xbox��PS��NS��EA��ս����EPIC�����̡�ȭͷ��Ϸ��Rockstar��Spotify...\nѡ�к���ʱ�����Զ���IP��Xbox��PS��ʹ�ù���IP��\nͬʱ���ܽ��Xbox��װֹͣ��������Ϸ����CDNû����������������\n\n��ʾ��\n����IP��Xbox��ս�������� ȭͷ��Ϸ �ͻ�����Ҫ��ͣ���أ�Ȼ�����»ָ���װ��\nEA app��Epic�ͻ��������޸�/������������Ҫ�ȴ�DNS�������(100��)��");

            tbDnsIP.Text = Properties.Settings.Default.DnsIP;
            tbComIP.Text = Properties.Settings.Default.ComIP;
            ckbGameLink.Checked = Properties.Settings.Default.GameLink;
            tbCnIP.Text = Properties.Settings.Default.CnIP;
            tbCnIP2.Text = Properties.Settings.Default.CnIP2;
            tbAppIP.Text = Properties.Settings.Default.AppIP;
            tbPSIP.Text = Properties.Settings.Default.PSIP;
            tbNSIP.Text = Properties.Settings.Default.NSIP;
            ckbNSBrowser.Checked = Properties.Settings.Default.NSBrowser;
            tbEAIP.Text = Properties.Settings.Default.EAIP;
            tbBattleIP.Text = Properties.Settings.Default.BattleIP;
            ckbBattleNetease.Checked = Properties.Settings.Default.BattleNetease;
            tbEpicIP.Text = Properties.Settings.Default.EpicIP;
            if (Properties.Settings.Default.EpicCDN) rbEpicCDN1.Checked = true;
            else rbEpicCDN2.Checked = true;
            tbUbiIP.Text = Properties.Settings.Default.UbiIP;
            ckbTruncation.Checked = Properties.Settings.Default.Truncation;
            ckbLocalUpload.Checked = Properties.Settings.Default.LocalUpload;
            if (string.IsNullOrEmpty(Properties.Settings.Default.LocalPath))
                Properties.Settings.Default.LocalPath = Path.Combine(Application.StartupPath, "Upload");
            tbLocalPath.Text = Properties.Settings.Default.LocalPath;
            cbListenIP.SelectedIndex = Properties.Settings.Default.ListenIP;
            ckbDnsService.Checked = Properties.Settings.Default.DnsService;
            ckbHttpService.Checked = Properties.Settings.Default.HttpService;
            ckbDoH.Checked = Properties.Settings.Default.DoH;
            ckbDisableIPv6DNS.Checked = Properties.Settings.Default.DisableIPv6DNS;
            ckbSetDns.Checked = Properties.Settings.Default.SetDns;
            ckbMicrosoftStore.Checked = Properties.Settings.Default.MicrosoftStore;
            ckbEAStore.Checked = Properties.Settings.Default.EAStore;
            ckbBattleStore.Checked = Properties.Settings.Default.BattleStore;
            ckbEpicStore.Checked = Properties.Settings.Default.EpicStore;
            ckbUbiStore.Checked = Properties.Settings.Default.UbiStore;
            ckbSniProxy.Checked = Properties.Settings.Default.SniProxy;
            ckbRecordLog.Checked = Properties.Settings.Default.RecordLog;
            tbCdnAkamai.Text = Properties.Settings.Default.IpsAkamai;

            string dohserverFilePath = Path.Combine(resourceDirectory, "DohServer.json");
            if (File.Exists(dohserverFilePath))
            {
                JsonDocument? jsDoH = null;
                try
                {
                    jsDoH = JsonDocument.Parse(File.ReadAllText(dohserverFilePath));
                }
                catch { }
                if (jsDoH != null)
                {
                    foreach (JsonElement arr in jsDoH.RootElement.EnumerateArray())
                    {
                        string name = string.Empty, url = string.Empty, host = string.Empty;
                        if (arr.TryGetProperty("name", out JsonElement jeName)) name = jeName.ToString();
                        if (arr.TryGetProperty("url", out JsonElement jeUrl)) url = jeUrl.ToString();
                        if (arr.TryGetProperty("host", out JsonElement jeHost)) host = jeHost.ToString();
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
                        {
                            int originalRows = DnsListen.dohs.GetLength(0);
                            int originalColumns = DnsListen.dohs.GetLength(1);
                            int newRows = originalRows + 1;
                            int newColumns = originalColumns;
                            string[,] newArray = new string[newRows, newColumns];
                            for (int i = 0; i < originalRows; i++)
                            {
                                for (int j = 0; j < originalColumns; j++)
                                {
                                    newArray[i, j] = DnsListen.dohs[i, j];
                                }
                            }
                            newArray[originalRows, 0] = name;
                            newArray[originalRows, 1] = url;
                            newArray[originalRows, 2] = host;
                            DnsListen.dohs = newArray;
                        }
                    }
                }
            }

            int iDohServer = Properties.Settings.Default.DoHServer >= DnsListen.dohs.GetLongLength(0) ? 0 : Properties.Settings.Default.DoHServer;
            DnsListen.dohServer.Website = DnsListen.dohs[iDohServer, 1];
            DnsListen.dohServer.Headers = new() { { "Accept", "application/dns-json" } };
            if (!string.IsNullOrEmpty(DnsListen.dohs[iDohServer, 2])) DnsListen.dohServer.Headers.Add("Host", DnsListen.dohs[iDohServer, 2]);

            rbEpicCDN1.CheckedChanged += RbCDN_CheckedChanged;
            rbEpicCDN2.CheckedChanged += RbCDN_CheckedChanged;
            ckbRecordLog.CheckedChanged += new EventHandler(CkbRecordLog_CheckedChanged);

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && !x.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (adapters.Length == 0) adapters = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                UnicastIPAddressInformationCollection ipCollection = adapterProperties.UnicastAddresses;
                foreach (UnicastIPAddressInformation ipadd in ipCollection)
                {
                    if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ComboboxItem item = new()
                        {
                            Text = ipadd.Address.ToString(),
                            Value = adapter
                        };
                        cbLocalIP.Items.Add(item);
                    }
                }
            }
            if (cbLocalIP.Items.Count >= 1)
            {
                int index = 0;
                if (!string.IsNullOrEmpty(Properties.Settings.Default.LocalIP))
                {
                    for (int i = 0; i < cbLocalIP.Items.Count; i++)
                    {
                        string ip = cbLocalIP.Items[i].ToString() ?? string.Empty;
                        if (Properties.Settings.Default.LocalIP == ip)
                        {
                            index = i;
                            break;
                        }
                        else if (Properties.Settings.Default.LocalIP.StartsWith(Regex.Replace(ip, @"\d+$", "")))
                        {
                            index = i;
                        }
                    }
                }
                cbLocalIP.SelectedIndex = index;
            }

            tbHosts1Akamai.Text = Regex.Replace(Properties.Resource.Akamai, "\r?\n", Environment.NewLine);
            string akamaiFilePath = Path.Combine(resourceDirectory, "Akamai.txt");
            if (File.Exists(akamaiFilePath))
            {
                tbHosts2Akamai.Text = File.ReadAllText(akamaiFilePath).Trim() + "\r\n";
            }

            cbHosts.SelectedIndex = 0;
            cbSpeedTestTimeOut.SelectedIndex = 1;
            cbImportIP.SelectedIndex = 0;

            dtHosts.Columns.Add("Enable", typeof(Boolean));
            dtHosts.Columns.Add("HostName", typeof(String));
            dtHosts.Columns.Add("IP", typeof(String));
            dtHosts.Columns.Add("Remark", typeof(String));
            string hostsFilePath = Path.Combine(resourceDirectory, "Hosts.xml");
            if (File.Exists(hostsFilePath))
            {
                try
                {
                    dtHosts.ReadXml(hostsFilePath);
                }
                catch { }
                dtHosts.AcceptChanges();
            }
            dgvHosts.DataSource = dtHosts;

            dtDoHServer.Columns.Add("Enable", typeof(Boolean));
            dtDoHServer.Columns.Add("Host", typeof(String));
            dtDoHServer.Columns.Add("DoHServer", typeof(Int32));
            dtDoHServer.Columns.Add("Remark", typeof(String));
            string dohFilePath = Path.Combine(resourceDirectory, "DoH.xml");
            if (File.Exists(dohFilePath))
            {
                try
                {
                    dtDoHServer.ReadXml(dohFilePath);
                }
                catch { }
                int length = (int)DnsListen.dohs.GetLongLength(0);
                foreach (DataRow row in dtDoHServer.Rows)
                {
                    if (int.TryParse(row["DoHServer"].ToString(), out int index) && index >= length)
                        row["DoHServer"] = 0;
                }
                dtDoHServer.AcceptChanges();
            }
            DnsListen.SetDoHServer();

            Form1.lsMarket.AddRange((new List<Market>
            {
                new("Taiwan", "̨��", "TW", "zh-TW"),
                new("Hong Kong SAR", "���", "HK", "zh-HK"),
                new("Singapore", "�¼���", "SG", "en-SG"),
                new("Korea", "����", "KR", "ko-KR"),
                new("Japan", "�ձ�", "JP", "ja-JP"),
                new("United States","����", "US", "en-US"),

                new("Argentina", "����͢", "AR", "es-AR"),
                new("United Arab Emirates", "������", "AE", "ar-AE"),
                new("Ireland", "������", "IE", "en-IE"),
                new("Austria", "�µ���", "AT", "de-AT"),
                new("Austalia", "�Ĵ�����", "AU", "en-AU"),
                new("Brazil", "����", "BR", "pt-BR"),
                new("Belgium", "����ʱ", "BE", "nl-BE"),
                new("Poland", "����", "PL", "pl-PL"),
                new("Denmark", "����", "DK", "da-DK"),
                new("Germany", "�¹�", "DE", "de-DE"),
                new("Russia", "����˹", "RU", "ru-RU"),
                new("France", "����", "FR", "fr-FR"),
                new("Finland", "����", "FI", "fi-FI"),
                new("Colombia", "���ױ���", "CO", "es-CO"),
                //new("Korea", "����", "KR", "ko-KR"),
                new("Netherlands", "����", "NL", "nl-NL"),
                new("Canada", "���ô�", "CA", "en-CA"),
                new("Czech Republic", "�ݿ˹��͹�", "CZ", "cs-CZ"),
                //new("United States", "����", "US", "en-US"),
                new("Mexico", "ī����", "MX", "es-MX"),
                new("South Africa", "�Ϸ�", "ZA", "en-ZA"),
                new("Norway", "Ų��", "NO", "nb-NO"),
                new("Portugal", "������", "PT", "pt-PT"),
                //new("Japan", "�ձ�", "JP", "ja-JP"),
                new("Sweden", "���", "SE", "sv-SE"),
                new("Switzerland", "��ʿ", "CH", "de-CH"),
                new("Saudi Arabia", "ɳ�ذ�����", "SA", "ar-SA"),
                new("Slovakia", "˹�工��", "SK", "sk-SK"),
                //new("Taiwan", "̨��", "TW", "zh-TW"),
                new("Turkey", "������", "TR", "tr-TR"),
                new("Spain", "������", "ES", "es-ES"),
                new("Greece", "ϣ��", "GR", "el-GR"),
                //new("Hong Kong SAR", "���", "HK", "zh-HK"),
                //new("Singapore", "�¼���", "SG", "en-SG"),
                new("New Zealand", "������", "NZ", "en-NZ"),
                new("Hungary", "������", "HU", "hu-HU"),
                new("Israel", "��ɫ��", "IL", "he-IL"),
                new("Italy", "�����", "IT", "it-IT"),
                new("India", "ӡ��", "IN", "en-IN"),
                new("United Kingdom", "Ӣ��", "GB", "en-GB"),
                new("Chile", "����", "CL", "es-CL"),
                new("China", "�й�", "CN", "zh-CN")
            }).ToArray());
            //Form1.lsMarket.Sort((x, y) => string.Compare(x.ename, y.ename));
            cbGameMarket.Items.AddRange(Form1.lsMarket.ToArray());
            cbGameMarket.SelectedIndex = 0;
            pbGame.Image = pbGame.InitialImage;

            if (Environment.OSVersion.Version.Major < 10)
            {
                linkAppxAdd.Enabled = false;
                gbAddAppxPackage.Visible = gbGamingServices.Visible = false;
            }
            string xboxGameFilePath = Path.Combine(resourceDirectory, "XboxGame.json");
            if (File.Exists(xboxGameFilePath))
            {
                string json = File.ReadAllText(xboxGameFilePath);
                XboxGameDownload.XboxGame? xboxGame = null;
                try
                {
                    xboxGame = JsonSerializer.Deserialize<XboxGameDownload.XboxGame>(json);
                }
                catch { }
                if (xboxGame != null && xboxGame.Serialize != null && !xboxGame.Serialize.IsEmpty)
                    XboxGameDownload.dicXboxGame = xboxGame.Serialize;
            }
            if (bAutoStartup)
            {
                ButStart_Click(null, null);
            }
        }

        private class ComboboxItem
        {
            public string? Text { get; set; }
            public object? Value { get; set; }
            public override string? ToString()
            {
                return Text;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (DateTime.Compare(DateTime.Now, new DateTime(Properties.Settings.Default.NextUpdate)) >= 0)
            {
                tsmUpdate.Enabled = false;
                ThreadPool.QueueUserWorkItem(delegate { UpdateFile.Start(true, this); });
            }
            Task.Run(async () =>
            {
                bIPv6Support = await ClassWeb.TestIPv6();
                if (bIPv6Support) SaveLog("��ʾ��Ϣ", "��⵽����ʹ��IPv6�������������Xbox�������أ��������·������̨�رգ�PC�û����Դ���Ϣ��", "localhost", 0x0000FF);
            });
            if (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 1803)
            {
                Task.Run(() =>
                {
                    string outputString = "";
                    try
                    {
                        using Process p = new();
                        p.StartInfo.FileName = "powershell.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.StandardInput.WriteLine("Get-DOConfig");
                        p.StandardInput.Close();
                        outputString = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();
                    }
                    catch { }

                    var keyValuePairs = new Dictionary<string, string>();
                    var regex = new Regex(@"^\s*(\S.*?)\s*:\s*(.*?)\s*$", RegexOptions.Multiline);
                    var matches = regex.Matches(outputString);
                    foreach (Match match in matches)
                    {
                        var key = match.Groups[1].Value.Trim();
                        var value = match.Groups[2].Value.Trim();
                        keyValuePairs[key] = value;
                    }
                    StringBuilder sb = new();
                    if (keyValuePairs.TryGetValue("DownBackLimitBpsProvider", out string? DownBackLimitBpsProvider) && DownBackLimitBpsProvider != "DefaultProvider" && keyValuePairs.TryGetValue("DownBackLimitBps", out string? DownBackLimitBps) && DownBackLimitBps != "0")
                    {
                        sb.Append("��̨���� " + Math.Round(double.Parse(DownBackLimitBps) / 131072, 1, MidpointRounding.AwayFromZero) + "Mbps��");
                    }
                    if (keyValuePairs.TryGetValue("DownloadForegroundLimitBpsProvider", out string? DownloadForegroundLimitBpsProvider) && DownloadForegroundLimitBpsProvider != "DefaultProvider" && keyValuePairs.TryGetValue("DownloadForegroundLimitBps", out string? DownloadForegroundLimitBps) && DownloadForegroundLimitBps != "0")
                    {
                        sb.Append("ǰ̨���� " + Math.Round(double.Parse(DownloadForegroundLimitBps) / 131072, 1, MidpointRounding.AwayFromZero) + "Mbps��");
                    }
                    if (keyValuePairs.TryGetValue("DownBackLimitPctProvider", out string? DownBackLimitPctProvider) && DownBackLimitPctProvider != "DefaultProvider" && keyValuePairs.TryGetValue("DownBackLimitPct", out string? DownBackLimitPct) && DownBackLimitPct != "0")
                    {
                        sb.Append("��̨���� " + DownBackLimitPct + "%��");
                    }
                    if (keyValuePairs.TryGetValue("DownloadForegroundLimitPctProvider", out string? DownloadForegroundLimitPctProvider) && DownloadForegroundLimitPctProvider != "DefaultProvider" && keyValuePairs.TryGetValue("DownloadForegroundLimitPct", out string? DownloadForegroundLimitPct) && DownloadForegroundLimitPct != "0")
                    {
                        sb.Append("ǰ̨���� " + DownloadForegroundLimitPct + "%��");
                    }
                    if (sb.Length > 0)
                    {
                        SaveLog("������Ϣ", "ϵͳ������������ʱʹ�õĴ���" + sb.ToString() + "����Windowsϵͳ�����������Ż����á�������ơ�", "localhost", 0xFF0000);
                    }
                });
            }
        }

        private void TsmUpdate_Click(object sender, EventArgs e)
        {
            tsmUpdate.Enabled = false;
            ThreadPool.QueueUserWorkItem(delegate { UpdateFile.Start(false, this); });
        }

        private void TsmiStartup_Click(object sender, EventArgs e)
        {
            FormStartup dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void TsmProductManual_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(UpdateFile.project) { UseShellExecute = true });
        }

        private void TsmAbout_Click(object sender, EventArgs e)
        {
            FormAbout dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void TsmOpenSite_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            Process.Start(new ProcessStartInfo((string)tsmi.Tag) { UseShellExecute = true });
        }

        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TsmiShow_Click(sender, EventArgs.Empty);
            }
        }

        private void TsmiShow_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            OldUp = OldDown = 0;
            timerTraffic.Start();
        }

        private void TsmiExit_Click(object sender, EventArgs e)
        {
            bClose = true;
            this.Close();
        }

        bool bTips = true, bClose = false;
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bClose) return;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            if (bTips && !bAutoStartup)
            {
                bTips = false;
                this.notifyIcon1.ShowBalloonTip(5, "Xbox��������", "��С����ϵͳ����", ToolTipIcon.Info);
                timerTraffic.Stop();
            }
            e.Cancel = true;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            notifyIcon1.Visible = false;
            if (bServiceFlag) ButStart_Click(null, null);
            if (Form1.bAutoStartup) Application.Exit();
            this.Dispose();
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Show();
            switch (tabControl1.SelectedTab.Name)
            {
                case "tabStore":
                    if (gbMicrosoftStore.Tag == null || (gbMicrosoftStore.Tag != null && DateTime.Compare(DateTime.Now, Convert.ToDateTime(gbMicrosoftStore.Tag).AddHours(12)) >= 0))
                    {
                        gbMicrosoftStore.Tag = DateTime.Now;
                        cbGameXGP1.Items.Clear();
                        cbGameXGP2.Items.Clear();
                        dicExchangeRate.Clear();
                    }
                    if (Environment.OSVersion.Version.Major >= 10)
                    {
                        if (cbGameXGP1.Items.Count == 0 || (cbGameXGP1.Items[0].ToString() ?? string.Empty).Contains("(����ʧ��)") || (cbGameXGP1.Items[^1].ToString() ?? string.Empty).Contains("(����ʧ��)"))
                        {
                            cbGameXGP1.Items.Clear();
                            cbGameXGP1.Items.Add(new Product("���ܻ�ӭ Xbox Game Pass ��Ϸ (������)", "0"));
                            cbGameXGP1.SelectedIndex = 0;
                            ThreadPool.QueueUserWorkItem(delegate { XboxGamePass(1); });
                        }
                        if (cbGameXGP2.Items.Count == 0 || (cbGameXGP2.Items[0].ToString() ?? string.Empty).Contains("(����ʧ��)") || (cbGameXGP2.Items[^1].ToString() ?? string.Empty).Contains("(����ʧ��)"))
                        {
                            cbGameXGP2.Items.Clear();
                            cbGameXGP2.Items.Add(new Product("�������� Xbox Game Pass ��Ϸ (������)", "0"));
                            cbGameXGP2.SelectedIndex = 0;
                            ThreadPool.QueueUserWorkItem(delegate { XboxGamePass(2); });
                        }
                    }
                    else if (cbGameXGP1.Items.Count == 0)
                    {
                        cbGameXGP1.Items.Add(new Product("���ܻ�ӭ Xbox Game Pass ��Ϸ (��֧��)", "0"));
                        cbGameXGP1.SelectedIndex = 0;
                        cbGameXGP2.Items.Add(new Product("�������� Xbox Game Pass ��Ϸ (��֧��)", "0"));
                        cbGameXGP2.SelectedIndex = 0;
                    }
                    break;
                case "tabTools":
                    if (cbAppxDrive.Items.Count == 0 && gbAddAppxPackage.Visible)
                    {
                        LinkAppxRefreshDrive_LinkClicked(null, null);
                    }
                    break;
            }
        }

        private void Dgv_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            Rectangle rectangle = new(e.RowBounds.Location.X, e.RowBounds.Location.Y, dgv.RowHeadersWidth - 1, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), dgv.RowHeadersDefaultCellStyle.Font, rectangle, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        delegate void CallbackTextBox(TextBox tb, string str);
        public void SetTextBox(TextBox tb, string str)
        {
            if (tb.InvokeRequired)
            {
                CallbackTextBox d = new(SetTextBox);
                Invoke(d, new object[] { tb, str });
            }
            else tb.Text = str;
        }

        delegate void CallbackSaveLog(string status, string content, string ip, int argb);
        public void SaveLog(string status, string content, string ip, int argb = 0)
        {
            if (lvLog.InvokeRequired)
            {
                CallbackSaveLog d = new(SaveLog);
                Invoke(d, new object[] { status, content, ip, argb });
            }
            else
            {
                ListViewItem listViewItem = new(new string[] { status, content, ip, DateTime.Now.ToString("HH:mm:ss.fff") });
                if (argb >= 1) listViewItem.ForeColor = Color.FromArgb(argb);
                lvLog.Items.Insert(0, listViewItem);
            }
        }

        #region ѡ�-����
        NetworkInterface? adapter = null;
        private long OldUp { get; set; }
        private long OldDown { get; set; }

        private void TimerTraffic_Tick(object sender, EventArgs e)
        {
            if (adapter != null)
            {
                long nowUp = adapter.GetIPStatistics().BytesSent;
                long nowDown = adapter.GetIPStatistics().BytesReceived;
                if (OldUp > 0 || OldDown > 0)
                {
                    long up = nowUp - OldUp;
                    long down = nowDown - OldDown;
                    labelTraffic.Text = String.Format("����: �� {0} �� {1}", ClassMbr.ConvertBps(up * 8), ClassMbr.ConvertBps(down * 8));
                }
                OldUp = nowUp;
                OldDown = nowDown;
            }
        }

        private void CkbNSBrowser_CheckedChanged(object sender, EventArgs e)
        {
            linkNSHomepage.Enabled = ckbNSBrowser.Checked;
            if (ckbNSBrowser.Checked) ckbHttpService.Checked = true;
        }

        private void LinkNSHomepage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormNSBH dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void RbCDN_CheckedChanged(object? sender, EventArgs? e)
        {
            if (sender == null) return;
            RadioButton control = (RadioButton)sender;
            if (!control.Checked) return;
            switch (control.Name)
            {
                case "rbEpicCDN1":
                    if (!Properties.Settings.Default.EpicCDN)
                        tbEpicIP.Clear();
                    else
                        tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                    break;
                case "rbEpicCDN2":
                    if (Properties.Settings.Default.EpicCDN)
                        tbEpicIP.Clear();
                    else
                        tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                    break;
            }
        }

        private void CkbBattleNetease_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbBattleNetease.Checked)
            {
                ckbDnsService.Checked = true;
                ckbSetDns.Checked = true;
            }
        }

        private void ButBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new()
            {
                SelectedPath = tbLocalPath.Text
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tbLocalPath.Text = dlg.SelectedPath;
            }
        }

        private void LinkDoHServer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormDoH dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void CkbSetDns_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbSetDns.Checked)
            {
                ckbDnsService.Checked = true;
            }
            else
            {
                ckbBattleNetease.Checked = false;
            }
        }

        private void CkbGameLink_CheckedChanged(object? sender, EventArgs? e)
        {
            if (ckbGameLink.Checked)
            {
                ckbHttpService.Checked = true;
            }
            else
            {
                ckbLocalUpload.Checked = false;
            }
        }

        private void CkbLocalUpload_CheckedChanged(object? sender, EventArgs? e)
        {
            if (ckbLocalUpload.Checked)
            {
                ckbGameLink.Checked = true;
                ckbHttpService.Checked = true;
            }
        }

        private async void CkbBetterAkamaiIP_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbBetterAkamaiIP.Checked)
            {
                bool update = true;
                FileInfo fi = new(Path.Combine(resourceDirectory, "IP.AkamaiV2.txt"));
                if (fi.Exists && fi.Length >= 1) update = DateTime.Compare(DateTime.Now, fi.LastWriteTime.AddDays(7)) >= 0;
                if (update) await UpdateFile.DownloadIP(fi);
                List<string[]> lsIP = new();
                if (fi.Exists)
                {
                    using StreamReader sr = fi.OpenText();
                    string content = sr.ReadToEnd();
                    Match result = FormImportIP.rMatchIP.Match(content);
                    while (result.Success)
                    {
                        if (IPAddress.TryParse(result.Groups["IP"].Value, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            lsIP.Add(new string[] { ip.ToString(), result.Groups["Location"].Value });
                        }
                        result = result.NextMatch();
                    }
                }
                if (lsIP.Count == 0)
                {
                    MessageBox.Show("Akamai ��ѡ IP �б����ڣ����ڲ���ѡ��е��롣", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                lsIP = lsIP.OrderBy(s => Guid.NewGuid()).Take(30).ToList();
                ckbBetterAkamaiIP.Enabled = false;
                string[] test = { "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt", "http://gst.prod.dl.playstation.net/networktest/get_192m", "http://ctest-dl-lp1.cdn.nintendo.net/30m" };
                Random ran = new();
                Uri uri = new(test[ran.Next(test.Length)]);
                StringBuilder sb = new();
                sb.AppendLine("GET " + uri.PathAndQuery + " HTTP/1.1");
                sb.AppendLine("Host: " + uri.Host);
                sb.AppendLine("User-Agent: XboxDownload" + (uri.Host.Contains("nintendo") ? "/Nintendo NX" : ""));
                sb.AppendLine("Range: bytes=0-10485759");
                sb.AppendLine();
                byte[] buffer = Encoding.ASCII.GetBytes(sb.ToString());
                CancellationTokenSource cts = new();
                Task[] tasks = new Task[lsIP.Count];
                string[] akamai = Array.Empty<string>();
                for (int i = 0; i <= tasks.Length - 1; i++)
                {
                    string[] _ip = lsIP[i];
                    tasks[i] = new Task(() =>
                    {
                        SocketPackage socketPackage = uri.Scheme == "http" ? ClassWeb.TcpRequest(uri, buffer, _ip[0], false, null, 30000, cts) : ClassWeb.TlsRequest(uri, buffer, _ip[0], false, null, 30000, cts);
                        if (akamai.Length == 0 && socketPackage.Buffer?.Length == 10485760) akamai = _ip;
                        else if (!cts.IsCancellationRequested) Task.Delay(30000, cts.Token);
                        socketPackage.Buffer = null;
                    });
                }
                Array.ForEach(tasks, x => x.Start());
                await Task.WhenAny(tasks);
                cts.Cancel();
                GC.Collect();
                if (!bServiceFlag) return;
                if (akamai.Length == 0)
                {
                    cts = new();
                    tasks = lsIP.Select(_ip => Task.Run(() =>
                    {
                        Uri uri = new(test[ran.Next(test.Length)]);
                        using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(uri.ToString().Replace(uri.Host, _ip[0]), "HEAD", null, null, new() { { "Host", uri.Host }, { "User-Agent", "XboxDownload" + (uri.Host.Contains("nintendo") ? "/Nintendo NX" : "") } }, 3000, null, cts.Token);
                        if (akamai.Length == 0 && response != null && response.IsSuccessStatusCode) akamai = _ip;
                        else if (!cts.IsCancellationRequested) Task.Delay(3000, cts.Token);
                    })).ToArray();
                    await Task.WhenAny(tasks);
                    cts.Cancel();
                    if (!bServiceFlag) return;
                    if (akamai.Length > 0)
                    {
                        SaveLog("��ʾ��Ϣ", "��ѡ Akamai IP ���ٳ�ʱ�����ָ�� -> " + akamai[0] + "�������ڲ���ѡ����ֶ�����ָ����", "localhost", 0xFF0000);
                    }
                    else
                    {
                        SaveLog("��ʾ��Ϣ", "��ѡ Akamai IP ȫ���������ӣ���������״����", "localhost", 0xFF0000);
                        ckbBetterAkamaiIP.Enabled = true;
                        return;
                    }
                }
                else
                {
                    SaveLog("��ʾ��Ϣ", "��ѡ Akamai IP -> " + akamai[0] + " (" + akamai[1] + ")", "localhost", 0x008000);
                }
                if (akamai.Length > 0)
                {
                    ckbBetterAkamaiIP.Tag = true;
                    DnsListen.SetAkamaiIP(akamai[0]);
                    UpdateHosts(true, akamai[0]);
                    DnsListen.UpdateHosts(akamai[0]);
                    if (ckbLocalUpload.Checked) Properties.Settings.Default.LocalUpload = false;
                    tbComIP.Text = tbCnIP.Text = tbCnIP2.Text = tbAppIP.Text = tbPSIP.Text = tbNSIP.Text = tbEAIP.Text = tbUbiIP.Text = tbBattleIP.Text = akamai[0];
                    if (!Properties.Settings.Default.EpicCDN) tbEpicIP.Text = akamai[0];
                }
                ckbBetterAkamaiIP.Enabled = true;
            }
            else if (bServiceFlag && Convert.ToBoolean(ckbBetterAkamaiIP.Tag))
            {
                ckbBetterAkamaiIP.Tag = null;
                if (!string.IsNullOrEmpty(Properties.Settings.Default.ComIP))
                    tbComIP.Text = Properties.Settings.Default.ComIP;
                else if (DnsListen.dicService2V4.TryGetValue("xvcf2.xboxlive.com", out List<ResouceRecord>? lsComIp))
                    tbComIP.Text = lsComIp.Count >= 1 ? new IPAddress(lsComIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
                    tbCnIP.Text = Properties.Settings.Default.CnIP;
                else if (DnsListen.dicService2V4.TryGetValue("assets2.xboxlive.cn", out List<ResouceRecord>? lsCnIp))
                    tbCnIP.Text = lsCnIp.Count >= 1 ? new IPAddress(lsCnIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP2))
                    tbCnIP2.Text = Properties.Settings.Default.CnIP2;
                else if (DnsListen.dicService2V4.TryGetValue("dlassets2.xboxlive.cn", out List<ResouceRecord>? lsCnIp2))
                    tbCnIP2.Text = lsCnIp2.Count >= 1 ? new IPAddress(lsCnIp2?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
                    tbAppIP.Text = Properties.Settings.Default.AppIP;
                else if (DnsListen.dicService2V4.TryGetValue("2.tlu.dl.delivery.mp.microsoft.com", out List<ResouceRecord>? lsAppIp))
                    tbAppIP.Text = lsAppIp.Count >= 1 ? new IPAddress(lsAppIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.PSIP))
                    tbPSIP.Text = Properties.Settings.Default.PSIP;
                else if (DnsListen.dicService2V4.TryGetValue("gst.prod.dl.playstation.net", out List<ResouceRecord>? lsPSIp))
                    tbPSIP.Text = lsPSIp.Count >= 1 ? new IPAddress(lsPSIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.NSIP))
                    tbNSIP.Text = Properties.Settings.Default.NSIP;
                else if (DnsListen.dicService2V4.TryGetValue("atum.hac.lp1.d4c.nintendo.net", out List<ResouceRecord>? lsNSIp))
                    tbNSIP.Text = lsNSIp.Count >= 1 ? new IPAddress(lsNSIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.EAIP))
                    tbEAIP.Text = Properties.Settings.Default.EAIP;
                else if (DnsListen.dicService2V4.TryGetValue("origin-a.akamaihd.net", out List<ResouceRecord>? lsEAIp))
                    tbEAIP.Text = lsEAIp.Count >= 1 ? new IPAddress(lsEAIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.BattleIP))
                    tbBattleIP.Text = Properties.Settings.Default.BattleIP;
                else if (DnsListen.dicService2V4.TryGetValue("blzddist1-a.akamaihd.net", out List<ResouceRecord>? lsBattleIp))
                    tbBattleIP.Text = lsBattleIp.Count >= 1 ? new IPAddress(lsBattleIp?[0].Datas!).ToString() : "";
                if (!Properties.Settings.Default.EpicCDN)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP))
                        tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                    else if (DnsListen.dicService2V4.TryGetValue("epicgames-download1.akamaized.net", out List<ResouceRecord>? lsEpicIp))
                        tbEpicIP.Text = lsEpicIp.Count >= 1 ? new IPAddress(lsEpicIp?[0].Datas!).ToString() : "";
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UbiIP))
                    tbUbiIP.Text = Properties.Settings.Default.UbiIP;
                else if (DnsListen.dicService2V4.TryGetValue("uplaypc-s-ubisoft.cdn.ubionline.com.cn", out List<ResouceRecord>? lsUbiIp))
                    tbUbiIP.Text = lsUbiIp.Count >= 1 ? new IPAddress(lsUbiIp?[0].Datas!).ToString() : "";
                DnsListen.SetAkamaiIP();
                UpdateHosts(true);
                DnsListen.UpdateHosts();
                if (ckbLocalUpload.Checked) Properties.Settings.Default.LocalUpload = true;
            }
            if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
        }

        public async void ButStart_Click(object? sender, EventArgs? e)
        {
            if (bServiceFlag)
            {
                butStart.Enabled = false;
                bServiceFlag = false;
                UpdateHosts(false);
                if (Properties.Settings.Default.SetDns) ClassDNS.SetDns();
                tbDnsIP.Text = Properties.Settings.Default.DnsIP;
                tbComIP.Text = Properties.Settings.Default.ComIP;
                tbCnIP.Text = Properties.Settings.Default.CnIP;
                tbCnIP2.Text = Properties.Settings.Default.CnIP2;
                tbAppIP.Text = Properties.Settings.Default.AppIP;
                tbPSIP.Text = Properties.Settings.Default.PSIP;
                tbNSIP.Text = Properties.Settings.Default.NSIP;
                tbEAIP.Text = Properties.Settings.Default.EAIP;
                tbBattleIP.Text = Properties.Settings.Default.BattleIP;
                tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                tbUbiIP.Text = Properties.Settings.Default.UbiIP;
                pictureBox1.Image = Properties.Resource.Xbox1;
                linkTestDns.Enabled = linkRestartEABackgroundService.Enabled = linkRestartEpic.Enabled = false;

                foreach (Control control in this.groupBox1.Controls)
                {
                    if ((control is TextBox || control is CheckBox || control is Panel || control is Button || control is ComboBox) && control != butStart)
                        control.Enabled = true;
                }
                ckbBetterAkamaiIP.Checked = ckbBetterAkamaiIP.Enabled = false;
                ckbBetterAkamaiIP.Tag = null;
                linkRepairDNS.Enabled = linkSniProxy.Enabled = cbLocalIP.Enabled = true;
                linkSniProxy.Text = "����";
                dnsListen.Close();
                httpListen.Close();
                httpsListen.Close();
                Program.SystemSleep.RestoreForCurrentThread();
                if (Properties.Settings.Default.SetDns)
                {
                    butStart.Text = "����ֹͣ...";
                    await Task.Run(() =>
                    {
                        string[] hosts = { "www.xbox.com", "www.playstation.com", "www.nintendo.com" };
                        for (int i = 0; i < 15; i++)
                        {
                            IPHostEntry? hostEntry = null;
                            try
                            {
                                hostEntry = Dns.GetHostEntry(hosts[i % hosts.Length]);
                            }
                            catch { }
                            if (hostEntry == null)
                                Thread.Sleep(1000);
                            else
                                break;
                        }
                    });
                }
                butStart.Text = "��ʼ����";
            }
            else
            {
                string? dnsIP = null;
                if (!string.IsNullOrWhiteSpace(tbDnsIP.Text))
                {
                    if (IPAddress.TryParse(tbDnsIP.Text.Trim(), out IPAddress? ipAddress) && !IPAddress.IsLoopback(ipAddress))
                    {
                        dnsIP = tbDnsIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("DNS ������ IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbDnsIP.Focus();
                        tbDnsIP.SelectAll();
                        return;
                    }
                }
                string? comIP = null;
                if (!string.IsNullOrWhiteSpace(tbComIP.Text))
                {
                    if (IPAddress.TryParse(tbComIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        comIP = tbComIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ�� com �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbComIP.Focus();
                        tbComIP.SelectAll();
                        return;
                    }
                }
                string? cnIP = null;
                if (!string.IsNullOrWhiteSpace(tbCnIP.Text))
                {
                    if (IPAddress.TryParse(tbCnIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        cnIP = tbCnIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ�� cn1 �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbCnIP.Focus();
                        tbCnIP.SelectAll();
                        return;
                    }
                }
                string? cnIP2 = null;
                if (!string.IsNullOrWhiteSpace(tbCnIP2.Text))
                {
                    if (IPAddress.TryParse(tbCnIP2.Text.Trim(), out IPAddress? ipAddress))
                    {
                        cnIP2 = tbCnIP2.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ�� cn2 �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbCnIP2.Focus();
                        tbCnIP2.SelectAll();
                        return;
                    }
                }
                string? appIP = null;
                if (!string.IsNullOrWhiteSpace(tbAppIP.Text))
                {
                    if (IPAddress.TryParse(tbAppIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        appIP = tbAppIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ��Ӧ���������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbAppIP.Focus();
                        tbAppIP.SelectAll();
                        return;
                    }
                }
                string? psIP = null;
                if (!string.IsNullOrWhiteSpace(tbPSIP.Text))
                {
                    if (IPAddress.TryParse(tbPSIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        psIP = tbPSIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ�� PS �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbPSIP.Focus();
                        tbPSIP.SelectAll();
                        return;
                    }
                }
                string? nsIP = null;
                if (!string.IsNullOrWhiteSpace(tbNSIP.Text))
                {
                    if (IPAddress.TryParse(tbNSIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                        {
                            nsIP = tbNSIP.Text = ipAddress.ToString();
                        }
                        else
                        {
                            MessageBox.Show("NS ������֧�� IPv6", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            tbNSIP.Focus();
                            tbNSIP.SelectAll();
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("ָ�� NS �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbNSIP.Focus();
                        tbNSIP.SelectAll();
                        return;
                    }
                }
                string? eaIP = null;
                if (!string.IsNullOrWhiteSpace(tbEAIP.Text))
                {
                    if (IPAddress.TryParse(tbEAIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        eaIP = tbEAIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ�� EA �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbEAIP.Focus();
                        tbEAIP.SelectAll();
                        return;
                    }
                }
                string? battleIP = null;
                if (!string.IsNullOrWhiteSpace(tbBattleIP.Text))
                {
                    if (IPAddress.TryParse(tbBattleIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        battleIP = tbBattleIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ�� ս�� ����IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbBattleIP.Focus();
                        tbBattleIP.SelectAll();
                        return;
                    }
                }
                string? epicIP = null;
                if (!string.IsNullOrWhiteSpace(tbEpicIP.Text))
                {
                    if (rbEpicCDN2.Checked)
                    {
                        if (IPAddress.TryParse(tbEpicIP.Text.Trim(), out IPAddress? ipAddress))
                        {
                            epicIP = tbEpicIP.Text = ipAddress.ToString();
                        }
                        else
                        {
                            MessageBox.Show("ָ�� Epic �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            tbEpicIP.Focus();
                            tbEpicIP.SelectAll();
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("��Ѷ��CDN���Զ��ض���������ط�����������ָ��IP��", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        tbEpicIP.Focus();
                        tbEpicIP.SelectAll();
                        return;
                    }
                }
                string? ubiIP = null;
                if (!string.IsNullOrWhiteSpace(tbUbiIP.Text))
                {
                    if (IPAddress.TryParse(tbUbiIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        ubiIP = tbUbiIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("ָ�� ���� �������� IP ����ȷ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbUbiIP.Focus();
                        tbUbiIP.SelectAll();
                        return;
                    }
                }
                butStart.Enabled = false;

                Properties.Settings.Default.DnsIP = dnsIP;
                Properties.Settings.Default.ComIP = comIP;
                Properties.Settings.Default.CnIP = cnIP;
                Properties.Settings.Default.CnIP2 = cnIP2;
                Properties.Settings.Default.AppIP = appIP;
                Properties.Settings.Default.PSIP = psIP;
                Properties.Settings.Default.NSIP = nsIP;
                Properties.Settings.Default.NSBrowser = ckbNSBrowser.Checked;
                Properties.Settings.Default.EAIP = eaIP;
                Properties.Settings.Default.BattleIP = battleIP;
                Properties.Settings.Default.BattleNetease = ckbBattleNetease.Checked;
                Properties.Settings.Default.EpicIP = epicIP;
                Properties.Settings.Default.EpicCDN = rbEpicCDN1.Checked;
                Properties.Settings.Default.UbiIP = ubiIP;
                Properties.Settings.Default.GameLink = ckbGameLink.Checked;
                Properties.Settings.Default.Truncation = ckbTruncation.Checked;
                Properties.Settings.Default.LocalUpload = ckbLocalUpload.Checked;
                Properties.Settings.Default.LocalPath = tbLocalPath.Text;
                Properties.Settings.Default.ListenIP = cbListenIP.SelectedIndex;
                Properties.Settings.Default.DnsService = ckbDnsService.Checked;
                Properties.Settings.Default.HttpService = ckbHttpService.Checked;
                Properties.Settings.Default.DoH = ckbDoH.Checked;
                Properties.Settings.Default.DisableIPv6DNS = ckbDisableIPv6DNS.Checked;
                Properties.Settings.Default.SetDns = ckbSetDns.Checked;
                Properties.Settings.Default.MicrosoftStore = ckbMicrosoftStore.Checked;
                Properties.Settings.Default.EAStore = ckbEAStore.Checked;
                Properties.Settings.Default.BattleStore = ckbBattleStore.Checked;
                Properties.Settings.Default.EpicStore = ckbEpicStore.Checked;
                Properties.Settings.Default.UbiStore = ckbUbiStore.Checked;
                Properties.Settings.Default.SniProxy = ckbSniProxy.Checked;
                Properties.Settings.Default.Save();

                try
                {
                    Type? t1 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                    if (t1 != null)
                    {
                        if (Activator.CreateInstance(t1) is INetFwPolicy2 policy2)
                        {
                            bool bRuleAdd = true;
                            foreach (INetFwRule rule in policy2.Rules)
                            {
                                if (rule.Name == "XboxDownload" || rule.Name == "Xbox��������")
                                {
                                    if (bRuleAdd && rule.ApplicationName == Application.ExecutablePath && rule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN && rule.Protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY && rule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW && rule.Profiles == (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL && rule.Enabled)
                                        bRuleAdd = false;
                                    else
                                        policy2.Rules.Remove(rule.Name);
                                }
                                else if (String.Equals(rule.ApplicationName, Application.ExecutablePath, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    policy2.Rules.Remove(rule.Name);
                                }
                            }
                            if (bRuleAdd)
                            {
                                Type? t2 = Type.GetTypeFromProgID("HNetCfg.FwRule");
                                if (t2 != null)
                                {
                                    if (Activator.CreateInstance(t2) is INetFwRule rule)
                                    {
                                        rule.Name = "XboxDownload";
                                        rule.ApplicationName = Application.ExecutablePath;
                                        rule.Enabled = true;
                                        policy2.Rules.Add(rule);
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                string resultInfo = string.Empty;
                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo("netstat", @"-aon")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    p.Start();
                    resultInfo = p.StandardOutput.ReadToEnd();
                    p.Close();
                }
                Match result = Regex.Match(resultInfo, @"(?<protocol>TCP|UDP)\s+(?<ip>[^\s]+):(?<port>80|443|53)\s+[^\s]+\s+(?<status>[^\s]+\s+)?(?<pid>\d+)");
                if (result.Success)
                {
                    ConcurrentDictionary<Int32, Process?> dic = new();
                    StringBuilder sb = new();
                    while (result.Success)
                    {
                        string ip = result.Groups["ip"].Value;
                        if (Properties.Settings.Default.ListenIP == 0 && ip == Properties.Settings.Default.LocalIP || Properties.Settings.Default.ListenIP == 1)
                        {
                            string protocol = result.Groups["protocol"].Value;
                            if (protocol == "TCP" && result.Groups["status"].Value.Trim() == "LISTENING" || protocol == "UDP")
                            {
                                int port = Convert.ToInt32(result.Groups["port"].Value);
                                if (port == 53 && Properties.Settings.Default.DnsService || ((port == 80 || port == 443) && Properties.Settings.Default.HttpService))
                                {
                                    int pid = int.Parse(result.Groups["pid"].Value);
                                    if (!dic.ContainsKey(pid) && pid != 0)
                                    {
                                        sb.AppendLine(protocol + "\t" + ip + ":" + port);
                                        if (pid == 4)
                                        {
                                            dic.TryAdd(pid, null);
                                            sb.AppendLine("ϵͳ����");
                                        }
                                        else
                                        {
                                            try
                                            {
                                                Process proc = Process.GetProcessById(pid);
                                                dic.TryAdd(pid, proc);
                                                string? filename = proc.MainModule?.FileName;
                                                sb.AppendLine(filename);
                                            }
                                            catch
                                            {
                                                sb.AppendLine("δ֪");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        result = result.NextMatch();
                    }
                    if (!dic.IsEmpty && MessageBox.Show("��⵽���¶˿ڱ�ռ��\n" + sb.ToString() + "\n�Ƿ���ǿ�ƽ���ռ�ö˿ڳ���", "�˿�ռ��", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        foreach (var item in dic)
                        {
                            if (item.Key == 4)
                            {
                                ServiceController[] services = ServiceController.GetServices();
                                foreach (ServiceController service in services)
                                {
                                    switch (service.ServiceName)
                                    {
                                        case "MsDepSvc":        //Web Deployment Agent Service (MsDepSvc)
                                        case "PeerDistSvc":     //BranchCache (PeerDistSvc)
                                        case "ReportServer":    //SQL Server Reporting Services (ReportServer)
                                        case "SyncShareSvc":    //Sync Share Service (SyncShareSvc)
                                        case "W3SVC":           //World Wide Web Publishing Service (W3SVC)
                                            if (service.Status == ServiceControllerStatus.Running)
                                            {
                                                service.Stop();
                                                service.WaitForStatus(ServiceControllerStatus.Stopped);
                                            }
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    item.Value?.Kill();
                                }
                                catch { }
                            }
                        }
                    }
                }
                bServiceFlag = true;
                pictureBox1.Image = Properties.Resource.Xbox2;
                butStart.Text = "ֹͣ����";
                foreach (Control control in this.groupBox1.Controls)
                {
                    if (control is TextBox || control is CheckBox || control is Panel || control is Button || control is ComboBox)
                        control.Enabled = false;
                }
                ckbBetterAkamaiIP.Enabled = true;
                linkRepairDNS.Enabled = cbLocalIP.Enabled = false;
                if (Properties.Settings.Default.SniProxy)
                {
                    linkSniProxy.Text = "����";
                    foreach (var proxy in HttpsListen.dicSniProxy.Values)
                    {
                        proxy.IPs = null;
                    }
                    _ = Task.Run(async () =>
                    {
                        bIPv6Support = await ClassWeb.TestIPv6();
                    });
                }
                else linkSniProxy.Enabled = false;
                UpdateHosts(true);
                DnsListen.UpdateHosts();
                if (Properties.Settings.Default.EAStore) linkRestartEABackgroundService.Enabled = true;
                if (Properties.Settings.Default.EpicStore) linkRestartEpic.Enabled = true;
                if (Properties.Settings.Default.DnsService)
                {
                    linkTestDns.Enabled = true;
                    new Thread(new ThreadStart(dnsListen.Listen))
                    {
                        IsBackground = true
                    }.Start();
                }
                if (Properties.Settings.Default.HttpService)
                {
                    new Thread(new ThreadStart(httpListen.Listen))
                    {
                        IsBackground = true
                    }.Start();
                    new Thread(new ThreadStart(httpsListen.Listen))
                    {
                        IsBackground = true
                    }.Start();
                }
                Program.SystemSleep.PreventForCurrentThread(false);
            }
            if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
            butStart.Enabled = true;
        }

        private void UpdateHosts(bool add, string? akamai = null)
        {
            if (!(Properties.Settings.Default.MicrosoftStore || Properties.Settings.Default.EAStore || Properties.Settings.Default.BattleStore || Properties.Settings.Default.EpicStore || Properties.Settings.Default.UbiStore || Properties.Settings.Default.SniProxy)) return;

            StringBuilder sb = new();
            try
            {
                string sHosts;
                FileInfo fi = new(Environment.SystemDirectory + "\\drivers\\etc\\hosts");
                using (FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    using StreamReader sr = new(fs);
                    sHosts = sr.ReadToEnd();
                }
                if (!(Properties.Settings.Default.SetDns && string.IsNullOrEmpty(Regex.Replace(sHosts, "#.*", "").Trim())))
                {
                    sHosts = Regex.Replace(sHosts, @"# Added by (XboxDownload|Xbox��������)\r\n(.*\r\n)*# End of (XboxDownload|Xbox��������)\r\n", "");
                    if (add)
                    {
                        if (string.IsNullOrEmpty(Properties.Settings.Default.ComIP)) tbComIP.Text = Properties.Settings.Default.LocalIP;
                        sb.AppendLine("# Added by XboxDownload");
                        if (Properties.Settings.Default.MicrosoftStore)
                        {
                            if (!string.IsNullOrEmpty(akamai))
                            {
                                if (Properties.Settings.Default.GameLink)
                                {
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " xvcf1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " tlu.dl.delivery.mp.microsoft.com");
                                }
                                else
                                {
                                    sb.AppendLine(akamai + " xvcf1.xboxlive.com");
                                    sb.AppendLine(akamai + " assets1.xboxlive.com");
                                    sb.AppendLine(akamai + " d1.xboxlive.com");
                                    sb.AppendLine(akamai + " dlassets.xboxlive.com");
                                    sb.AppendLine(akamai + " assets1.xboxlive.cn");
                                    sb.AppendLine(akamai + " d1.xboxlive.cn");
                                    sb.AppendLine(akamai + " dlassets.xboxlive.cn");
                                    sb.AppendLine(akamai + " tlu.dl.delivery.mp.microsoft.com");
                                }
                                sb.AppendLine(akamai + " xvcf2.xboxlive.com");
                                sb.AppendLine(akamai + " assets2.xboxlive.com");
                                sb.AppendLine(akamai + " d2.xboxlive.com");
                                sb.AppendLine(akamai + " dlassets2.xboxlive.com");
                                sb.AppendLine(akamai + " assets2.xboxlive.cn");
                                sb.AppendLine(akamai + " d2.xboxlive.cn");
                                sb.AppendLine(akamai + " dlassets2.xboxlive.cn");
                                sb.AppendLine(akamai + " dl.delivery.mp.microsoft.com");
                                sb.AppendLine(akamai + " 2.tlu.dl.delivery.mp.microsoft.com");
                            }
                            else
                            {
                                string comIP = string.IsNullOrEmpty(Properties.Settings.Default.ComIP) ? Properties.Settings.Default.LocalIP : Properties.Settings.Default.ComIP;
                                if (Properties.Settings.Default.GameLink)
                                {
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " xvcf1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.com");
                                    sb.AppendLine(comIP + " xvcf2.xboxlive.com");
                                    sb.AppendLine(comIP + " assets2.xboxlive.com");
                                    sb.AppendLine(comIP + " d2.xboxlive.com");
                                    sb.AppendLine(comIP + " dlassets2.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.cn");
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " assets2.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " d2.xboxlive.cn");
                                    }
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " 2.tlu.dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " dlassets2.xboxlive.cn");
                                    }
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " tlu.dl.delivery.mp.microsoft.com");
                                }
                                else
                                {
                                    sb.AppendLine(comIP + " xvcf1.xboxlive.com");
                                    sb.AppendLine(comIP + " xvcf2.xboxlive.com");
                                    sb.AppendLine(comIP + " assets1.xboxlive.com");
                                    sb.AppendLine(comIP + " assets2.xboxlive.com");
                                    sb.AppendLine(comIP + " d1.xboxlive.com");
                                    sb.AppendLine(comIP + " d2.xboxlive.com");
                                    sb.AppendLine(comIP + " dlassets.xboxlive.com");
                                    sb.AppendLine(comIP + " dlassets2.xboxlive.com");
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " assets1.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " assets2.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " d1.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " d2.xboxlive.cn");
                                    }
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP2))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.CnIP2 + " dlassets.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP2 + " dlassets2.xboxlive.cn");
                                    }
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " tlu.dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " 2.tlu.dl.delivery.mp.microsoft.com");
                                    }
                                }
                            }
                            if (Properties.Settings.Default.HttpService)
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " www.msftconnecttest.com");
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " packagespc.xboxlive.com");
                            }
                        }
                        if (Properties.Settings.Default.EAStore)
                        {
                            if (!string.IsNullOrEmpty(akamai))
                            {
                                sb.AppendLine(akamai + " origin-a.akamaihd.net");
                            }
                            else if (!string.IsNullOrEmpty(Properties.Settings.Default.EAIP))
                            {
                                sb.AppendLine(Properties.Settings.Default.EAIP + " origin-a.akamaihd.net");
                            }
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                        }
                        if (Properties.Settings.Default.BattleStore)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " us.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " eu.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " kr.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " level3.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " blizzard.gcdn.cloudn.co.kr");
                            sb.AppendLine("0.0.0.0 level3.ssl.blizzard.com");
                            if (!string.IsNullOrEmpty(akamai))
                            {
                                sb.AppendLine(akamai + " downloader.battle.net");
                                sb.AppendLine(akamai + " blzddist1-a.akamaihd.net");
                            }
                            else if (!string.IsNullOrEmpty(Properties.Settings.Default.BattleIP))
                            {
                                sb.AppendLine(Properties.Settings.Default.BattleIP + " downloader.battle.net");
                                if (Regex.IsMatch(Properties.Settings.Default.BattleIP, @"^\d+\.\d+\.\d+\.\d+$"))
                                    sb.AppendLine(Properties.Settings.Default.BattleIP + " blzddist1-a.akamaihd.net");
                                else
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " blzddist1-a.akamaihd.net");
                            }
                        }
                        if (Properties.Settings.Default.EpicStore)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " download.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " fastly-download.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " cloudflare.epicgamescdn.com");
                            if (Properties.Settings.Default.EpicCDN)
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " epicgames-download1.akamaized.net");
                                if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP)) sb.AppendLine(Properties.Settings.Default.EpicIP + " epicgames-download1-1251447533.file.myqcloud.com");
                            }
                            else
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " epicgames-download1-1251447533.file.myqcloud.com");
                                string ip = !string.IsNullOrEmpty(akamai) ? akamai : Properties.Settings.Default.EpicIP;
                                if (!string.IsNullOrEmpty(ip)) sb.AppendLine(ip + " epicgames-download1.akamaized.net");
                            }
                        }
                        if (Properties.Settings.Default.UbiStore)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " uplaypc-s-ubisoft.cdn.ubi.com");
                            sb.AppendLine("0.0.0.0 ubisoftconnect.cdn.ubi.com");
                            if (!string.IsNullOrEmpty(Properties.Settings.Default.UbiIP))
                            {
                                if (Regex.IsMatch(Properties.Settings.Default.UbiIP, @"^\d+\.\d+\.\d+\.\d+$"))
                                    sb.AppendLine(Properties.Settings.Default.UbiIP + " uplaypc-s-ubisoft.cdn.ubionline.com.cn");
                                else
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " uplaypc-s-ubisoft.cdn.ubionline.com.cn");
                            }
                        }
                        if (Properties.Settings.Default.SniProxy)
                        {
                            foreach (string host in HttpsListen.dicSniProxy.Keys)
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " " + host);
                            }
                        }
                        DataTable dt = Form1.dtHosts.Copy();
                        dt.RejectChanges();
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (!Convert.ToBoolean(dr["Enable"])) continue;
                            string? hostName = dr["HostName"].ToString()?.ToLower();
                            string? ip = dr["IP"].ToString()?.Trim();
                            if (!string.IsNullOrEmpty(hostName) && !hostName.StartsWith('*') && !string.IsNullOrEmpty(ip))
                            {
                                sb.AppendLine(ip + " " + hostName);
                            }
                        }
                        sb.AppendLine("# End of XboxDownload");
                        sHosts = sb.ToString() + sHosts;
                    }
                    FileSecurity fSecurity = fi.GetAccessControl();
                    fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                        fi.Attributes = FileAttributes.Normal;
                    using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        if (!string.IsNullOrEmpty(sHosts.Trim()))
                        {
                            using StreamWriter sw = new(fs);
                            sw.WriteLine(sHosts.Trim());
                        }
                    }
                    fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                }
            }
            catch (Exception ex)
            {
                if (add) MessageBox.Show("�޸�ϵͳHosts�ļ�ʧ�ܣ�������Ϣ��" + ex.Message + "\n\n���ֽ��������\n1����ѡ�����ñ��� DNS����\n2����ʱ�رհ�ȫ���������Ӱ�������\n3���ֶ�ɾ��\"" + Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\drivers\\etc\\hosts\"�ļ��������ʼ�������½�һ����", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (Properties.Settings.Default.MicrosoftStore) ThreadPool.QueueUserWorkItem(delegate { RestartService("DoSvc"); });
        }

        private static void RestartService(string servicename)
        {
            Task.Run(() =>
            {
                ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == servicename).SingleOrDefault();
                if (service != null)
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(30000);
                    try
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        }
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            });
        }

        private void LvLog_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && lvLog.SelectedItems.Count == 1)
            {
                cmsLog.Show(MousePosition.X, MousePosition.Y);
            }
        }

        private void LvLog_DoubleClick(object sender, EventArgs e)
        {
            if (lvLog.SelectedItems.Count == 1 && lvLog.SelectedItems[0].SubItems[0].Text.StartsWith("DNS"))
            {
                Match result = Regex.Match(lvLog.SelectedItems[0].SubItems[1].Text, @"(.+) -> ([^,']+)");
                if (result.Success)
                {
                    string host = result.Groups[1].Value;
                    string ip = result.Groups[2].Value;
                    if (Regex.IsMatch(ip, @"^((127\.0\.0\.1)|(10\.\d{1,3}\.\d{1,3}\.\d{1,3})|(172\.((1[6-9])|(2\d)|(3[01]))\.\d{1,3}\.\d{1,3})|(192\.168\.\d{1,3}\.\d{1,3}))$"))
                        return;
                    FormConnectTest dialog = new(host, ip);
                    dialog.ShowDialog();
                    dialog.Dispose();
                }
            }
        }

        private void TsmCopyLog_Click(object sender, EventArgs e)
        {
            string content = lvLog.SelectedItems[0].SubItems[1].Text;
            Clipboard.SetDataObject(content);
            if (Regex.IsMatch(content, @"^https?://(origin-a\.akamaihd\.net|ssl-lvlt\.cdn\.ea\.com|lvlt\.cdn\.ea\.com)"))
            {
                MessageBox.Show("���߰���װ������������ɺ�ɾ����װĿ¼�µ������ļ����ѽ�ѹ���ļ����Ƶ���װĿ¼���ص� EA app ���� Origin ѡ��������أ��ȴ���Ϸ��֤��ɺ󼴿ɡ�", "��ʾ��Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void TsmExportLog_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new()
            {
                Title = "������־",
                Filter = "�ı��ļ�(*.txt)|*.txt",
                FileName = "������־"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new();
                for (int i = 0; i <= lvLog.Items.Count - 1; i++)
                {
                    sb.AppendLine(lvLog.Items[i].SubItems[0].Text + "\t" + lvLog.Items[i].SubItems[1].Text + "\t" + lvLog.Items[i].SubItems[2].Text + "\t" + lvLog.Items[i].SubItems[3].Text);
                }
                File.WriteAllText(dlg.FileName, sb.ToString());
            }
        }

        private void CbLocalIP_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LocalIP = cbLocalIP.Text;
            Properties.Settings.Default.Save();

            timerTraffic.Stop();
            adapter = (cbLocalIP.SelectedItem as ComboboxItem)?.Value as NetworkInterface;
            OldUp = OldDown = 0;
            timerTraffic.Start();
        }

        private void LinkTestDns_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormDns dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void LabelTraffic_MouseEnter(object sender, EventArgs e)
        {
            if (adapter != null && adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) //Refresh Wireless adapter Speed
            {
                adapter = NetworkInterface.GetAllNetworkInterfaces().Where(s => s.Id == adapter!.Id).FirstOrDefault();
                OldUp = OldDown = 0;
            }
            if (adapter != null) toolTip1.SetToolTip(this.labelTraffic, "���ƣ�" + adapter.Name + "\n������" + adapter.Description + "\n�ٶȣ�" + ClassMbr.ConvertBps(adapter.Speed));
        }

        private void LinkRepairDNS_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MessageBox.Show("�������˳�Ӧ�ÿ��ܻ����DNS�����쳣�޷�������\n�˲�������DNS���ø�Ϊ�Զ���ȡ���Ƿ������", "�޸� DNS", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                try
                {
                    using (Process p = new())
                    {
                        p.StartInfo.FileName = @"powershell.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.StandardInput.WriteLine("Get-NetAdapter -Physical | Set-DnsClientServerAddress -ResetServerAddresses");
                        p.StandardInput.WriteLine("exit");
                    }
                    MessageBox.Show("�޸� DNS �ɹ�������������������ڲ���ѡ��е�������ϵͳHosts�ļ�����", "Success", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�޸� DNS ʧ�ܣ�������Ϣ��" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LinkRestartEABackgroundService_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string? path = null;
            using (var key = Microsoft.Win32.Registry.LocalMachine)
            {
                var rk = key.OpenSubKey(@"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop");
                if (rk != null)
                {
                    path = rk.GetValue("LauncherAppPath", null)?.ToString();
                    rk.Close();
                }
            }
            if (path != null && File.Exists(path))
            {
                if (MessageBox.Show("EA app ��û��ʼ���ػ���ֹͣ���س���һ���ӣ����Բ����޸���\n\n��� ���ǡ� ������������ IP �������� EA app���Ƿ������", "�޸� EA app", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
                    Process? processes = Process.GetProcesses().Where(s => s.ProcessName == "EADesktop").FirstOrDefault();
                    if (processes != null)
                    {
                        try
                        {
                            processes.Kill();
                        }
                        catch { }
                    }
                    ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "EABackgroundService").FirstOrDefault();
                    if (service != null)
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                    }
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show("û���ҵ� EA app��", "�޸� EA app", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LinkRestartEpic_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string? path = null;
            using (var key = Microsoft.Win32.Registry.LocalMachine)
            {
                var rk = key.OpenSubKey(@"SOFTWARE\WOW6432Node\Epic Games\EOS\InstallHelper");
                if (rk != null)
                {
                    path = rk.GetValue("Path", null)?.ToString();
                    rk.Close();
                }
                if (path != null)
                {
                    Match result = Regex.Match(path, @"(^.+\\Epic Games\\)");
                    if (result.Success) path = result.Groups[1].Value + "Launcher\\Portal\\Binaries\\Win32\\EpicGamesLauncher.exe";
                }
            }
            if (path != null && File.Exists(path))
            {
                if (MessageBox.Show("��Ҫ�����б��е���Ϸ��Ҫ�����ͻ��˲��ܱ�֤ʹ���� IP��\n\n��� ���ǡ� ������������ Epic �ͻ��ˣ��Ƿ������", "����Epic�ͻ���", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
                    Process? processes = Process.GetProcesses().Where(s => s.ProcessName == "EpicGamesLauncher").FirstOrDefault();
                    if (processes != null)
                    {
                        try
                        {
                            processes.Kill();
                        }
                        catch { }
                    }
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show("û���ҵ�Epic�ͻ��ˡ�", "����Epic�ͻ���", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LinkSniProxy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (bServiceFlag)
            {
                Task.Run(async () =>
                {
                    bIPv6Support = await ClassWeb.TestIPv6();
                });
                foreach (var proxy in HttpsListen.dicSniProxy.Values)
                {
                    proxy.IPs = null;
                }
                SaveLog("��ʾ��Ϣ", "�������ش������DNS���档", "localhost", 0x008000);
            }
            else
            {
                FormSniProxy dialog = new();
                dialog.ShowDialog();
                dialog.Dispose();
            }
        }

        private void CkbRecordLog_CheckedChanged(object? sender, EventArgs? e)
        {
            Properties.Settings.Default.RecordLog = ckbRecordLog.Checked;
            Properties.Settings.Default.Save();
        }

        private void LinkClearLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lvLog.Items.Clear();
        }
        #endregion

        #region ѡ�-����
        private void DgvIpList_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex == -1) return;
            if (e.Button == MouseButtons.Left && dgvIpList.Columns[dgvIpList.CurrentCell.ColumnIndex].Name == "Col_Speed" && dgvIpList.Rows[e.RowIndex].Tag != null)
            {
                var msg = dgvIpList.Rows[e.RowIndex].Tag;
                if (msg != null)
                    MessageBox.Show(msg.ToString(), "Request Headers", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
        }

        private void DgvIpList_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.Button != MouseButtons.Right) return;
            string? host = dgvIpList.Tag.ToString();
            dgvIpList.ClearSelection();
            DataGridViewRow dgvr = dgvIpList.Rows[e.RowIndex];
            dgvr.Selected = true;
            tsmUseIP.Visible = tsmExportRule.Visible = true;
            foreach (var item in this.tsmUseIP.DropDownItems)
            {
                if (item.GetType() == typeof(ToolStripMenuItem))
                {
                    if (item is not ToolStripMenuItem tsmi || tsmi.Name == "tsmUseIPHosts")
                        continue;
                    tsmi.Visible = false;
                }
            }
            tssUseIP1.Visible = tssUseIP2.Visible = tssUseIP3.Visible = false;
            switch (host)
            {
                case "assets1.xboxlive.cn":
                case "assets2.xboxlive.cn":
                case "d1.xboxlive.cn":
                case "d2.xboxlive.cn":
                    tsmUseIPCn.Visible = true;
                    tssUseIP1.Visible = true;
                    break;
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    tsmUseIPCn2.Visible = true;
                    tssUseIP1.Visible = true;
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                    tsmUseIPApp.Visible = true;
                    tssUseIP1.Visible = true;
                    break;
                case "gst.prod.dl.playstation.net":
                case "gs2.ww.prod.dl.playstation.net":
                case "zeus.dl.playstation.net":
                case "ares.dl.playstation.net":
                    tsmUseIPPS.Visible = true;
                    tssUseIP2.Visible = true;
                    break;
                case "Akamai":
                case "AkamaiV2":
                case "AkamaiV6":
                case "atum.hac.lp1.d4c.nintendo.net":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "epicgames-download1.akamaized.net":
                case "uplaypc-s-ubisoft.cdn.ubi.com":
                    tsmUseIPCom.Visible = true;
                    tsmUseIPXbox.Visible = true;
                    tsmUseIPApp.Visible = true;
                    tsmUseIPPS.Visible = true;
                    if (host != "AkamaiV6")
                        tsmUseIPNS.Visible = true;
                    tsmUseIPEa.Visible = true;
                    tsmUseAkamai.Visible = true;
                    tsmUseIPBattle.Visible = true;
                    tsmUseIPEpic.Visible = true;
                    tsmUseIPUbi.Visible = true;
                    tssUseIP1.Visible = true;
                    tssUseIP2.Visible = true;
                    tssUseIP3.Visible = true;
                    break;
                case "uplaypc-s-ubisoft.cdn.ubionline.com.cn":
                    tsmUseIPUbi.Visible = true;
                    tssUseIP3.Visible = true;
                    break;
                default:
                    break;
            }
            tsmSpeedTest.Visible = true;
            tsmSpeedTest.Enabled = ctsSpeedTest is null;
            tsmSpeedTestLog.Enabled = dgvr.Tag is not null;
            cmsIP.Show(MousePosition.X, MousePosition.Y);
        }

        private void TsmUseIP_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            string? ip = dgvr.Cells["Col_IPAddress"].Value?.ToString();
            if (ip == null) return;
            if (sender is not ToolStripMenuItem tsmi) return;
            if (bServiceFlag && tsmi.Name != "tsmUseAkamai")
            {
                MessageBox.Show("����ֹͣ�����������á�", "ʹ��ָ��IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            switch (tsmi.Name)
            {
                case "tsmUseIPCom":
                    tabControl1.SelectedTab = tabService;
                    tbComIP.Text = ip;
                    tbComIP.Focus();
                    break;
                case "tsmUseIPCn":
                    tabControl1.SelectedTab = tabService;
                    tbCnIP.Text = ip;
                    tbCnIP.Focus();
                    break;
                case "tsmUseIPCn2":
                    tabControl1.SelectedTab = tabService;
                    tbCnIP2.Text = ip;
                    tbCnIP2.Focus();
                    break;
                case "tsmUseIPXbox":
                    tabControl1.SelectedTab = tabService;
                    tbComIP.Text = tbCnIP.Text = tbCnIP2.Text = ip;
                    tbCnIP2.Focus();
                    break;
                case "tsmUseIPApp":
                    tabControl1.SelectedTab = tabService;
                    tbAppIP.Text = ip;
                    tbAppIP.Focus();
                    break;
                case "tsmUseIPPS":
                    tabControl1.SelectedTab = tabService;
                    tbPSIP.Text = ip;
                    tbPSIP.Focus();
                    break;
                case "tsmUseIPNS":
                    tabControl1.SelectedTab = tabService;
                    tbNSIP.Text = ip;
                    tbNSIP.Focus();
                    break;
                case "tsmUseIPEa":
                    tabControl1.SelectedTab = tabService;
                    tbEAIP.Text = ip;
                    tbEAIP.Focus();
                    break;
                case "tsmUseIPBattle":
                    tabControl1.SelectedTab = tabService;
                    tbBattleIP.Text = ip;
                    tbBattleIP.Focus();
                    break;
                case "tsmUseIPEpic":
                    tabControl1.SelectedTab = tabService;
                    rbEpicCDN2.Checked = true;
                    tbEpicIP.Text = ip;
                    tbEpicIP.Focus();
                    break;
                case "tsmUseIPUbi":
                    tabControl1.SelectedTab = tabService;
                    tbUbiIP.Text = ip;
                    tbUbiIP.Focus();
                    break;
                case "tsmUseAkamai":
                    tabControl1.SelectedTab = tabCDN;
                    string ips = string.Join(", ", tbCdnAkamai.Text.Replace("��", ",").Split(',').Select(a => a.Trim()).Where(a => !a.Equals(ip)).ToArray());
                    tbCdnAkamai.Text = string.IsNullOrEmpty(ips) ? ip : ip + ", " + ips;
                    tbCdnAkamai.Focus();
                    tbCdnAkamai.SelectionStart = 0;
                    tbCdnAkamai.SelectionLength = ip.Length;
                    tbCdnAkamai.ScrollToCaret();
                    break;
            }
        }

        private async void CbImportIP_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbImportIP.SelectedIndex == 0) return;

            dgvIpList.Rows.Clear();
            flpTestUrl.Controls.Clear();
            tbDlUrl.Clear();
            cbImportIP.Enabled = false;

            string display = string.Empty, host = string.Empty;
            switch (cbImportIP.SelectedIndex)
            {
                case 1:
                    display = host = "assets1.xboxlive.cn";
                    break;
                case 2:
                    display = host = "dlassets.xboxlive.cn";
                    break;
                case 3:
                    display = host = "tlu.dl.delivery.mp.microsoft.com";
                    break;
                case 4:
                    display = host = "gst.prod.dl.playstation.net";
                    break;
                case 5:
                    display = host = "Akamai";
                    break;
                case 6:
                    display = "Akamai ��ѡ IP";
                    host = "AkamaiV2";
                    break;
                case 7:
                    display = "Akamai IPv6";
                    host = "AkamaiV6";
                    break;
                case 8:
                    display = host = "uplaypc-s-ubisoft.cdn.ubionline.com.cn";
                    break;
            }
            dgvIpList.Tag = host;
            gbIPList.Text = "IP �б� (" + display + ")";

            bool update = true;
            FileInfo fi = new(Path.Combine(resourceDirectory, "IP." + host + ".txt"));
            if (fi.Exists && fi.Length >= 1) update = DateTime.Compare(DateTime.Now, fi.LastWriteTime.AddHours(24)) >= 0;
            if (update)
            {
                await UpdateFile.DownloadIP(fi);
            }
            string content = string.Empty;
            if (fi.Exists)
            {
                using StreamReader sr = fi.OpenText();
                content = sr.ReadToEnd();
            }
            List<DataGridViewRow> list = new();
            Match result = FormImportIP.rMatchIP.Match(content);
            if (result.Success)
            {
                while (result.Success)
                {
                    string ip = result.Groups["IP"].Value;
                    string location = result.Groups["Location"].Value.Trim();

                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvIpList);
                    dgvr.Resizable = DataGridViewTriState.False;
                    if (location.Contains("����"))
                        dgvr.Cells[0].Value = ckbChinaTelecom.Checked;
                    if (location.Contains("��ͨ"))
                        dgvr.Cells[0].Value = ckbChinaUnicom.Checked;
                    if (location.Contains("�ƶ�"))
                        dgvr.Cells[0].Value = ckbChinaMobile.Checked;
                    if (location.Contains("���") || location.Contains("����"))
                        dgvr.Cells[0].Value = ckbHK.Checked;
                    if (location.Contains("̨��"))
                        dgvr.Cells[0].Value = ckbTW.Checked;
                    if (location.Contains("�ձ�"))
                        dgvr.Cells[0].Value = ckbJapan.Checked;
                    if (location.Contains("����"))
                        dgvr.Cells[0].Value = ckbKorea.Checked;
                    if (location.Contains("�¼���"))
                        dgvr.Cells[0].Value = ckbSG.Checked;
                    if (!Regex.IsMatch(location, "����|��ͨ|�ƶ�|���|����|̨��|�ձ�|����|�¼���"))
                        dgvr.Cells[0].Value = ckbOther.Checked;
                    dgvr.Cells[1].Value = ip;
                    dgvr.Cells[2].Value = location;
                    list.Add(dgvr);
                    result = result.NextMatch();
                }
                if (list.Count >= 1)
                {
                    dgvIpList.Rows.AddRange(list.ToArray());
                    dgvIpList.ClearSelection();
                    AddTestUrl(host);
                }
            }
            cbImportIP.Enabled = true;
        }

        private void AddTestUrl(string host)
        {
            switch (host)
            {
                case "assets1.xboxlive.cn":
                case "assets2.xboxlive.cn":
                case "d1.xboxlive.cn":
                case "d2.xboxlive.cn":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://assets1.xboxlive.cn/Z/routing/extraextralarge.txt",
                            Text = "Xbox�����ļ�",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        string[,] games = new string[,]
                        {
                            {"�⻷: ����(XS)", "0698b936-d300-4451-b9a0-0be0514bbbe5_xs", "/Z/eca149b3-7278-44dc-8a0c-bae1bf3c4ea5/0698b936-d300-4451-b9a0-0be0514bbbe5/1.4110.61456.0.989b2038-bcbf-4e3a-a665-0d449076400f/Microsoft.254428597CFE2_1.4110.61456.0_neutral__8wekyb3d8bbwe_xs.xvc" },
                            {"���޾���: ��ƽ��5(PC)", "3d263e92-93cd-4f9b-90c7-5438150cecbf", "/3/be691921-c541-4027-83d0-8e99252ffd97/3d263e92-93cd-4f9b-90c7-5438150cecbf/3.681.890.0.3fe9a0e6-3489-4508-91f4-02e46700f94e/Microsoft.624F8B84B80_3.681.890.0_x64__8wekyb3d8bbwe.msixvc" },
                            {"ս������5(PC)", "1e66a3e7-2f7b-461c-9f46-3ee0aec64b8c", "/8/82e2c767-56a2-4cff-9adf-bc901fd81e1a/1e66a3e7-2f7b-461c-9f46-3ee0aec64b8c/1.1.967.0.4e71a28b-d845-42e5-86bf-36afdd5eb82f/Microsoft.HalifaxBaseGame_1.1.967.0_x64__8wekyb3d8bbwe.msixvc"}
                        };
                        for (int i = 0; i <= games.GetLength(0) - 1; i++)
                        {
                            string? url = null;
                            if (XboxGameDownload.dicXboxGame.TryGetValue(games[i, 1], out XboxGameDownload.Products? XboxGame))
                            {
                                if (XboxGame.Url != null && XboxGame.Version > new Version(Regex.Match(games[i, 2], @"(\d+\.\d+\.\d+\.\d+)").Value))
                                {
                                    url = XboxGame.Url.Replace(".xboxlive.com", ".xboxlive.cn");
                                }
                            }
                            if (string.IsNullOrEmpty(url)) url = "http://assets1.xboxlive.cn" + games[i, 2];
                            LinkLabel lb = new()
                            {
                                Tag = url,
                                Text = games[i, 0],
                                AutoSize = true,
                                Parent = this.flpTestUrl,
                                LinkColor = Color.Green
                            };
                            lb.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        }
                        Label lbTip = new()
                        {
                            ForeColor = Color.Red,
                            Text = "��Ϸ����������",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                    }
                    break;
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/1b5a4a08-06f0-49d6-b25f-d7322c11f3c8/372e2966-b158-4488-8bc8-15ef23db1379/1.5.0.1018.88cd7a5d-f56a-40c7-afd8-85cd4940b891/ACUEU771E1BF7_1.5.0.1018_x64__b6krnev7r9sf8",
                            Text = "�̿�����: �����",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/1d6640d3-3441-42bd-bffd-953d7d09ff5c/26213de4-885d-4eaa-a433-ed5157116507/1.2.1.0.89417ea8-51b5-408c-9283-60c181763a39/Microsoft.Max_1.2.1.0_neutral__ph1m9x8skttmg",
                            Text = "���˹���ֵ�ħ��",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/77d0d59a-34b7-4482-a1c7-c0abbed17de2/db7a9163-9c5e-43a8-b8bf-fe0208149792/1.0.0.3.65565c9c-8a1e-438a-b714-2d9965f0485b/ChildOfLight_1.0.0.3_x64__b6krnev7r9sf8",
                            Text = "��֮��",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/1c4b6e60-b2e3-420c-a8a8-540fb14c9286/57f7a51d-e6c2-42b2-967b-6f075e1923a7/1.0.0.5.acd29c4f-6d78-41c8-a705-90de47b8273b/SHPUPWW446612E0_1.0.0.5_x64__zjr0dfhgjwvde",
                            Text = "�Ϳ���",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        Label lbTip = new()
                        {
                            ForeColor = Color.Red,
                            Text = "XboxOne��������Ϸ��������",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        ToolTip toolTip1 = new()
                        {
                            AutoPopDelay = 30000,
                            IsBalloon = true
                        };
                        toolTip1.SetToolTip(lbTip, "PC����������Ϸ������ʹ�ô�������\n���������Թ�ѡ���Զ���ѡ Akamai IP����ʹ�ù���CDN��������");
                    }
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "986a47b3-0085-4c0c-b3b3-3b806f969b00|MsixBundle|9MV0B5HZVK9Z",
                            Text = "Xbox app(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "64293252-5926-453c-9494-2d4021f1c78d|MsixBundle|9WZDNCRFJBMP",
                            Text = "΢���̵�(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "e0229546-200d-4c66-a693-df9bf799635f|EAppxBundle|9PNQKHFLD2WQ",
                            Text = "���޾���: ��ƽ��4(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "4828c82e-7fe6-4d95-9572-20bbe9721c86|EAppx|9NBLGGH4PBBM",
                            Text = "ս������4(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        Label lbTip = new()
                        {
                            ForeColor = Color.Red,
                            Text = "Ӧ�úͲ���PC��Ϸ",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        ToolTip toolTip1 = new()
                        {
                            AutoPopDelay = 30000,
                            IsBalloon = true
                        };
                        toolTip1.SetToolTip(lbTip, "Xbox app ��ʾ ������Ϸ��֧�ְ�װ���ض��ļ��С����������� Windows Ӧ��һ��װ������\n������Ϸ����ʹ�� tlu.dl.delivery.mp.microsoft.com Ӧ���������ء�");
                    }
                    break;
                case "gst.prod.dl.playstation.net":
                case "gs2.ww.prod.dl.playstation.net":
                case "zeus.dl.playstation.net":
                case "ares.dl.playstation.net":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://gst.prod.dl.playstation.net/networktest/get_192m",
                            Text = "PSN�����ļ�",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://gst.prod.dl.playstation.net/gst/prod/00/PPSA04478_00/app/pkg/26/f_f2e4ff2bc3be11cb844dfe2a7ff8df357d7930152fb5984294a794823ec7472b/EP1464-PPSA04478_00-XXXXXXXXXXXXXXXX_0.pkg",
                            Text = "�Ƕ���(PS5)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://gs2.ww.prod.dl.playstation.net/gs2/appkgo/prod/CUSA03962_00/4/f_526a2fab32d369a8ca6298b59686bf823fa9edfe95acb85bc140c27f810842ce/f/UP0102-CUSA03962_00-BH70000000000001_0.pkg",
                            Text = "����Σ��7(PS4)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://zeus.dl.playstation.net/cdn/UP1004/NPUB31154_00/eISFknCNDxqSsVVywSenkJdhzOIfZjrqKHcuGBHEGvUxQJksdPvRNYbIyWcxFsvH.pkg",
                            Text = "�����Գ���5(PS3)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb5 = new()
                        {
                            Tag = "http://ares.dl.playstation.net/cdn/JP0102/PCSG00350_00/fMBmIgPfrBTVSZCRQFevSzxaPyzFWOuorSKrvdIjDIJwmaGLjpTmRgzLLTJfASFYZMqEpwSknlWocYelXNHMkzXvpbbvtCSymAwWF.pkg",
                            Text = "��������: �߾�G(PSV)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb5.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                    }
                    break;
                case "Akamai":
                case "AkamaiV2":
                case "AkamaiV6":
                case "atum.hac.lp1.d4c.nintendo.net":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "epicgames-download1.akamaized.net":
                case "uplaypc-s-ubisoft.cdn.ubi.com":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt",
                            Text = "Xbox�����ļ�",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://gst.prod.dl.playstation.net/networktest/get_192m",
                            Text = "PSN�����ļ�",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://ctest-dl-lp1.cdn.nintendo.net/30m",
                            Text = "Switch�����ļ�",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://origin-a.akamaihd.net/Origin-Client-Download/origin/live/OriginThinSetup.exe",
                            Text = "Origin(EA)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        if (host == "Akamai" || host == "AkamaiV2" || host == "AkamaiV6")
                        {
                            LinkLabel lb = new()
                            {
                                Name = "UploadBetterAkamaiIp",
                                Text = "�ϴ�������ѡIP",
                                AutoSize = true,
                                Parent = this.flpTestUrl,
                                LinkColor = Color.Red,
                                Enabled = false
                            };
                            lb.LinkClicked += new LinkLabelLinkClickedEventHandler(this.Link_UploadBetterAkamaiIp);
                        }
                    }
                    break;
            }
        }

        private void GetAppUrl(string wuCategoryId, string extension, CancellationToken token = default)
        {
            SetTextBox(tbDlUrl, "���ڻ�ȡ�������ӣ����Ժ�...");
            string? url = null;
            string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload", null, token);
            if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
            {
                XboxPackage.App? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.Code != null && json.Code == "200")
                {
                    url = json.Data?.Where(x => (x.Name ?? string.Empty).ToLower().EndsWith("." + extension)).Select(x => x.Url).FirstOrDefault();
                }
            }
            this.Invoke(new Action(() =>
            {
                tbDlUrl.Text = url;
            }));
        }

        private void LinkTestUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is not LinkLabel link) return;
            string? url = link.Tag.ToString();
            if (url == null) return;
            if (Regex.IsMatch(url, @"^https?://"))
            {
                tbDlUrl.Text = url;
            }
            else if (Regex.IsMatch(url, @"^\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\|"))
            {
                string[] product = url.Split('|');
                string wuCategoryId = product[0];
                string extension = product[1].ToLower();
                ThreadPool.QueueUserWorkItem(delegate { GetAppUrl(wuCategoryId, extension); });
            }
        }

        private void CkbLocation_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            string network = cb.Text;
            bool isChecked = cb.Checked;
            foreach (DataGridViewRow dgvr in dgvIpList.Rows)
            {
                string? location = dgvr.Cells["Col_Location"].Value.ToString();
                if (location == null) continue;
                switch (network)
                {
                    case "����":
                        if (location.Contains("����"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "��ͨ":
                        if (location.Contains("��ͨ"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "�ƶ�":
                        if (location.Contains("�ƶ�"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "���":
                        if (location.Contains("���") || location.Contains("����"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "̨��":
                        if (location.Contains("̨��"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "�ձ�":
                        if (location.Contains("�ձ�"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "����":
                        if (location.Contains("����"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "�¼���":
                        if (location.Contains("�¼���"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    default:
                        if (!Regex.IsMatch(location, "����|��ͨ|�ƶ�|���|����|̨��|�ձ�|����|�¼���"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                }
            }
        }

        private void LinkFindIpArea_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (dgvIpList.Rows.Count == 0)
            {
                MessageBox.Show("���ȵ���IP��", "IP�б�Ϊ��", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            FormIpLocation dialog = new();
            dialog.ShowDialog();
            string key = dialog.key;
            dialog.Dispose();
            if (!string.IsNullOrEmpty(key))
            {
                key = key.Replace("\\", "\\\\")
                    .Replace("(", "\\(")
                    .Replace(")", "\\)")
                    .Replace("[", "\\[")
                    .Replace("]", "\\]")
                    .Replace("{", "\\{")
                    .Replace("}", "\\}")
                    .Replace(".", "\\.")
                    .Replace("+", "\\+")
                    .Replace("*", "\\*")
                    .Replace("?", "\\?")
                    .Replace("^", "\\^")
                    .Replace("$", "\\$")
                    .Replace("|", "\\|");
                key = ".*?" + Regex.Replace(key, @"\s+", ".*?") + ".*?";
                Regex reg = new(@key);
                int rowIndex = 0;
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (dgvr.Cells["Col_Location"].Value == null) continue;
                    string? location = dgvr.Cells["Col_Location"].Value.ToString();
                    if (location != null && reg.IsMatch(location))
                    {
                        dgvr.Cells["Col_Check"].Value = true;
                        dgvIpList.Rows.Remove(dgvr);
                        dgvIpList.Rows.Insert(rowIndex, dgvr);
                        rowIndex++;
                    }
                }
                if (rowIndex >= 1) dgvIpList.Rows[0].Cells[0].Selected = true;
            }
        }

        private void LinkExportIP_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (dgvIpList.Rows.Count == 0) return;
            string? host = dgvIpList.Tag.ToString();
            SaveFileDialog dlg = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Title = "��������",
                Filter = "�ı��ļ�(*.txt)|*.txt",
                FileName = "����IP(" + host + ")"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new();
                sb.AppendLine(host);
                sb.AppendLine("");
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (dgvr.Cells["Col_Speed"].Value != null && !string.IsNullOrEmpty(dgvr.Cells["Col_Speed"].Value.ToString()))
                        sb.AppendLine(dgvr.Cells["Col_IPAddress"].Value + "\t(" + dgvr.Cells["Col_Location"].Value + ")\t" + dgvr.Cells["Col_TTL"].Value + "|" + dgvr.Cells["Col_RoundtripTime"].Value + "|" + dgvr.Cells["Col_Speed"].Value);
                    else
                        sb.AppendLine(dgvr.Cells["Col_IPAddress"].Value + "\t(" + dgvr.Cells["Col_Location"].Value + ")");
                }
                File.WriteAllText(dlg.FileName, sb.ToString());
            }
        }

        private void LinkImportIPManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormImportIP dialog = new();
            dialog.ShowDialog();
            string host = dialog.host;
            DataTable dt = dialog.dt;
            dialog.Dispose();
            if (dt != null && dt.Rows.Count >= 1)
            {
                cbImportIP.SelectedIndex = 0;
                dgvIpList.Rows.Clear();
                flpTestUrl.Controls.Clear();
                tbDlUrl.Clear();
                dgvIpList.Tag = host;
                gbIPList.Text = "IP �б� (" + host + ")";
                List<DataGridViewRow> list = new();
                foreach (DataRow dr in dt.Select("", "Location, IpLong"))
                {
                    string location = dr["Location"].ToString() ?? string.Empty;
                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvIpList);
                    dgvr.Resizable = DataGridViewTriState.False;
                    if (location.Contains("����"))
                        dgvr.Cells[0].Value = ckbChinaTelecom.Checked;
                    if (location.Contains("��ͨ"))
                        dgvr.Cells[0].Value = ckbChinaUnicom.Checked;
                    if (location.Contains("�ƶ�"))
                        dgvr.Cells[0].Value = ckbChinaMobile.Checked;
                    if (location.Contains("���") || location.Contains("����"))
                        dgvr.Cells[0].Value = ckbHK.Checked;
                    if (location.Contains("̨��"))
                        dgvr.Cells[0].Value = ckbTW.Checked;
                    if (location.Contains("�ձ�"))
                        dgvr.Cells[0].Value = ckbJapan.Checked;
                    if (location.Contains("����"))
                        dgvr.Cells[0].Value = ckbKorea.Checked;
                    if (location.Contains("�¼���"))
                        dgvr.Cells[0].Value = ckbSG.Checked;
                    if (!Regex.IsMatch(location, "����|��ͨ|�ƶ�|���|����|̨��|�ձ�|����|�¼���"))
                        dgvr.Cells[0].Value = ckbOther.Checked;
                    dgvr.Cells[1].Value = dr["IP"];
                    dgvr.Cells[2].Value = location;
                    list.Add(dgvr);
                }
                if (list.Count >= 1)
                {
                    dgvIpList.Rows.AddRange(list.ToArray());
                    dgvIpList.ClearSelection();
                    AddTestUrl(host);
                }
            }
        }

        private void TsmUseIPHosts_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            string? host = dgvIpList.Tag.ToString();
            string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(ip)) return;

            try
            {
                string sHosts;
                FileInfo fi = new(Environment.SystemDirectory + "\\drivers\\etc\\hosts");
                using (FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    using StreamReader sr = new(fs);
                    sHosts = sr.ReadToEnd();
                }
                StringBuilder sb = new();
                string msg = string.Empty;
                switch (host)
                {
                    case "assets1.xboxlive.cn":
                    case "assets2.xboxlive.cn":
                    case "d1.xboxlive.cn":
                    case "d2.xboxlive.cn":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+(assets1|assets2|d1|d2)\.xboxlive\.cn\s+# (XboxDownload|Xbox��������)\r\n", "");
                        sb.AppendLine(ip + " assets1.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " assets2.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " d1.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " d2.xboxlive.cn # XboxDownload");
                        msg = "\nXbox��PC�̵���Ϸ���ؿ��ܻ�ʹ��com������ֻд��cn�������ٲ�һ����Ч��";
                        break;
                    case "dlassets.xboxlive.cn":
                    case "dlassets2.xboxlive.cn":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+(dlassets2?)\.xboxlive\.cn\s+# (XboxDownload|Xbox��������)\r\n", "");
                        sb.AppendLine(ip + " dlassets.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " dlassets2.xboxlive.cn # XboxDownload");
                        msg = "\nXbox��PC�̵���Ϸ���ؿ��ܻ�ʹ��com������ֻд��cn�������ٲ�һ����Ч��";
                        break;
                    case "dl.delivery.mp.microsoft.com":
                    case "tlu.dl.delivery.mp.microsoft.com":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+((tlu\.)?dl\.delivery\.mp\.microsoft\.com)\s+# (XboxDownload|Xbox��������)\r\n", "");
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com # XboxDownload");
                        break;
                    case "gst.prod.dl.playstation.net":
                    case "gs2.ww.prod.dl.playstation.net":
                    case "zeus.dl.playstation.net":
                    case "ares.dl.playstation.net":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+[^\s]+\.dl\.playstation\.net\s+# (XboxDownload|Xbox��������)\r\n", "");
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " zeus.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " ares.dl.playstation.net # XboxDownload");
                        break;
                    case "Akamai":
                    case "AkamaiV2":
                    case "AkamaiV6":
                    case "atum.hac.lp1.d4c.nintendo.net":
                    case "origin-a.akamaihd.net":
                    case "blzddist1-a.akamaihd.net":
                    case "epicgames-download1.akamaized.net":
                    case "uplaypc-s-ubisoft.cdn.ubi.com":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+[^\s]+(\.xboxlive\.com|\.delivery\.mp\.microsoft\.com|\.dl\.playstation\.net|\.nintendo\.net|\.cdn\.ea\.com|\.akamaihd\.net|\.akamaized\.net|\.ubi\.net)\s+# (XboxDownload|Xbox��������)\r\n", "");
                        sb.AppendLine(ip + " xvcf1.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " xvcf2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " assets1.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " assets2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " d1.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " d2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " dlassets.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " dlassets2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " zeus.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " ares.dl.playstation.net # XboxDownload");
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sb.AppendLine(ip + " atum.hac.lp1.d4c.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " nemof.p01.lp1.nemo.srv.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " nemof.hac.lp1.nemo.srv.nintendo.net # XboxDownload"); 
                            sb.AppendLine(ip + " ctest-ul-lp1.cdn.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " ctest-dl.p01.lp1.ctest.srv.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " ctest-ul.p01.lp1.ctest.srv.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " ctest-dl-lp1.cdn.nintendo.net # XboxDownload");
                            sb.AppendLine("0.0.0.0 atum-eda.hac.lp1.d4c.nintendo.net # XboxDownload");
                            sb.AppendLine("0.0.0.0 atum-4ff.hac.lp1.d4c.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " origin-a.akamaihd.net # XboxDownload");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com # XboxDownload");
                            sb.AppendLine(ip + " downloader.battle.net # XboxDownload");
                            sb.AppendLine(ip + " blzddist1-a.akamaihd.net # XboxDownload");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net # XboxDownload");
                            sb.AppendLine(ip + " uplaypc-s-ubisoft.cdn.ubi.com # XboxDownload");
                            sb.AppendLine(ip + " ubisoftconnect.cdn.ubi.com # XboxDownload");
                            msg = "\n\nս����Epic������ ��Ҫʹ�ü�����ʽ��ת��";
                        }
                        else
                        {
                            sb.AppendLine(ip + " origin-a.akamaihd.net # XboxDownload");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com # XboxDownload");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net # XboxDownload");
                            msg = "\n\nNS������ս���ͻ��ˡ����̿ͻ��� ��֧��ʹ�� IPv6��";
                        }
                        break;
                    case "uplaypc-s-ubisoft.cdn.ubionline.com.cn":
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sHosts = Regex.Replace(sHosts, @"[^\s]+\s+[^\s]+\.cdn\.ubionline\.com\.cn\s+# (XboxDownload|Xbox��������)\r\n", "");
                            sb.AppendLine(ip + " " + host + " # XboxDownload");
                        }
                        else
                        {
                            MessageBox.Show("���̿ͻ��˲�֧��ʹ��IPv6����ʹ�ü�����ʽ��", "�ͻ��˲�֧��", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        break;
                    default:
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+" + host + @"\s+# (XboxDownload|Xbox��������)\r\n", "");
                        sb.AppendLine(ip + " " + host + " # XboxDownload");
                        break;
                }
                sHosts = sHosts.Trim() + "\r\n" + sb.ToString();
                FileSecurity fSecurity = fi.GetAccessControl();
                fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
                if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    fi.Attributes = FileAttributes.Normal;
                using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    if (!string.IsNullOrEmpty(sHosts.Trim()))
                    {
                        using StreamWriter sw = new(fs);
                        sw.WriteLine(sHosts.Trim());
                    }
                }
                fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
                MessageBox.Show("ϵͳHosts�ļ�д��ɹ������¹�����д��ϵͳHosts�ļ�\n\n" + sb.ToString() + msg, "д��ϵͳHosts�ļ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("д��ϵͳHosts�ļ�ʧ�ܣ�������Ϣ��" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TsmExportRule_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            string? host = dgvIpList.Tag.ToString();
            string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(ip)) return;

            StringBuilder sb = new();
            if (sender is not ToolStripMenuItem tsmi) return;
            string msg = string.Empty;
            switch (host)
            {
                case "assets1.xboxlive.cn":
                case "assets2.xboxlive.cn":
                case "d1.xboxlive.cn":
                case "d2.xboxlive.cn":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/assets1.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/assets2.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/d1.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/d2.xboxlive.cn/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " assets1.xboxlive.cn");
                        sb.AppendLine(ip + " assets2.xboxlive.cn");
                        sb.AppendLine(ip + " d1.xboxlive.cn");
                        sb.AppendLine(ip + " d2.xboxlive.cn");
                    }
                    msg = "\nXbox��PC�̵���Ϸ���ؿ��ܻ�ʹ��com������ֻд��cn�������ٲ�һ����Ч��";
                    break;
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/dlassets.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/dlassets2.xboxlive.cn/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " dlassets.xboxlive.cn");
                        sb.AppendLine(ip + " dlassets2.xboxlive.cn");
                    }
                    msg = "\nXbox��PC�̵���Ϸ���ؿ��ܻ�ʹ��com������ֻд��cn�������ٲ�һ����Ч��";
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine("address=/tlu.dl.delivery.mp.microsoft.com/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com");
                    }
                    break;
                case "gst.prod.dl.playstation.net":
                case "gs2.ww.prod.dl.playstation.net":
                case "zeus.dl.playstation.net":
                case "ares.dl.playstation.net":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/gst.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/gs2.ww.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/zeus.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/ares.dl.playstation.net/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net");
                        sb.AppendLine(ip + " zeus.dl.playstation.net");
                        sb.AppendLine(ip + " ares.dl.playstation.net");
                    }
                    break;
                case "Akamai":
                case "AkamaiV2":
                case "AkamaiV6":
                case "atum.hac.lp1.d4c.nintendo.net":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "epicgames-download1.akamaized.net":
                case "uplaypc-s-ubisoft.cdn.ubi.com":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("# Xbox ��������");
                        sb.AppendLine("address=/xvcf1.xboxlive.com/" + ip);
                        sb.AppendLine("address=/xvcf2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/assets1.xboxlive.com/" + ip);
                        sb.AppendLine("address=/assets2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/d1.xboxlive.com/" + ip);
                        sb.AppendLine("address=/d2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/dlassets.xboxlive.com/" + ip);
                        sb.AppendLine("address=/dlassets2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine("address=/tlu.dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine();
                        sb.AppendLine("# PlayStation");
                        sb.AppendLine("address=/gst.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/gs2.ww.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/zeus.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/ares.dl.playstation.net/" + ip);
                        sb.AppendLine();
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sb.AppendLine("# Nintendo Switch");
                            sb.AppendLine("address=/atum.hac.lp1.d4c.nintendo.net/" + ip);
                            sb.AppendLine("address=/nemof.p01.lp1.nemo.srv.nintendo.net/" + ip);
                            sb.AppendLine("address=/nemof.hac.lp1.nemo.srv.nintendo.net/" + ip); 
                            sb.AppendLine("address=/ctest-dl.p01.lp1.ctest.srv.nintendo.net/" + ip);
                            sb.AppendLine("address=/ctest-ul.p01.lp1.ctest.srv.nintendo.net/" + ip);
                            sb.AppendLine("address=/ctest-ul-lp1.cdn.nintendo.net/" + ip);
                            sb.AppendLine("address=/ctest-dl-lp1.cdn.nintendo.net/" + ip);
                            sb.AppendLine("address=/atum-eda.hac.lp1.d4c.nintendo.net/0.0.0.0");
                            sb.AppendLine("address=/atum-4ff.hac.lp1.d4c.nintendo.net/0.0.0.0");
                            sb.AppendLine();
                            sb.AppendLine("# EA��ս����Epic������");
                            sb.AppendLine("address=/origin-a.akamaihd.net/" + ip);
                            sb.AppendLine("address=/ssl-lvlt.cdn.ea.com/0.0.0.0");
                            sb.AppendLine("address=/downloader.battle.net/" + ip);
                            sb.AppendLine("address=/blzddist1-a.akamaihd.net/" + ip);
                            sb.AppendLine("address=/epicgames-download1.akamaized.net/" + ip);
                            sb.AppendLine("address=/uplaypc-s-ubisoft.cdn.ubi.com/" + ip);
                            sb.AppendLine("address=/ubisoftconnect.cdn.ubi.com/" + ip);
                            msg = "\n\nս����Epic������ ��Ҫʹ�ü�����ʽ��ת��";
                        }
                        else
                        {
                            sb.AppendLine("# EA��Epic");
                            sb.AppendLine("address=/origin-a.akamaihd.net/" + ip);
                            sb.AppendLine("address=/ssl-lvlt.cdn.ea.com/0.0.0.0");
                            sb.AppendLine("address=/epicgames-download1.akamaized.net/" + ip);
                            msg = "\n\nNS������ս���ͻ��ˡ����̿ͻ��� ��֧��ʹ�� IPv6��";
                        }
                    }
                    else
                    {
                        sb.AppendLine("# Xbox ��������");
                        sb.AppendLine(ip + " xvcf1.xboxlive.com");
                        sb.AppendLine(ip + " xvcf2.xboxlive.com");
                        sb.AppendLine(ip + " assets1.xboxlive.com");
                        sb.AppendLine(ip + " assets2.xboxlive.com");
                        sb.AppendLine(ip + " d1.xboxlive.com");
                        sb.AppendLine(ip + " d2.xboxlive.com");
                        sb.AppendLine(ip + " dlassets.xboxlive.com");
                        sb.AppendLine(ip + " dlassets2.xboxlive.com");
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com");
                        sb.AppendLine();
                        sb.AppendLine("# PlayStation");
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net");
                        sb.AppendLine(ip + " zeus.dl.playstation.net");
                        sb.AppendLine(ip + " ares.dl.playstation.net");
                        sb.AppendLine();
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sb.AppendLine("# Nintendo Switch");
                            sb.AppendLine(ip + " atum.hac.lp1.d4c.nintendo.net");
                            sb.AppendLine(ip + " nemof.p01.lp1.nemo.srv.nintendo.net");
                            sb.AppendLine(ip + " nemof.hac.lp1.nemo.srv.nintendo.net"); 
                            sb.AppendLine(ip + " ctest-dl.p01.lp1.ctest.srv.nintendo.net");
                            sb.AppendLine(ip + " ctest-ul.p01.lp1.ctest.srv.nintendo.net");
                            sb.AppendLine(ip + " ctest-ul-lp1.cdn.nintendo.net");
                            sb.AppendLine(ip + " ctest-dl-lp1.cdn.nintendo.net");
                            sb.AppendLine("0.0.0.0 atum-eda.hac.lp1.d4c.nintendo.net");
                            sb.AppendLine("0.0.0.0 atum-4ff.hac.lp1.d4c.nintendo.net");
                            sb.AppendLine();
                            sb.AppendLine("# EA��ս����Epic������");
                            sb.AppendLine(ip + " origin-a.akamaihd.net");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                            sb.AppendLine(ip + " downloader.battle.net");
                            sb.AppendLine(ip + " blzddist1-a.akamaihd.net");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net");
                            sb.AppendLine(ip + " uplaypc-s-ubisoft.cdn.ubi.com");
                            sb.AppendLine(ip + " ubisoftconnect.cdn.ubi.com");
                            msg = "\nOrigin ���û������ڡ����� -> EA Origin �л�CDN����������ָ��ʹ�� Akamai��\n\nս����Epic������ ��Ҫʹ�ü�����ʽ��ת��";
                        }
                        else
                        {
                            sb.AppendLine("# EA��ս����Epic������");
                            sb.AppendLine(ip + " origin-a.akamaihd.net");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net");
                            msg = "\nOrigin ���û������ڡ����� -> EA Origin �л�CDN����������ָ��ʹ�� Akamai��\n\nNS������ս���ͻ��ˡ����̿ͻ��� ��֧��ʹ�� IPv6��";
                        }
                    }
                    break;
                case "uplaypc-s-ubisoft.cdn.ubionline.com.cn":
                    if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                    {
                        sb.AppendLine("# ����");
                        if (tsmi.Name == "tsmDNSmasp")
                            sb.AppendLine("address=/" + host + "/" + ip);
                        else
                            sb.AppendLine(ip + " " + host);
                    }
                    else
                    {
                        MessageBox.Show("���̿ͻ��˲�֧��ʹ��IPv6����ʹ�ü�����ʽ��", "�ͻ��˲�֧��", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    break;
                default:
                    if (tsmi.Name == "tsmDNSmasp")
                        sb.AppendLine("address=/" + host + "/" + ip);
                    else
                        sb.AppendLine(ip + " " + host);
                    break;
            }
            Clipboard.SetDataObject(sb.ToString());
            MessageBox.Show("���¹����Ѹ��Ƶ�������\n\n" + sb.ToString() + msg, "��������", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TsmSpeedTest_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            List<DataGridViewRow> ls = new()
            {
                dgvIpList.SelectedRows[0]
            };
            dgvIpList.ClearSelection();
            if (string.IsNullOrEmpty(tbDlUrl.Text) && flpTestUrl.Controls.Count >= 1)
            {
                LinkLabel? link = flpTestUrl.Controls[0] as LinkLabel;
                tbDlUrl.Text = link?.Tag.ToString();
            }
            foreach (Control control in this.panelSpeedTest.Controls)
            {
                if (control is TextBox || control is CheckBox || control is Button || control is ComboBox || control is LinkLabel || control is FlowLayoutPanel)
                    control.Enabled = false;
            }
            Col_IPAddress.SortMode = Col_Location.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = Col_Speed.SortMode = DataGridViewColumnSortMode.NotSortable;
            var timeout = cbSpeedTestTimeOut.SelectedIndex switch
            {
                0 => 3000,
                1 => 5000,
                _ => 10000,
            };
            ThreadPool.QueueUserWorkItem(delegate { SpeedTest(ls, timeout); });
        }

        private void TsmSpeedTestLog_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            if (dgvr.Tag != null) MessageBox.Show(dgvr.Tag.ToString(), "Request Headers", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private async void Link_UploadBetterAkamaiIp(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel linkLabel = (LinkLabel)sender;
            string text = Regex.Replace(linkLabel.Text, @"\(.+\)", "").Trim();
            linkLabel.Text = text;
            JsonArray ja = new();
            foreach (DataGridViewRow dgvr in dgvIpList.Rows)
            {
                if (dgvr.Cells["Col_Speed"].Value == null) continue;
                if (double.TryParse(dgvr.Cells["Col_Speed"].Value.ToString(), out double speed) && speed >= 15)
                {
                    string? _ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
                    string? _location = dgvr.Cells["Col_Location"].Value.ToString();
                    if (!string.IsNullOrEmpty(_ip) && !string.IsNullOrEmpty(_location))
                    {
                        ja.Add(new JsonObject()
                        {
                            ["ip"] = _ip,
                            ["location"] = _location,
                            ["speed"] = speed
                        });
                    }
                }
            }
            if (ja.Count >= 1 && MessageBox.Show("�˹�������й���½�����û�ʹ�ã����й���½��������ͨ���ʾ�Ʒ����ר�� ����ʹ�� ��������������� ���ٵ��û��벻Ҫ�ϴ���лл������\n\n���� IP �������ٶȳ���15MB/s�������ϴ��� ��Akamai ��ѡ IP�� �б��Ƿ������\n" + string.Join("\n", ja.Select(a => a!["ip"] + "\t" + a!["location"] + "\t" + a!["speed"]).ToArray()), "�ϴ����� Akamai ��ѡ IP", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                linkLabel.Text = text + " (���λ��)";
                bool bCheckLocation = false;
                using (HttpResponseMessage? response = await ClassWeb.HttpResponseMessageAsync("https://qifu-api.baidubce.com/ip/local/geo/v1/district", "GET", null, null, null, 6000))
                {
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        JsonDocument? jsonDocument = null;
                        try
                        {
                            jsonDocument = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                        }
                        catch { }
                        if (jsonDocument != null)
                        {
                            JsonElement root = jsonDocument.RootElement;
                            if (root.TryGetProperty("data", out JsonElement je))
                            {
                                string country = je.TryGetProperty("country", out JsonElement jeCountry) ? jeCountry.ToString().Trim() : "";
                                string prov = je.TryGetProperty("prov", out JsonElement jeProv) ? jeProv.ToString().Trim() : "";
                                bCheckLocation = country == "�й�" && !Regex.IsMatch(prov, @"���|����|̨��");
                            }
                        }
                    }
                }
                if (bCheckLocation)
                {
                    linkLabel.Text = text + " (�����ϴ�)";
                    using HttpResponseMessage? response2 = await ClassWeb.HttpResponseMessageAsync(UpdateFile.website + "/Akamai/Better", "POST", ja.ToString(), "application/json", null, 6000, "XboxDownload");
                    if (response2 != null && response2.IsSuccessStatusCode)
                    {
                        string ipFilepath = Path.Combine(resourceDirectory, "IP.AkamaiV2.txt");
                        if (File.Exists(ipFilepath)) File.SetLastWriteTime(ipFilepath, DateTime.Now.AddDays(-7));
                        linkLabel.Text = text + " (�ϴ��ɹ�)";
                    }
                    else
                        linkLabel.Text = text + " (�ϴ�ʧ��)";
                }
                else
                {
                    linkLabel.Text = text + " (���й���½����)";
                }
            }
        }

        private void LinkHostsClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string sHosts;
                FileInfo fi = new(Environment.SystemDirectory + "\\drivers\\etc\\hosts");
                using (FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    using StreamReader sr = new(fs);
                    sHosts = sr.ReadToEnd();
                }
                StringBuilder sb1 = new(), sb2 = new();
                string header = string.Empty;
                Match result = Regex.Match(sHosts, @"# Added by (XboxDownload|Xbox��������)\r\n(.*\r\n)*# End of (XboxDownload|Xbox��������)\r\n");
                if (result.Success)
                {
                    header = result.Groups[0].Value;
                    sHosts = sHosts.Replace(header, "");
                    if (!bServiceFlag)
                    {
                        sb2.Append(header);
                        header = string.Empty;
                    }
                }
                foreach (string str in sHosts.Split('\n'))
                {
                    string tmp = str.Trim();
                    if (tmp.StartsWith('#') || string.IsNullOrEmpty(tmp))
                        sb1.AppendLine(tmp);
                    else
                        sb2.AppendLine(tmp);
                }
                if (sb2.Length == 0)
                {
                    MessageBox.Show("Hosts�ļ�û��д���κι������������", "���ϵͳHosts�ļ�", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
                else if (MessageBox.Show("�Ƿ�ȷ���������д�����\n\n" + sb2.ToString(), "���ϵͳHosts�ļ�", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    FileSecurity fSecurity = fi.GetAccessControl();
                    fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                        fi.Attributes = FileAttributes.Normal;
                    using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        string hosts = string.Empty;
                        if (bServiceFlag && !Properties.Settings.Default.SetDns) hosts = (header + sb1.ToString()).Trim();
                        else hosts = sb1.ToString().Trim();
                        using StreamWriter sw = new(fs);
                        if (hosts.Length > 0)
                            sw.WriteLine(hosts);
                        else
                            sw.Write(hosts);
                    }
                    fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("���ϵͳHosts�ļ�ʧ�ܣ�������Ϣ��" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkHostsEdit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string sHostsPath = Environment.SystemDirectory + "\\drivers\\etc\\hosts";
            if (File.Exists(sHostsPath))
                Process.Start("notepad.exe", sHostsPath);
            else
                MessageBox.Show("Hosts �ļ�������", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void ButSpeedTest_Click(object sender, EventArgs? e)
        {
            if (ctsSpeedTest == null)
            {
                if (dgvIpList.Rows.Count == 0)
                {
                    MessageBox.Show("���ȵ���IP��", "IP�б�Ϊ��", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                List<DataGridViewRow> ls = new();
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (Convert.ToBoolean(dgvr.Cells["Col_Check"].Value))
                    {
                        ls.Add(dgvr);
                    }
                }
                if (ls.Count == 0)
                {
                    MessageBox.Show("�빴ѡ��Ҫ����IP��", "ѡ�����IP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int rowIndex = 0;
                foreach (DataGridViewRow dgvr in ls.ToArray())
                {
                    dgvIpList.Rows.Remove(dgvr);
                    dgvIpList.Rows.Insert(rowIndex, dgvr);
                    rowIndex++;
                }
                dgvIpList.Rows[0].Cells[0].Selected = true;

                butSpeedTest.Text = "ֹͣ����";
                foreach (Control control in this.panelSpeedTest.Controls)
                {
                    switch (control.Name)
                    {
                        case "linkHostsClear":
                        case "linkHostsEdit":
                        case "butSpeedTest":
                            break;
                        default:
                            control.Enabled = false;
                            break;
                    }
                }
                Col_IPAddress.SortMode = Col_Location.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = Col_Speed.SortMode = DataGridViewColumnSortMode.NotSortable;
                Col_Check.ReadOnly = true;
                var timeout = cbSpeedTestTimeOut.SelectedIndex switch
                {
                    0 => 3000,
                    1 => 5000,
                    _ => 10000,
                };
                Thread thread = new(new ThreadStart(() =>
                {
                    SpeedTest(ls, timeout);
                }))
                {
                    IsBackground = true
                };
                thread.Start();
            }
            else
            {
                butSpeedTest.Enabled = false;
                ctsSpeedTest.Cancel();
            }
            dgvIpList.ClearSelection();
        }

        CancellationTokenSource? ctsSpeedTest = null;
        private void SpeedTest(List<DataGridViewRow> ls, int timeout)
        {
            ctsSpeedTest = new CancellationTokenSource();
            string url = tbDlUrl.Text.Trim();
            if (!Regex.IsMatch(tbDlUrl.Text, @"^https?://") && flpTestUrl.Controls.Count >= 1)
            {
                if (flpTestUrl.Controls[0] is LinkLabel link)
                {
                    url = link.Tag.ToString() ?? string.Empty;
                    if (Regex.IsMatch(url, @"^https?://"))
                    {
                        SetTextBox(tbDlUrl, url);
                    }
                    else if (Regex.IsMatch(url, @"^\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\|"))
                    {
                        string[] product = url.Split('|');
                        string wuCategoryId = product[0];
                        string extension = product[1].ToLower();
                        ls[0].Cells["Col_Speed"].Value = "��ȡ��������";
                        GetAppUrl(wuCategoryId, extension, ctsSpeedTest.Token);
                        if (ctsSpeedTest.IsCancellationRequested)
                        {
                            ls[0].Cells["Col_Speed"].Value = null;
                        }
                        url = tbDlUrl.Text;
                        if (!Regex.IsMatch(url, @"^https?://"))
                        {
                            url = flpTestUrl.Controls[2].Tag.ToString() ?? string.Empty;
                            SetTextBox(tbDlUrl, url);
                        }
                    }
                }
            }
            Uri? uri = null;
            if (Regex.IsMatch(url, @"^https?://"))
            {
                try
                {
                    uri = new Uri(url);
                }
                catch { }
            }
            string? _tag = dgvIpList.Tag.ToString();
            if (uri != null)
            {
                int range = Regex.IsMatch(gbIPList.Text, @"Akamai") ? 31457279 : 52428799;  //����IP��������30M������IP��������50M
                //if (Form1.debug) range = 1048575;     //1M

                string userAgent = uri.Host.EndsWith(".nintendo.net") ? "XboxDownload/Nintendo NX" : "XboxDownload";
                Stopwatch sw = new();
                StringBuilder sb = new();
                sb.AppendLine("GET " + uri.PathAndQuery + " HTTP/1.1");
                sb.AppendLine("Host: " + uri.Host);
                sb.AppendLine("User-Agent: " + userAgent);
                sb.AppendLine("Range: bytes=0-" + range);
                sb.AppendLine();
                byte[] buffer = Encoding.ASCII.GetBytes(sb.ToString());
                using Ping p1 = new();
                foreach (DataGridViewRow dgvr in ls)
                {
                    if (ctsSpeedTest.IsCancellationRequested) break;
                    string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
                    if (string.IsNullOrEmpty(ip)) continue;
                    dgvr.Cells["Col_302"].Value = false;
                    dgvr.Cells["Col_TTL"].Value = null;
                    dgvr.Cells["Col_RoundtripTime"].Value = null;
                    dgvr.Cells["Col_Speed"].Value = "���ڲ���";
                    dgvr.Cells["Col_RoundtripTime"].Style.ForeColor = Color.Empty;
                    dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Empty;
                    dgvr.Tag = null;

                    Task[] tasks = new Task[2];
                    tasks[0] = new Task(() =>
                    {
                        try
                        {
                            PingReply reply = p1.Send(ip);
                            if (reply.Status == IPStatus.Success)
                            {
                                dgvr.Cells["Col_TTL"].Value = reply.Options?.Ttl;
                                dgvr.Cells["Col_RoundtripTime"].Value = reply.RoundtripTime;
                            }
                        }
                        catch { }
                    });
                    tasks[1] = new Task(() =>
                    {
                        sw.Restart();
                        SocketPackage socketPackage = uri.Scheme == "https" ? ClassWeb.TlsRequest(uri, buffer, ip, false, null, timeout, ctsSpeedTest) : ClassWeb.TcpRequest(uri, buffer, ip, false, null, timeout, ctsSpeedTest);
                        sw.Stop();
                        if (socketPackage.Headers.StartsWith("HTTP/1.1 302"))
                        {
                            dgvr.Cells["Col_302"].Value = true;
                            Match result = Regex.Match(socketPackage.Headers, @"Location: (.+)");
                            if (result.Success)
                            {
                                Uri uri2 = new(uri, result.Groups[1].Value);
                                dgvr.Tag = socketPackage.Headers + "===============��ʱ���ض���(302)===============\n" + uri2.OriginalString + "\n\n";
                                sb.Clear();
                                sb.AppendLine("GET " + uri2.PathAndQuery + " HTTP/1.1");
                                sb.AppendLine("Host: " + uri2.Host);
                                sb.AppendLine("User-Agent: " + userAgent);
                                sb.AppendLine("Range: bytes=0-" + range);
                                sb.AppendLine();
                                byte[] buffer2 = Encoding.ASCII.GetBytes(sb.ToString());
                                sw.Restart();
                                socketPackage = uri2.Scheme == "https" ? ClassWeb.TlsRequest(uri2, buffer2, null, false, null, timeout, ctsSpeedTest) : ClassWeb.TcpRequest(uri2, buffer2, null, false, null, timeout, ctsSpeedTest);
                                sw.Stop();
                            }
                        }
                        dgvr.Tag += string.IsNullOrEmpty(socketPackage.Err) ? socketPackage.Headers : socketPackage.Err;
                        if (socketPackage.Headers.StartsWith("HTTP/1.1 206") && socketPackage.Buffer != null)
                        {
                            double speed = Math.Round((double)(socketPackage.Buffer.Length) / sw.ElapsedMilliseconds * 1000 / 1024 / 1024, 2, MidpointRounding.AwayFromZero);
                            dgvr.Cells["Col_Speed"].Value = speed;
                            dgvr.Tag += "���أ�" + ClassMbr.ConvertBytes((ulong)socketPackage.Buffer.Length) + "����ʱ��" + sw.ElapsedMilliseconds.ToString("N0") + " ���룬ƽ���ٶȣ�" + speed + " MB/s";
                        }
                        else
                        {
                            dgvr.Cells["Col_Speed"].Value = (double)0;
                            dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Red;
                        }
                        socketPackage.Buffer = null;
                    });
                    Array.ForEach(tasks, x => x.Start());
                    Task.WaitAll(tasks);
                }
            }
            else
            {
                using Ping p1 = new();
                foreach (DataGridViewRow dgvr in ls)
                {
                    if (ctsSpeedTest.IsCancellationRequested) break;
                    string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
                    if (string.IsNullOrEmpty(ip)) continue;
                    dgvr.Cells["Col_302"].Value = false;
                    dgvr.Cells["Col_TTL"].Value = null;
                    dgvr.Cells["Col_RoundtripTime"].Value = null;
                    dgvr.Cells["Col_Speed"].Value = "���ڲ���";
                    dgvr.Cells["Col_RoundtripTime"].Style.ForeColor = Color.Empty;
                    dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Empty;
                    dgvr.Tag = null;
                    try
                    {
                        PingReply reply = p1.Send(ip);
                        if (reply.Status == IPStatus.Success)
                        {
                            dgvr.Cells["Col_TTL"].Value = reply.Options?.Ttl;
                            dgvr.Cells["Col_RoundtripTime"].Value = reply.RoundtripTime;
                        }
                    }
                    catch { }
                    dgvr.Cells["Col_Speed"].Value = null;
                }
            }
            GC.Collect();
            ctsSpeedTest = null;

            bool bUploadBetterAkamaiIpEnable = false;
            LinkLabel? linkUploadBetterAkamaiIp = null;
            if (_tag == "Akamai" || _tag == "AkamaiV2" || _tag == "AkamaiV6")
            {
                linkUploadBetterAkamaiIp = this.Controls.Find("UploadBetterAkamaiIp", true)[0] as LinkLabel;
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (dgvr.Cells["Col_Speed"].Value == null) continue;
                    if (double.TryParse(dgvr.Cells["Col_Speed"].Value.ToString(), out double speed) && speed >= 15)
                    {
                        bUploadBetterAkamaiIpEnable = true;
                        break;
                    }
                }
            }

            this.Invoke(new Action(() =>
            {
                butSpeedTest.Text = "��ʼ����";
                foreach (Control control in this.panelSpeedTest.Controls)
                {
                    control.Enabled = true;
                }
                Col_IPAddress.SortMode = Col_Location.SortMode = Col_Speed.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = DataGridViewColumnSortMode.Automatic;
                Col_Check.ReadOnly = false;
                butSpeedTest.Enabled = true;
                if (linkUploadBetterAkamaiIp != null) linkUploadBetterAkamaiIp.Enabled = bUploadBetterAkamaiIpEnable;
            }));
        }
        #endregion

        #region ѡ�-����
        private void DgvHosts_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["Col_Enable"].Value = true;
        }

        private void DgvHosts_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            dgvHosts.Rows[e.RowIndex].ErrorText = "";
            if (dgvHosts.Rows[e.RowIndex].IsNewRow) return;
            switch (dgvHosts.Columns[e.ColumnIndex].Name)
            {
                case "Col_IP":
                    if (!string.IsNullOrWhiteSpace(e.FormattedValue.ToString()))
                    {
                        if (!(IPAddress.TryParse(e.FormattedValue.ToString()?.Trim(), out _)))
                        {
                            e.Cancel = true;
                            dgvHosts.Rows[e.RowIndex].ErrorText = "������ЧIP��ַ";
                        }
                    }
                    break;
            }
        }

        private void DgvHosts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            switch (dgvHosts.Columns[e.ColumnIndex].Name)
            {
                case "Col_HostName":
                    dgvHosts.CurrentCell.Value = Regex.Replace((dgvHosts.CurrentCell.FormattedValue.ToString() ?? string.Empty).Trim().ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2");
                    break;
                case "Col_IP":
                    dgvHosts.CurrentCell.Value = dgvHosts.CurrentCell.FormattedValue.ToString()?.Trim();
                    break;
            }
        }

        private void DgvHosts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;
            DataGridViewRow dgvr = dgvHosts.Rows[e.RowIndex];
            string? hostName = dgvr.Cells["Col_HostName"].Value?.ToString();
            string? ip = dgvr.Cells["Col_IP"].Value?.ToString();
            if (!string.IsNullOrEmpty(hostName) && !string.IsNullOrEmpty(ip) && DnsListen.reHosts.IsMatch(hostName))
            {
                FormConnectTest dialog = new(hostName, ip);
                dialog.ShowDialog();
                dialog.Dispose();
            }
        }

        private void CbHosts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbHosts.SelectedIndex <= 0) return;
            if (cbHosts.Text == "Xbox360���������ϴ�")
            {
                string[] hostNames = new string[] { "download.xbox.com", "download.xbox.com.edgesuite.net", "xbox-ecn102.vo.msecnd.net" };
                foreach (string hostName in hostNames)
                {
                    DataRow[] rows = dtHosts.Select("HostName='" + hostName + "'");
                    if (rows.Length >= 1)
                    {
                        rows[0]["Enable"] = true;
                        rows[0]["IP"] = Properties.Settings.Default.LocalIP;
                        rows[0]["Remark"] = "Xbox360������������";
                    }
                    else
                    {
                        DataRow dr = dtHosts.NewRow();
                        dr["Enable"] = true;
                        dr["HostName"] = hostName;
                        dr["IP"] = Properties.Settings.Default.LocalIP;
                        dr["Remark"] = "Xbox360������������";
                        dtHosts.Rows.Add(dr);
                    }
                    dgvHosts.ClearSelection();
                }
                DataGridViewRow? dgvr = dgvHosts.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Col_HostName"].Value.ToString() == hostNames[0]).Select(r => r).FirstOrDefault();
                if (dgvr != null) dgvr.Cells["Col_IP"].Selected = true;
            }
        }

        private void ButHostSave_Click(object sender, EventArgs e)
        {
            dtHosts.AcceptChanges();
            string hostFilepath = Path.Combine(resourceDirectory, "Hosts.xml");
            if (dtHosts.Rows.Count >= 1)
            {
                if (!Directory.Exists(resourceDirectory)) Directory.CreateDirectory(resourceDirectory);
                dtHosts.WriteXml(hostFilepath);
            }
            else if (File.Exists(hostFilepath))
            {
                File.Delete(hostFilepath);
            }
            dgvHosts.ClearSelection();
            if (bServiceFlag)
            {
                DnsListen.UpdateHosts();
                if (ckbBetterAkamaiIP.Checked) ckbBetterAkamaiIP.Checked = false;
                else UpdateHosts(true);
                if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
            }
        }

        private void ButHostReset_Click(object sender, EventArgs e)
        {
            dtHosts.RejectChanges();
            dgvHosts.ClearSelection();
        }

        private void LinkHostsAdd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormHost dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
            string host = dialog.host, ip = dialog.ip;
            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(ip))
            {
                DataRow[] rows = dtHosts.Select("HostName='" + host + "'");
                DataRow dr;
                if (rows.Length >= 1)
                {
                    dr = rows[0];
                }
                else
                {
                    dr = dtHosts.NewRow();
                    dr["Enable"] = true;
                    dr["HostName"] = host;
                    dtHosts.Rows.Add(dr);
                }
                dr["IP"] = ip.ToString();
                DataGridViewRow? dgvr = dgvHosts.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Col_HostName"].Value.ToString() == host).Select(r => r).FirstOrDefault();
                if (dgvr != null) dgvr.Cells["Col_IP"].Selected = true;
            }
        }

        private void LinkHostsImport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormImportHosts dialog = new();
            dialog.ShowDialog();
            string hosts = dialog.hosts;
            dialog.Dispose();
            if (string.IsNullOrEmpty(hosts)) return;
            Regex regex = new(@"^(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4})\s+(?<hostname>[^\s]+)(?<remark>.*)|^address=/(?<hostname>[^/]+)/(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4})(?<remark>.*)$");
            string[] array = hosts.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in array)
            {
                Match result = regex.Match(str.Trim());
                if (result.Success)
                {
                    string hostname = result.Groups["hostname"].Value.Trim().ToLower();
                    string remark = result.Groups["remark"].Value.Trim();
                    if (remark.StartsWith('#'))
                        remark = remark[1..].Trim();
                    if (IPAddress.TryParse(result.Groups["ip"].Value, out IPAddress? ip) && DnsListen.reHosts.IsMatch(hostname))
                    {
                        DataRow[] rows = dtHosts.Select("HostName='" + hostname + "'");
                        DataRow dr;
                        if (rows.Length >= 1)
                        {
                            dr = rows[0];
                        }
                        else
                        {
                            dr = dtHosts.NewRow();
                            dr["Enable"] = true;
                            dr["HostName"] = hostname;
                            dtHosts.Rows.Add(dr);
                        }
                        dr["IP"] = ip.ToString();
                        if (!string.IsNullOrEmpty(remark)) dr["Remark"] = remark;
                    }
                }
            }
        }

        private void LinkHostClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            for (int i = dgvHosts.Rows.Count - 2; i >= 0; i--)
            {
                dgvHosts.Rows.RemoveAt(i);
            }
        }
        #endregion

        #region ѡ�-CDN
        private void LinkCdnSpeedTest_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (ctsSpeedTest != null)
            {
                MessageBox.Show("������������У����Ժ����ԡ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl1.SelectedTab = tabSpeedTest;
                return;
            }
            List<string> lsIpTmp = new();
            foreach (string str in tbCdnAkamai.Text.Replace("��", ",").Split(','))
            {
                if (IPAddress.TryParse(str.Trim(), out IPAddress? address))
                {
                    string ip = address.ToString();
                    if (!lsIpTmp.Contains(ip))
                    {
                        lsIpTmp.Add(ip);
                    }
                }
            }
            if (lsIpTmp.Count >= 1)
            {
                string host = "Akamai";
                cbImportIP.SelectedIndex = 0;
                dgvIpList.Rows.Clear();
                flpTestUrl.Controls.Clear();
                tbDlUrl.Clear();
                dgvIpList.Tag = host;
                List<DataGridViewRow> list = new();
                gbIPList.Text = "IP �б� (" + host + ")";
                foreach (string ip in lsIpTmp)
                {
                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvIpList);
                    dgvr.Resizable = DataGridViewTriState.False;
                    dgvr.Cells[0].Value = true;
                    dgvr.Cells[1].Value = ip;
                    list.Add(dgvr);
                }
                if (list.Count >= 1)
                {
                    dgvIpList.Rows.AddRange(list.ToArray());
                    dgvIpList.ClearSelection();
                    AddTestUrl(host);
                    ButSpeedTest_Click(sender, null);
                    tabControl1.SelectedTab = tabSpeedTest;
                }
            }
            else
            {
                foreach (var item in cbImportIP.Items)
                {
                    string? str = item.ToString();
                    if (str != null && str.Contains("Akamai"))
                    {
                        cbImportIP.SelectedItem = item;
                        break;
                    }
                }
                tabControl1.SelectedTab = tabSpeedTest;
            }
        }

        private void ButCdnSave_Click(object sender, EventArgs e)
        {
            List<string> lsIpV4 = new(), lsIpV6 = new();
            foreach (string str in tbCdnAkamai.Text.Replace("��", ",").Split(','))
            {
                if (IPAddress.TryParse(str.Trim(), out IPAddress? ipAddress))
                {
                    string ip = ipAddress.ToString();
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork && !lsIpV4.Contains(ip))
                    {
                        lsIpV4.Add(ip);
                    }
                    else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 && !lsIpV6.Contains(ip))
                    {
                        lsIpV6.Add(ip);
                    }
                }
            }
            List<string> lsIp = lsIpV6.Union(lsIpV4).ToList<string>();
            tbCdnAkamai.Text = string.Join(", ", lsIp.ToArray());
            string akamaiFilePath = Path.Combine(resourceDirectory, "Akamai.txt");
            if (string.IsNullOrWhiteSpace(tbHosts2Akamai.Text))
            {
                if (File.Exists(akamaiFilePath)) File.Delete(akamaiFilePath);
            }
            else
            {
                if (!Directory.Exists(resourceDirectory)) Directory.CreateDirectory(resourceDirectory);
                File.WriteAllText(akamaiFilePath, tbHosts2Akamai.Text.Trim() + "\r\n");
            }
            Properties.Settings.Default.IpsAkamai = tbCdnAkamai.Text;
            Properties.Settings.Default.Save();
            if (bServiceFlag)
            {
                DnsListen.UpdateHosts();
                if (ckbBetterAkamaiIP.Checked) ckbBetterAkamaiIP.Checked = false;
                if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
            }
        }

        private void ButCdnReset_Click(object sender, EventArgs e)
        {
            tbCdnAkamai.Text = Properties.Settings.Default.IpsAkamai;
            string akamaiFilePath = Path.Combine(resourceDirectory, "Akamai.txt");
            if (File.Exists(akamaiFilePath))
            {
                tbHosts2Akamai.Text = File.ReadAllText(akamaiFilePath).Trim() + "\r\n";
            }
            else tbHosts2Akamai.Clear();
        }
        #endregion

        #region ѡ�-Ӳ��
        private void ButScan_Click(object sender, EventArgs e)
        {
            dgvDevice.Rows.Clear();
            butEnablePc.Enabled = butEnableXbox.Enabled = false;
            List<DataGridViewRow> list = new();

            ManagementClass mc = new("Win32_DiskDrive");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (var mo in moc)
            {
                string? sDeviceID = mo.Properties["DeviceID"].Value.ToString();
                if (string.IsNullOrEmpty(sDeviceID)) continue;
                string mbr = ClassMbr.ByteToHex(ClassMbr.ReadMBR(sDeviceID));
                if (string.Equals(mbr[..892], ClassMbr.MBR))
                {
                    string mode = mbr[1020..];
                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvDevice);
                    dgvr.Resizable = DataGridViewTriState.False;
                    dgvr.Tag = mode;
                    dgvr.Cells[0].Value = sDeviceID;
                    dgvr.Cells[1].Value = mo.Properties["Model"].Value;
                    dgvr.Cells[2].Value = mo.Properties["InterfaceType"].Value;
                    dgvr.Cells[3].Value = ClassMbr.ConvertBytes(Convert.ToUInt64(mo.Properties["Size"].Value));
                    if (mode == "99CC")
                        dgvr.Cells[4].Value = "Xbox ģʽ";
                    else if (mode == "55AA")
                        dgvr.Cells[4].Value = "PC ģʽ";
                    list.Add(dgvr);
                }
            }
            if (list.Count >= 1)
            {
                dgvDevice.Rows.AddRange(list.ToArray());
                dgvDevice.ClearSelection();
            }
        }

        private void DgvDevice_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            string? mode = dgvDevice.Rows[e.RowIndex].Tag?.ToString();
            if (mode == "99CC")
            {
                butEnablePc.Enabled = true;
                butEnableXbox.Enabled = false;
            }
            else if (mode == "55AA")
            {
                butEnablePc.Enabled = false;
                butEnableXbox.Enabled = true;
            }
        }

        private void ButEnablePc_Click(object sender, EventArgs e)
        {
            if (dgvDevice.SelectedRows.Count != 1) return;
            if (Environment.OSVersion.Version.Major < 10)
            {
                MessageBox.Show("����Win10����ϵͳת���������������������ϵͳ��", "����ϵͳ�汾����", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            string? sDeviceID = dgvDevice.SelectedRows[0].Cells["Col_DeviceID"].Value.ToString();
            if (sDeviceID == null) return;
            string? mode = dgvDevice.SelectedRows[0].Tag?.ToString();
            string mbr = ClassMbr.ByteToHex(ClassMbr.ReadMBR(sDeviceID));
            if (mode == "99CC" && mbr[..892] == ClassMbr.MBR && mbr[1020..] == mode)
            {
                string newMBR = string.Concat(mbr.AsSpan(0, 1020), "55AA");
                if (ClassMbr.WriteMBR(sDeviceID, ClassMbr.HexToByte(newMBR)))
                {
                    dgvDevice.SelectedRows[0].Tag = "55AA";
                    dgvDevice.SelectedRows[0].Cells["Col_Mode"].Value = "PC ģʽ";
                    dgvDevice.ClearSelection();
                    butEnablePc.Enabled = false;
                    using (Process p = new())
                    {
                        p.StartInfo.FileName = "diskpart.exe";
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.UseShellExecute = false;
                        p.Start();
                        p.StandardInput.WriteLine("rescan");
                        p.StandardInput.WriteLine("exit");
                        p.Close();
                    }
                    MessageBox.Show("�ɹ�ת��PCģʽ��", "ת��PCģʽ", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
        }

        private void ButEnableXbox_Click(object sender, EventArgs e)
        {
            if (dgvDevice.SelectedRows.Count != 1) return;
            string? sDeviceID = dgvDevice.SelectedRows[0].Cells["Col_DeviceID"].Value.ToString();
            if (sDeviceID == null) return;
            string? mode = dgvDevice.SelectedRows[0].Tag?.ToString();
            string mbr = ClassMbr.ByteToHex(ClassMbr.ReadMBR(sDeviceID));
            if (mode == "55AA" && mbr[..892] == ClassMbr.MBR && mbr[1020..] == mode)
            {
                string newMBR = string.Concat(mbr.AsSpan(0, 1020), "99CC");
                if (ClassMbr.WriteMBR(sDeviceID, ClassMbr.HexToByte(newMBR)))
                {
                    dgvDevice.SelectedRows[0].Tag = "99CC";
                    dgvDevice.SelectedRows[0].Cells["Col_Mode"].Value = "Xbox ģʽ";
                    dgvDevice.ClearSelection();
                    butEnableXbox.Enabled = false;
                    MessageBox.Show("�ɹ�ת��Xboxģʽ��", "ת��Xboxģʽ", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
        }

        private async void ButAnalyze_Click(object sender, EventArgs e)
        {
            string url = tbDownloadUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            if (!Regex.IsMatch(url, @"^https?://"))
            {
                if (!url.StartsWith('/')) url = "/" + url;
                url = "http://assets1.xboxlive.cn" + url;
                tbDownloadUrl.Text = url;
            }
            tbFilePath.Text = string.Empty;
            tbContentId.Text = tbProductID.Text = tbBuildID.Text = tbFileTimeCreated.Text = tbDriveSize.Text = tbPackageVersion.Text = string.Empty;
            butAnalyze.Enabled = butOpenFile.Enabled = linkCopyContentID.Enabled = linkRename.Enabled = linkProductID.Visible = false;
            Dictionary<string, string> headers = new() { { "Range", "bytes=0-4095" } };
            using HttpResponseMessage? response = await ClassWeb.HttpResponseMessageAsync(url, "GET", null, null, headers);
            if (response != null && response.IsSuccessStatusCode)
            {
                byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                XvcParse(buffer);
            }
            else
            {
                string msg = response != null ? "����ʧ�ܣ�������Ϣ��" + response.ReasonPhrase : "����ʧ��";
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            butAnalyze.Enabled = butOpenFile.Enabled = true;
        }

        private void ButOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Title = "Open an Xbox Package"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            string sFilePath = ofd.FileName;
            tbDownloadUrl.Text = "";
            tbFilePath.Text = sFilePath;
            tbContentId.Text = tbProductID.Text = tbBuildID.Text = tbFileTimeCreated.Text = tbDriveSize.Text = tbPackageVersion.Text = string.Empty;
            butAnalyze.Enabled = butOpenFile.Enabled = linkCopyContentID.Enabled = linkRename.Enabled = linkProductID.Visible = false;
            FileStream? fs = null;
            try
            {
                fs = new FileStream(sFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (fs != null)
            {
                int len = fs.Length >= 49152 ? 49152 : (int)fs.Length;
                byte[] bFileBuffer = new byte[len];
                fs.Read(bFileBuffer, 0, len);
                fs.Close();
                XvcParse(bFileBuffer);
            }
            butAnalyze.Enabled = butOpenFile.Enabled = true;
        }

        private void XvcParse(byte[] bFileBuffer)
        {
            if (bFileBuffer != null && bFileBuffer.Length >= 4096)
            {
                using MemoryStream ms = new(bFileBuffer);
                BinaryReader? br = null;
                try
                {
                    br = new BinaryReader(ms);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                if (br != null)
                {
                    br.BaseStream.Position = 0x200;
                    if (Encoding.Default.GetString(br.ReadBytes(0x8)) == "msft-xvd")
                    {
                        br.BaseStream.Position = 0x210;
                        tbFileTimeCreated.Text = DateTime.FromFileTime(BitConverter.ToInt64(br.ReadBytes(0x8), 0)).ToString();

                        br.BaseStream.Position = 0x218;
                        tbDriveSize.Text = ClassMbr.ConvertBytes(BitConverter.ToUInt64(br.ReadBytes(0x8), 0)).ToString();

                        br.BaseStream.Position = 0x220;
                        tbContentId.Text = (new Guid(br.ReadBytes(0x10))).ToString();

                        br.BaseStream.Position = 0x39C;
                        Byte[] bProductID = br.ReadBytes(0x10);
                        tbProductID.Text = (new Guid(bProductID)).ToString();
                        string productid = Encoding.Default.GetString(bProductID, 0, 7) + Encoding.Default.GetString(bProductID, 9, 5);
                        if (Regex.IsMatch(productid, @"^[a-zA-Z0-9]{12}$"))
                        {
                            linkProductID.Text = productid;
                            linkProductID.Visible = true;
                        }

                        br.BaseStream.Position = 0x3AC;
                        tbBuildID.Text = (new Guid(br.ReadBytes(0x10))).ToString();

                        br.BaseStream.Position = 0x3BC;
                        ushort PackageVersion1 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        br.BaseStream.Position = 0x3BE;
                        ushort PackageVersion2 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        br.BaseStream.Position = 0x3C0;
                        ushort PackageVersion3 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        br.BaseStream.Position = 0x3C2;
                        ushort PackageVersion4 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        tbPackageVersion.Text = PackageVersion4 + "." + PackageVersion3 + "." + PackageVersion2 + "." + PackageVersion1;
                        linkCopyContentID.Enabled = true;
                        if (!string.IsNullOrEmpty(tbFilePath.Text))
                        {
                            string filename = Path.GetFileName(tbFilePath.Text).ToLowerInvariant();
                            if (filename != tbContentId.Text.ToLowerInvariant() && !filename.EndsWith(".msixvc")) linkRename.Enabled = true;
                        }
                    }
                    else
                    {
                        MessageBox.Show("������Ч�ļ�", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    br.Close();
                }
            }
            else
            {
                MessageBox.Show("������Ч�ļ�", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkCopyContentID_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string sContentID = tbContentId.Text;
            if (!string.IsNullOrEmpty(sContentID))
            {
                Clipboard.SetDataObject(sContentID.ToUpper());
            }
        }

        private void LinkRename_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MessageBox.Show(string.Format("�Ƿ�ȷ�������������ļ���\n\n�޸�ǰ�ļ�����{0}\n�޸ĺ��ļ�����{1}", Path.GetFileName(tbFilePath.Text), tbContentId.Text.ToUpperInvariant()), "�����������ļ�", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                FileInfo fi = new(tbFilePath.Text);
                try
                {
                    fi.MoveTo(Path.GetDirectoryName(tbFilePath.Text) + "\\" + tbContentId.Text.ToUpperInvariant());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�����������ļ�ʧ�ܣ�������Ϣ��" + ex.Message, "�����������ļ�", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                linkRename.Enabled = false;
                tbContentId.Focus();
            }
        }

        private void LinkProductID_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + linkProductID.Text;
            if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            tabControl1.SelectedTab = tabStore;
        }
        #endregion

        #region ѡ�-�̵�
        private void TbGameUrl_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (butGame.Enabled)
                {
                    ButGame_Click(sender, EventArgs.Empty);
                    e.Handled = true;
                }
            }
        }

        private void ButGame_Click(object sender, EventArgs e)
        {
            string url = tbGameUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            Market market = (Market)cbGameMarket.SelectedItem;
            string language = market.language;
            string pat =
                    @"^https?://www\.xbox\.com(/[^/]*)?/games/store/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://www\.microsoft\.com(/[^/]*)?/p/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://www\.microsoft\.com/store/productId/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://apps\.microsoft\.com(/store)?/detail(/[^/]+)?/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"productid=(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^(?<productId>[a-zA-Z0-9]{12})$";
            Match result = Regex.Match(url, pat, RegexOptions.IgnoreCase);
            if (result.Success)
            {
                pbGame.Image = pbGame.InitialImage;
                tbGameTitle.Clear();
                tbGameDeveloperName.Clear();
                tbGameCategory.Clear();
                tbGameOriginalReleaseDate.Clear();
                cbGameBundled.Items.Clear();
                tbGamePrice.Clear();
                tbGameDescription.Clear();
                tbGameLanguages.Clear();
                lvGame.Items.Clear();
                butGame.Enabled = false;
                linkCompare.Enabled = false;
                pbWebsite.Enabled = pbMsStore.Enabled = pbXboxApp.Enabled = false;
                pbWebsite.Image = pbWebsite.ErrorImage;
                pbMsStore.Image = pbMsStore.ErrorImage;
                pbXboxApp.Image = pbXboxApp.ErrorImage;
                this.cbGameBundled.SelectedIndexChanged -= new EventHandler(this.CbGameBundled_SelectedIndexChanged);
                string productId = result.Groups["productId"].Value.ToUpperInvariant();
                pbWebsite.Tag = "https://www.microsoft.com/" + language + "/p/_/" + productId;
                pbMsStore.Tag = "ms-windows-store://pdp/?productid=" + productId;
                pbXboxApp.Tag = "msxbox://game/?productId=" + productId;
                ThreadPool.QueueUserWorkItem(delegate { XboxStore(market, productId); });
            }
            else
            {
                MessageBox.Show("��Ч URL/ProductId", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TbGameSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.Down || e.KeyValue == (int)Keys.Up)
            {
                lvGameSearch.Focus();
                if (lvGameSearch.Items.Count >= 1 && lvGameSearch.SelectedItems.Count == 0)
                {
                    lvGameSearch.Items[0].Selected = true;
                }
            }
        }

        private void TbGameSearch_Leave(object sender, EventArgs e)
        {
            if (lvGameSearch.Focused == false)
            {
                lvGameSearch.Visible = false;
            }
        }

        private void TbGameSearch_Enter(object sender, EventArgs e)
        {
            if (lvGameSearch.Items.Count >= 1)
            {
                lvGameSearch.Visible = true;
            }
        }

        string query = string.Empty;
        private void TbGameSearch_TextChanged(object sender, EventArgs e)
        {
            string query = tbGameSearch.Text.Trim();
            if (this.query == query) return;
            lvGameSearch.Items.Clear();
            lvGameSearch.Visible = false;
            this.query = query;
            if (string.IsNullOrEmpty(query)) return;
            ThreadPool.QueueUserWorkItem(delegate { GameSearch(query); });
        }

        private void LvGameSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.Enter)
            {
                ListViewItem item = lvGameSearch.SelectedItems[0];
                string productId = item.SubItems[1].Text;
                lvGameSearch.Visible = false;
                tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + productId;
                if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            }
        }

        private void LvGameSearch_DoubleClick(object sender, EventArgs e)
        {
            if (lvGameSearch.SelectedItems.Count >= 1)
            {
                ListViewItem item = lvGameSearch.SelectedItems[0];
                string productId = item.SubItems[1].Text;
                lvGameSearch.Visible = false;
                tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + productId;
                if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            }
        }

        private void LvGameSearch_Leave(object sender, EventArgs e)
        {
            if (tbGameSearch.Focused == false)
            {
                lvGameSearch.Visible = false;
            }
        }

        private void GameSearch(string query)
        {
            Thread.Sleep(300);
            if (this.query != query) return;
            string language = ClassWeb.language;
            if (language == "zh-CN") language = "zh-TW";
            string url = "https://www.microsoft.com/msstoreapiprod/api/autosuggest?market=" + language + "&clientId=7F27B536-CF6B-4C65-8638-A0F8CBDFCA65&sources=Microsoft-Terms,Iris-Products,xSearch-Products&filter=+ClientType:StoreWeb&counts=5,1,5&query=" + ClassWeb.UrlEncode(query);
            string html = ClassWeb.HttpResponseContent(url);
            if (this.query != query) return;
            List<ListViewItem> ls = new();
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                ClassGame.Search? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<ClassGame.Search>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.ResultSets != null && json.ResultSets.Count >= 1)
                {
                    foreach (var resultSets in json.ResultSets)
                    {
                        if (resultSets.Suggests == null) continue;
                        foreach (var suggest in resultSets.Suggests)
                        {
                            if (suggest.Metas == null) continue;
                            var BigCatalogId = Array.FindAll(suggest.Metas.ToArray(), a => a.Key == "BigCatalogId");
                            if (BigCatalogId.Length == 1)
                            {
                                string? title = suggest.Title;
                                string? productId = BigCatalogId[0].Value;
                                if (title != null && productId != null)
                                {
                                    ListViewItem item = new(new string[] { title, productId });
                                    ls.Add(item);
                                    if (imageList1.Images.ContainsKey(productId))
                                    {
                                        item.ImageKey = productId;
                                    }
                                    else if (!string.IsNullOrEmpty(suggest.ImageUrl))
                                    {
                                        string imgUrl = suggest.ImageUrl.StartsWith("//") ? "http:" + suggest.ImageUrl : suggest.ImageUrl;
                                        imgUrl = Regex.Replace(imgUrl, @"\?.+", "") + "?w=25&h=25";
                                        Task.Run(() =>
                                        {
                                            using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(imgUrl);
                                            if (response != null && response.IsSuccessStatusCode)
                                            {
                                                using Stream stream = response.Content.ReadAsStreamAsync().Result;
                                                Image img = Image.FromStream(stream);
                                                imageList1.Images.Add(productId, img);
                                                this.Invoke(new Action(() => { item.ImageKey = productId; }));
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            this.Invoke(new Action(() =>
            {
                lvGameSearch.Items.Clear();
                if (ls.Count >= 1)
                {
                    int size = (int)(25 * Form1.dpiFactor);
                    imageList1.ImageSize = new Size(size, size);
                    lvGameSearch.Height = ls.Count * (size + 2);
                    lvGameSearch.Visible = true;
                    lvGameSearch.Items.AddRange(ls.ToArray());
                }
                else
                {
                    lvGameSearch.Visible = false;
                }
            }));
        }

        private void XboxGamePass(int sort)
        {
            ComboBox cb;
            string siglId1 = string.Empty, siglId2 = string.Empty, text1 = string.Empty, text2 = string.Empty;
            if (sort == 1)
            {
                cb = cbGameXGP1;
                siglId1 = "eab7757c-ff70-45af-bfa6-79d3cfb2bf81";
                siglId2 = "a884932a-f02b-40c8-a903-a008c23b1df1";
                text1 = "���ܻ�ӭ Xbox Game Pass ������Ϸ ({0})";
                text2 = "���ܻ�ӭ Xbox Game Pass ������Ϸ ({0})";
            }
            else
            {
                cb = cbGameXGP2;
                siglId1 = "f13cf6b4-57e6-4459-89df-6aec18cf0538";
                siglId2 = "163cdff5-442e-4957-97f5-1050a3546511";
                text1 = "�������� Xbox Game Pass ������Ϸ ({0})";
                text2 = "�������� Xbox Game Pass ������Ϸ ({0})";
            }
            List<Product> lsProduct1 = new();
            List<Product> lsProduct2 = new();
            Task[] tasks = new Task[2];
            tasks[0] = new Task(() =>
            {
                lsProduct1 = GetXGPGames(siglId1, text1);
            });
            tasks[1] = new Task(() =>
            {
                lsProduct2 = GetXGPGames(siglId2, text2);
            });
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            List<Product> lsProduct = lsProduct1.Union(lsProduct2).ToList<Product>();
            if (lsProduct.Count >= 1)
            {
                this.Invoke(new Action(() =>
                {
                    cb.Items.Clear();
                    cb.Items.AddRange(lsProduct.ToArray());
                    cb.SelectedIndex = 0;
                }));
            }
        }

        private static List<Product> GetXGPGames(string siglId, string text)
        {
            List<Product> lsProduct = new();
            List<string> lsBundledId = new();
            string url = "https://catalog.gamepass.com/sigls/v2?id=" + siglId + "&language=en-US&market=US";
            string html = ClassWeb.HttpResponseContent(url);
            Match result = Regex.Match(html, @"\{""id"":""(?<ProductId>[a-zA-Z0-9]{12})""\}");
            while (result.Success)
            {
                lsBundledId.Add(result.Groups["ProductId"].Value.ToLowerInvariant());
                result = result.NextMatch();
            }
            if (lsBundledId.Count >= 1)
            {
                url = "https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds=" + string.Join(",", lsBundledId.ToArray()) + "&market=US&languages=zh-Hans,zh-Hant&MS-CV=DGU1mcuYo0WMMp+F.1";
                html = ClassWeb.HttpResponseContent(url);
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    ClassGame.Game? json = null;
                    try
                    {
                        json = JsonSerializer.Deserialize<ClassGame.Game>(html, Form1.jsOptions);
                    }
                    catch { }
                    if (json != null && json.Products != null && json.Products.Count >= 1)
                    {
                        lsProduct.Add(new Product(string.Format(text, json.Products.Count), "0"));
                        foreach (var product in json.Products)
                        {
                            string? title = product.LocalizedProperties?[0].ProductTitle;
                            string? productId = product.ProductId;
                            if (title != null && productId != null)
                                lsProduct.Add(new Product("  " + title, productId));
                        }
                    }
                }
            }
            if (lsProduct.Count == 0)
                lsProduct.Add(new Product(string.Format(text, "����ʧ��"), "0"));
            return lsProduct;
        }

        private void CbGameXGP_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not ComboBox cb || cb.SelectedIndex <= 0) return;
            Product product = (Product)cb.SelectedItem;
            if (product.id == "0") return;
            tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + product.id;
            if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
        }

        private void LinkGameChinese_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormChinese dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
            if (!string.IsNullOrEmpty(dialog.productid))
            {
                tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + dialog.productid.ToUpperInvariant();
                foreach (var item in cbGameMarket.Items)
                {
                    Market market = (Market)item;
                    if (market.language == "zh-CN")
                    {
                        cbGameMarket.SelectedItem = item;
                        break;
                    }
                }
                if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            }
        }

        private void PbOpen_Click(object sender, EventArgs e)
        {
            if (sender is not PictureBox pb) return;
            string? url = pb.Tag.ToString();
            if (string.IsNullOrEmpty(url)) return;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void CbGameBundled_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (cbGameBundled.SelectedIndex < 0) return;
            tbGameTitle.Clear();
            tbGameDeveloperName.Clear();
            tbGameCategory.Clear();
            tbGameOriginalReleaseDate.Clear();
            tbGamePrice.Clear();
            tbGameDescription.Clear();
            tbGameLanguages.Clear();
            lvGame.Items.Clear();
            linkCompare.Enabled = false;
            pbWebsite.Enabled = pbMsStore.Enabled = pbXboxApp.Enabled = false;
            pbWebsite.Image = pbWebsite.ErrorImage;
            pbMsStore.Image = pbMsStore.ErrorImage;
            pbXboxApp.Image = pbXboxApp.ErrorImage;

            var market = (Market)cbGameBundled.Tag;
            var json = (ClassGame.Game)gbGameInfo.Tag;
            int index = cbGameBundled.SelectedIndex;
            string language = market.language;
            switch (language)
            {
                case "zh-TW":
                case "zh-HK":
                    language += ",zh-Hans";
                    break;
                case "en-SG":
                case "zh-SG":
                    language = "zh-Hans," + language;
                    break;
                case "zh-CN":
                    language += ",zh-Hans";
                    break;
            }
            StoreParse(market, json, index, language);
        }

        private void XboxStore(Market market, string productId)
        {
            cbGameBundled.Tag = market;
            string language = market.language;
            switch (language)
            {
                case "zh-TW":
                case "zh-HK":
                    language += ",zh-Hans";
                    break;
                case "en-SG":
                case "zh-SG":
                    language = "zh-Hans," + language;
                    break;
                case "zh-CN":
                    language += ",zh-Hans";
                    break;
            }
            string url = "https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds=" + productId + "&market=" + market.code + "&languages=" + language + ",neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
            string html = ClassWeb.HttpResponseContent(url);
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                ClassGame.Game? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<ClassGame.Game>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.Products != null && json.Products.Count >= 1)
                {
                    StoreParse(market, json, 0, language);
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("��Ч URL/ProductId", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        butGame.Enabled = true;
                    }));
                }
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("�޷����ӷ����������Ժ����ԡ�", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    butGame.Enabled = true;
                }));
            }
        }


        internal static ConcurrentDictionary<String, Double> dicExchangeRate = new();

        private void StoreParse(Market market, ClassGame.Game json, int index, string language)
        {
            string title = string.Empty, developerName = string.Empty, publisherName = string.Empty, description = string.Empty;
            var product = json.Products[index];
            List<string> bundledId = new();
            List<ListViewItem> lsDownloadUrl = new();
            var localizedPropertie = product.LocalizedProperties;
            if (localizedPropertie != null && localizedPropertie.Count >= 1)
            {
                title = localizedPropertie[0].ProductTitle;
                developerName = localizedPropertie[0].DeveloperName;
                publisherName = localizedPropertie[0].PublisherName;
                description = localizedPropertie[0].ProductDescription;
                string? imageUri = localizedPropertie[0].Images.Where(x => x.ImagePurpose == "BoxArt").Select(x => x.Uri).FirstOrDefault() ?? localizedPropertie[0].Images.Where(x => x.Width == x.Height).OrderByDescending(x => x.Width).Select(x => x.Uri).FirstOrDefault();
                if (!string.IsNullOrEmpty(imageUri))
                {
                    if (imageUri.StartsWith("//")) imageUri = "https:" + imageUri;
                    try
                    {
                        pbGame.LoadAsync(imageUri + "?w=170&h=170");
                    }
                    catch { }
                }
            }

            string originalReleaseDate = string.Empty;
            var marketProperties = product.MarketProperties;
            if (marketProperties != null && marketProperties.Count >= 1)
            {
                originalReleaseDate = marketProperties[0].OriginalReleaseDate.ToLocalTime().ToString("d");
            }

            string category = string.Empty;
            var properties = product.Properties;
            if (properties != null)
            {
                category = properties.Category;
            }

            string gameLanguages = string.Empty;
            if (product.DisplaySkuAvailabilities != null)
            {
                foreach (var displaySkuAvailabilitie in product.DisplaySkuAvailabilities)
                {
                    if (displaySkuAvailabilitie.Sku.SkuType == "full")
                    {
                        string wuCategoryId = string.Empty;
                        if (displaySkuAvailabilitie.Sku.Properties.Packages != null)
                        {
                            foreach (var packages in displaySkuAvailabilitie.Sku.Properties.Packages)
                            {
                                List<ClassGame.PlatformDependencies> platformDependencie = packages.PlatformDependencies;
                                List<ClassGame.PackageDownloadUris> packageDownloadUris = packages.PackageDownloadUris;
                                if (platformDependencie != null && packages.PlatformDependencies.Count >= 1)
                                {
                                    wuCategoryId = packages.FulfillmentData.WuCategoryId.ToLower();
                                    string url = packageDownloadUris != null && packageDownloadUris.Count >= 1 ? packageDownloadUris[0].Uri : string.Empty;
                                    if (url == "https://productingestionbin1.blob.core.windows.net") url = string.Empty;
                                    string contentId = packages.ContentId.ToLower();
                                    switch (platformDependencie[0].PlatformName)
                                    {
                                        case "Windows.Xbox":
                                            switch (packages.PackageRank)
                                            {
                                                case 50000:
                                                    {
                                                        string key = contentId + "_x";
                                                        ListViewItem item = new(new string[] { "Xbox One", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                        {
                                                            Tag = "Game"
                                                        };
                                                        item.SubItems[0].Tag = 0;
                                                        item.SubItems[2].Tag = key;
                                                        lsDownloadUrl.Add(item);
                                                        if (string.IsNullOrEmpty(url))
                                                        {
                                                            bool find = false;
                                                            if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                                                            {
                                                                item.SubItems[3].Tag = XboxGame.Url;
                                                                item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                                                                if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                                                                    find = true;
                                                                else
                                                                {
                                                                    item.ForeColor = Color.Red;
                                                                    item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                                                                }
                                                            }
                                                            if (!find)
                                                            {
                                                                ThreadPool.QueueUserWorkItem(delegate { GetGamePackage(item, contentId, key, 0, packages); });
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case 51000:
                                                    {
                                                        string key = contentId + "_xs";
                                                        ListViewItem item = new(new string[] { "Xbox Series X|S", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                        {
                                                            Tag = "Game"
                                                        };
                                                        item.SubItems[0].Tag = 1;
                                                        item.SubItems[2].Tag = key;
                                                        lsDownloadUrl.Add(item);
                                                        if (string.IsNullOrEmpty(url))
                                                        {
                                                            bool find = false;
                                                            if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                                                            {
                                                                item.SubItems[3].Tag = XboxGame.Url;
                                                                item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                                                                if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                                                                    find = true;
                                                                else
                                                                {
                                                                    item.ForeColor = Color.Red;
                                                                    item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                                                                }
                                                            }
                                                            if (!find)
                                                            {
                                                                ThreadPool.QueueUserWorkItem(delegate { GetGamePackage(item, contentId, key, 1, packages); });
                                                            }
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    {
                                                        string version = Regex.Match(packages.PackageFullName, @"(\d+\.\d+\.\d+\.\d+)").Value;
                                                        string filename = packages.PackageFullName + "." + packages.PackageFormat;
                                                        string key = filename.Replace(version, "").ToLower();
                                                        ListViewItem? item = lsDownloadUrl.ToArray().Where(x => x.Tag.ToString() == "App" && x.SubItems[2].Tag.ToString() == key).FirstOrDefault();
                                                        if (item == null)
                                                        {
                                                            item = new ListViewItem(new string[] { "Xbox One", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                            {
                                                                Tag = "App"
                                                            };
                                                            item.SubItems[0].Tag = 0;
                                                            item.SubItems[1].Tag = version;
                                                            item.SubItems[2].Tag = key;
                                                            item.SubItems[3].Tag = filename;
                                                            lsDownloadUrl.Add(item);
                                                        }
                                                        else
                                                        {
                                                            string? tag = item.SubItems[1].Tag.ToString();
                                                            if (tag != null && new Version(version) > new Version(tag))
                                                            {
                                                                item.SubItems[2].Text = ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes);
                                                                item.SubItems[1].Tag = version;
                                                                item.SubItems[3].Tag = filename;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                            break;
                                        case "Windows.Desktop":
                                        case "Windows.Universal":
                                            switch (packages.PackageFormat.ToLower())
                                            {
                                                case "msixvc":
                                                    {
                                                        string key = contentId;
                                                        ListViewItem item = new(new string[] { "Windows PC", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                        {
                                                            Tag = "Game"
                                                        };
                                                        item.SubItems[0].Tag = 2;
                                                        item.SubItems[1].Tag = product.ProductId;
                                                        item.SubItems[2].Tag = key;
                                                        lsDownloadUrl.Add(item);
                                                        if (string.IsNullOrEmpty(url))
                                                        {
                                                            bool find = false;
                                                            if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                                                            {
                                                                item.SubItems[3].Tag = XboxGame.Url;
                                                                item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                                                                if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                                                                    find = true;
                                                                else
                                                                {
                                                                    item.ForeColor = Color.Red;
                                                                    item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                                                                }
                                                            }
                                                            if (!find)
                                                            {
                                                                ThreadPool.QueueUserWorkItem(delegate { GetGamePackage(item, contentId, key, 2, packages); });
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case "appx":
                                                case "appxbundle":
                                                case "eappx":
                                                case "eappxbundle":
                                                case "msix":
                                                case "msixbundle":
                                                    {
                                                        string version = Regex.Match(packages.PackageFullName, @"(\d+\.\d+\.\d+\.\d+)").Value;
                                                        string filename = packages.PackageFullName + "." + packages.PackageFormat;
                                                        string key = filename.Replace(version, "").ToLower();
                                                        ListViewItem? item = lsDownloadUrl.ToArray().Where(x => x.Tag.ToString() == "App" && x.SubItems[2].Tag.ToString() == key).FirstOrDefault();
                                                        if (item == null)
                                                        {
                                                            item = new ListViewItem(new string[] { "Windows PC", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                            {
                                                                Tag = "App"
                                                            };
                                                            item.SubItems[0].Tag = 2;
                                                            item.SubItems[1].Tag = version;
                                                            item.SubItems[2].Tag = key;
                                                            item.SubItems[3].Tag = filename;
                                                            lsDownloadUrl.Add(item);
                                                        }
                                                        else
                                                        {
                                                            string? tag = item.SubItems[1].Tag.ToString();
                                                            if (tag != null && new Version(version) > new Version(tag))
                                                            {
                                                                item.SubItems[2].Text = ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes);
                                                                item.SubItems[1].Tag = version;
                                                                item.SubItems[3].Tag = filename;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                            break;
                                    }
                                    if (packages.Languages != null) gameLanguages = string.Join(", ", packages.Languages);
                                }
                            }
                            List<ListViewItem> lsAppItems = lsDownloadUrl.ToArray().Where(x => x.Tag.ToString() == "App").ToList();
                            if (lsAppItems.Count >= 1)
                            {
                                lvGame.Tag = wuCategoryId;
                                bool find = true;
                                foreach (var item in lsAppItems)
                                {
                                    string? filename = item.SubItems[3].Tag.ToString();
                                    if (filename != null && dicAppPackage.TryGetValue(filename.ToLower(), out AppPackage? appPackage) && (DateTime.Now - appPackage.Date).TotalSeconds <= 300)
                                    {
                                        string expire = string.Empty;
                                        Match result = Regex.Match(appPackage.Url, @"P1=(\d+)");
                                        if (result.Success) expire = " (Expire: " + DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime() + ")";
                                        item.SubItems[3].Text = filename + expire;
                                    }
                                    else
                                    {
                                        item.SubItems[3].Text = "���ڻ�ȡ�������ӣ����Ժ�...";
                                        find = false;
                                    }
                                }
                                if (!find) ThreadPool.QueueUserWorkItem(delegate { GetAppPackage(wuCategoryId, lsAppItems); });
                            }
                        }
                        if (displaySkuAvailabilitie.Sku.Properties.BundledSkus != null && displaySkuAvailabilitie.Sku.Properties.BundledSkus.Count >= 1)
                        {
                            foreach (var BundledSkus in displaySkuAvailabilitie.Sku.Properties.BundledSkus)
                            {
                                bundledId.Add(BundledSkus.BigId);
                            }
                        }
                        break;
                    }
                }
            }

            List<Product> lsProduct = new();
            if (bundledId.Count >= 1 && json.Products.Count == 1)
            {
                string url = "https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds=" + string.Join(",", bundledId.ToArray()) + "&market=" + market.code + "&languages=" + language + ",neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
                string html = ClassWeb.HttpResponseContent(url);
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    ClassGame.Game? json2 = null;
                    try
                    {
                        json2 = JsonSerializer.Deserialize<ClassGame.Game>(html, Form1.jsOptions);
                    }
                    catch { }
                    if (json2 != null && json2.Products != null && json2.Products.Count >= 1)
                    {
                        json.Products.AddRange(json2.Products);
                        lsProduct.Add(new Product("�����������(" + json2.Products.Count + ")", ""));
                        foreach (var product2 in json2.Products)
                        {
                            lsProduct.Add(new Product(product2.LocalizedProperties[0].ProductTitle, product2.ProductId));
                        }
                    }
                }
            }

            if (index == 0) gbGameInfo.Tag = json;
            var DisplaySkuAvailabilities = product.DisplaySkuAvailabilities;
            if (DisplaySkuAvailabilities != null)
            {
                string CurrencyCode = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.CurrencyCode.ToUpperInvariant();
                double MSRP = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.MSRP;
                double ListPrice_1 = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.ListPrice;
                double ListPrice_2 = DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.ListPrice : 0;
                double WholesalePrice_1 = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.WholesalePrice;
                double WholesalePrice_2 = DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.WholesalePrice : 0;
                if (ListPrice_1 > MSRP) MSRP = ListPrice_1;
                if (!string.IsNullOrEmpty(CurrencyCode) && MSRP > 0 && CurrencyCode != "CNY" && !dicExchangeRate.ContainsKey(CurrencyCode))
                {
                    ClassGame.ExchangeRate(CurrencyCode);
                }
                double ExchangeRate = dicExchangeRate.ContainsKey(CurrencyCode) ? dicExchangeRate[CurrencyCode] : 0;

                StringBuilder sbPrice = new();
                if (MSRP > 0)
                {
                    sbPrice.Append(string.Format("����: {0}, �������ۼ�: {1}", CurrencyCode, String.Format("{0:N}", MSRP)));
                    if (ExchangeRate > 0)
                    {
                        sbPrice.Append(string.Format("({0})", String.Format("{0:N}", MSRP * ExchangeRate)));
                    }
                    if (ListPrice_1 > 0 && ListPrice_1 != MSRP)
                    {
                        sbPrice.Append(string.Format(", ��ͨ�ۿ�{0}%: {1}", Math.Round(ListPrice_1 / MSRP * 100, 0, MidpointRounding.AwayFromZero), String.Format("{0:N}", ListPrice_1)));
                        if (ExchangeRate > 0)
                        {
                            sbPrice.Append(string.Format("({0})", String.Format("{0:N}", ListPrice_1 * ExchangeRate)));
                        }
                    }
                    if (ListPrice_2 > 0 && ListPrice_2 < ListPrice_1 && ListPrice_2 != MSRP)
                    {
                        string member = (DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags.Length >= 1 && DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags[0] == "LegacyDiscountEAAccess") ? "EA Play" : "��Ա";
                        sbPrice.Append(string.Format(", {0}�ۿ�{1}%: {2}", member, Math.Round(ListPrice_2 / MSRP * 100, 0, MidpointRounding.AwayFromZero), String.Format("{0:N}", ListPrice_2)));
                        if (ExchangeRate > 0)
                        {
                            sbPrice.Append(string.Format("({0})", String.Format("{0:N}", ListPrice_2 * ExchangeRate)));
                        }
                    }
                    if (WholesalePrice_1 > 0)
                    {
                        sbPrice.Append(string.Format(", ������: {0}", String.Format("{0:N}", WholesalePrice_1)));
                        if (ExchangeRate > 0)
                        {
                            sbPrice.Append(string.Format("({0})", String.Format("{0:N}", WholesalePrice_1 * ExchangeRate)));
                        }
                        if (WholesalePrice_2 > 0 && WholesalePrice_2 < WholesalePrice_1)
                        {
                            sbPrice.Append(string.Format(", �������ۿ�{0}%: {1}", Math.Round(WholesalePrice_2 / WholesalePrice_1 * 100, 0, MidpointRounding.AwayFromZero), String.Format("{0:N}", WholesalePrice_2)));
                            if (ExchangeRate > 0)
                            {
                                sbPrice.Append(string.Format("({0})", String.Format("{0:N}", WholesalePrice_2 * ExchangeRate)));
                            }
                        }
                    }
                    if (ExchangeRate > 0)
                    {
                        sbPrice.Append(string.Format(", CNY����: {0}", ExchangeRate));
                    }
                }

                this.Invoke(new Action(() =>
                {
                    tbGameTitle.Text = title;
                    tbGameDeveloperName.Text = publisherName.Trim() + " / " + developerName.Trim();
                    tbGameCategory.Text = category;
                    tbGameOriginalReleaseDate.Text = originalReleaseDate;
                    if (lsProduct.Count >= 1)
                    {
                        cbGameBundled.Items.AddRange(lsProduct.ToArray());
                        cbGameBundled.SelectedIndex = 0;
                        this.cbGameBundled.SelectedIndexChanged += new EventHandler(this.CbGameBundled_SelectedIndexChanged);
                    }
                    tbGameDescription.Text = Regex.Replace(description, @"\r\n|\n|\r", Environment.NewLine);
                    tbGameLanguages.Text = gameLanguages;
                    if (MSRP > 0)
                    {
                        tbGamePrice.Text = sbPrice.ToString();
                        linkCompare.Enabled = true;
                    }
                    if (lsDownloadUrl.Count >= 1)
                    {
                        lsDownloadUrl.Sort((x, y) => string.Compare(x.SubItems[0].Tag.ToString(), y.SubItems[0].Tag.ToString()));
                        lvGame.Items.AddRange(lsDownloadUrl.ToArray());
                    }
                    butGame.Enabled = true;
                    pbWebsite.Enabled = pbMsStore.Enabled = pbXboxApp.Enabled = true;
                    pbWebsite.Image = pbWebsite.InitialImage;
                    pbMsStore.Image = pbMsStore.InitialImage;
                    pbXboxApp.Image = pbXboxApp.InitialImage;
                }));
            }
        }

        readonly ConcurrentDictionary<string, DateTime> dicGetGamePackage = new();

        private void GetGamePackage(ListViewItem item, string contentId, string key, int platform, ClassGame.Packages packages)
        {
            XboxPackage.Game? json = null;
            if (!dicGetGamePackage.ContainsKey(key) || DateTime.Compare(dicGetGamePackage[key], DateTime.Now) < 0)
            {
                string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetGamePackage?contentId=" + contentId + "&platform=" + platform, "GET", null, null, null, 30000, "XboxDownload");
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    try
                    {
                        json = JsonSerializer.Deserialize<XboxPackage.Game>(html, Form1.jsOptions);
                    }
                    catch { }
                }
            }
            bool succeed = false;
            if (json != null && json.Code == "200")
            {
                DateTime limit = DateTime.Now.AddMinutes(3);
                dicGetGamePackage.AddOrUpdate(key, limit, (oldkey, oldvalue) => limit);
                if (json.Data != null)
                {
                    Version version = new(Regex.Match(json.Data.Url, @"(\d+\.\d+\.\d+\.\d+)").Value);
                    bool update = false;
                    if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                    {
                        if (version > XboxGame.Version)
                        {
                            XboxGame.Version = version;
                            XboxGame.FileSize = json.Data.Size;
                            XboxGame.Url = json.Data.Url;
                            update = true;
                        }
                    }
                    else
                    {
                        XboxGame = new XboxGameDownload.Products
                        {
                            Version = version,
                            FileSize = json.Data.Size,
                            Url = json.Data.Url
                        };
                        XboxGameDownload.dicXboxGame.TryAdd(key, XboxGame);
                        update = true;
                    }
                    if (update) XboxGameDownload.SaveXboxGame();
                    this.Invoke(new Action(() =>
                    {
                        if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                        {
                            succeed = true;
                            item.ForeColor = Color.Empty;
                        }
                        else
                        {
                            item.ForeColor = Color.Red;
                        }
                        item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                        item.SubItems[3].Tag = XboxGame.Url;
                        item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                    }));
                }
            }
            if (!succeed && (platform == 0 || platform == 2))
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Authorization))
                {
                    string hosts = "packagespc.xboxlive.com", url = String.Empty;
                    ulong filesize = 0;
                    string? ip = ClassDNS.DoH(hosts);
                    if (string.IsNullOrEmpty(ip)) return;
                    using (HttpResponseMessage? response = ClassWeb.HttpResponseMessage("https://" + ip + "/GetBasePackage/" + contentId, "GET", null, null, new() { { "Host", hosts }, { "Authorization", Properties.Settings.Default.Authorization } }))
                    {
                        if (response != null)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string html = response.Content.ReadAsStringAsync().Result;
                                if (Regex.IsMatch(html, @"^{.+}$"))
                                {
                                    XboxGameDownload.PackageFiles? packageFiles = null;
                                    try
                                    {
                                        var json2 = JsonSerializer.Deserialize<XboxGameDownload.Game>(html, Form1.jsOptions);
                                        if (json2 != null && json2.PackageFound)
                                        {
                                            packageFiles = json2.PackageFiles.Where(x => Regex.IsMatch(x.RelativeUrl, @"(\.msixvc|\.xvc)$", RegexOptions.IgnoreCase)).FirstOrDefault() ?? json2.PackageFiles.Where(x => !Regex.IsMatch(x.RelativeUrl, @"(\.xsp|\.phf)$", RegexOptions.IgnoreCase)).FirstOrDefault();
                                        }
                                    }
                                    catch { }
                                    if (packageFiles != null)
                                    {
                                        Match result = Regex.Match(packageFiles.RelativeUrl, @"(?<version>\d+\.\d+\.\d+\.\d+)\.\w{8}-\w{4}-\w{4}-\w{4}-\w{12}");
                                        if (result.Success)
                                        {
                                            url = packageFiles.CdnRootPaths[0].Replace(".xboxlive.cn", ".xboxlive.com") + packageFiles.RelativeUrl;
                                            Version version = new(result.Groups["version"].Value);
                                            XboxGameDownload.Products XboxGame = new()
                                            {
                                                Version = version,
                                                FileSize = packageFiles.FileSize,
                                                Url = url
                                            };
                                            filesize = packageFiles.FileSize;
                                            XboxGameDownload.dicXboxGame.AddOrUpdate(key, XboxGame, (oldkey, oldvalue) => XboxGame);
                                            packages.MaxDownloadSizeInBytes = filesize;
                                            packages.PackageDownloadUris[0].Uri = url;
                                            XboxGameDownload.SaveXboxGame();
                                        }
                                    }
                                }
                            }
                            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Properties.Settings.Default.Authorization = null;
                                Properties.Settings.Default.Save();
                            }
                        }
                    }
                    this.Invoke(new Action(() =>
                    {
                        if (filesize > 0)
                        {
                            item.ForeColor = Color.Empty;
                            item.SubItems[2].Text = ClassMbr.ConvertBytes(filesize);
                        }
                        if (!string.IsNullOrEmpty(url))
                        {
                            item.SubItems[3].Tag = url;
                            item.SubItems[3].Text = Path.GetFileName(url);
                        }
                    }));
                    if (Regex.IsMatch(url, @"^https?://")) _ = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/AddGameUrl?url=" + ClassWeb.UrlEncode(url), "PUT", null, null, null, 30000, "XboxDownload");
                }
            }
        }

        readonly ConcurrentDictionary<string, AppPackage> dicAppPackage = new();
        class AppPackage
        {
            public string Url { get; set; } = "";
            public DateTime Date { get; set; }
        }

        private void GetAppPackage(string wuCategoryId, List<ListViewItem> lsAppItems)
        {
            string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload");
            XboxPackage.App? json = null;
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                }
                catch { }
            }
            if (json != null && json.Code != null && json.Code == "200" && json.Data != null)
            {
                foreach (var item in json.Data)
                {
                    if (!string.IsNullOrEmpty(item.Url))
                    {
                        AppPackage appPackage = new()
                        {
                            Url = item.Url,
                            Date = DateTime.Now
                        };
                        dicAppPackage.AddOrUpdate(item.Name.ToLower(), appPackage, (oldkey, oldvalue) => appPackage);
                    }
                }
            }
            this.Invoke(new Action(() =>
            {
                foreach (var item in lsAppItems)
                {
                    string filename = String.Empty;
                    string? tag = item.SubItems[3].Tag?.ToString();
                    if (tag != null && dicAppPackage.TryGetValue(tag.ToLower(), out _))
                    {
                        filename = tag;
                    }
                    else if (json != null && json.Code != null && json.Code == "200" && json.Data != null)
                    {
                        var key = item.SubItems[2].Tag.ToString()?.ToLower();
                        if (key != null)
                        {
                            var data = json.Data.Where(x => Regex.Replace(x.Name, @"\d+\.\d+\.\d+\.\d+", "").ToLower() == key).FirstOrDefault();
                            if (data != null)
                            {
                                item.SubItems[3].Tag = data.Name;
                                item.SubItems[2].Text = ClassMbr.ConvertBytes(data.Size);
                                filename = data.Name;
                            }
                        }
                    }
                    string expire = string.Empty;
                    if (dicAppPackage.TryGetValue(filename.ToLower(), out AppPackage? appPackage))
                    {
                        Match result = Regex.Match(appPackage.Url, @"P1=(\d+)");
                        if (result.Success) expire = " (Expire: " + DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime() + ")";
                    }
                    item.SubItems[3].Text = filename + expire;
                }
            }));
        }

        private void LinkCompare_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int index = cbGameBundled.SelectedIndex == -1 ? 0 : cbGameBundled.SelectedIndex;
            FormCompare dialog = new(gbGameInfo.Tag, index);
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void Link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is not LinkLabel link) return;
            string? url = link.Tag.ToString();
            if (string.IsNullOrEmpty(url)) return;
            switch (link.Name)
            {
                case "linkWebPage":
                    if (gbGameInfo.Tag != null)
                    {
                        url += "?" + ((ClassGame.Game)gbGameInfo.Tag).Products[0].ProductId;
                    }
                    break;
            }
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void LinkAppxAdd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tabControl1.SelectedTab = tabTools;
            tbAppxFilePath.Focus();
        }

        private void LvGame_MouseClick(object sender, MouseEventArgs e)
        {
            if (lvGame.SelectedItems.Count == 1)
            {
                ListViewItem item = lvGame.SelectedItems[0];
                string text = item.SubItems[3].Text;
                if (e.Button == MouseButtons.Left)
                {
                    if (Regex.IsMatch(text, @"[\u4e00-\u9fa5]")) return;
                    if (item.Tag.ToString() == "Game")
                    {
                        if (Regex.IsMatch(item.SubItems[3].Text, @"^https?://"))
                            item.SubItems[3].Text = Path.GetFileName(item.SubItems[3].Text);

                        else
                            item.SubItems[3].Text = item.SubItems[3].Tag.ToString();
                    }
                    else
                    {
                        if (Regex.IsMatch(item.SubItems[3].Text, @"^https?://"))
                        {
                            string expire = string.Empty;
                            if (!string.IsNullOrEmpty(item.SubItems[1].Text) && dicAppPackage.TryGetValue((item.SubItems[3].Tag.ToString() ?? string.Empty).ToLower(), out AppPackage? appPackage))
                            {
                                Match result = Regex.Match(appPackage.Url, @"P1=(\d+)");
                                if (result.Success) expire = " (Expire: " + DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime() + ")";
                            }
                            item.SubItems[3].Text = item.SubItems[3].Tag.ToString() + expire;
                        }
                        else if (dicAppPackage.TryGetValue((item.SubItems[3].Tag.ToString() ?? string.Empty).ToLower(), out AppPackage? appPackage))
                        {
                            item.SubItems[3].Text = appPackage.Url;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(text) && !Regex.IsMatch(text, @"���Ժ�"))
                {
                    if (!Regex.IsMatch(text, @"��Ȩ"))
                    {
                        bool isGame = item.Tag.ToString() == "Game";
                        tsmCopyUrl1.Visible = true;
                        tsmCopyUrl2.Visible = tsmCopyUrl3.Visible = isGame;
                        tsmCopyUrl3.Enabled = isGame && item.SubItems[3].Tag != null && Regex.IsMatch(item.SubItems[3].Tag.ToString() ?? string.Empty, @"http://[^\.]+\.xboxlive\.com/(\d{1,2}|Z)/");
                        tsmAllUrl.Visible = !isGame && lvGame.Tag != null && item.SubItems[0].Text == "Windows PC";
                    }
                    else
                    {
                        tsmCopyUrl1.Visible = tsmCopyUrl2.Visible = tsmCopyUrl3.Visible = tsmAllUrl.Visible = false;
                    }
                    cmsCopyUrl.Show(MousePosition.X, MousePosition.Y);
                }
            }
        }

        private void TsmCopyUrl_Click(object sender, EventArgs e)
        {
            string url = string.Empty;
            ListViewItem item = lvGame.SelectedItems[0];
            if (item.Tag.ToString() == "Game")
            {
                url = item.SubItems[3].Tag.ToString() ?? string.Empty;
            }
            else
            {
                if (dicAppPackage.TryGetValue((item.SubItems[3].Tag.ToString() ?? string.Empty).ToLower(), out AppPackage? appPackage))
                {
                    url = appPackage.Url;
                }
            }
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            if (tsmi.Name == "tsmCopyUrl2")
            {
                string hosts = Regex.Match(url, @"(?<=://)[a-zA-Z\.0-9]+(?=\/)").Value;
                url = hosts switch
                {
                    "xvcf1.xboxlive.com" => url.Replace("xvcf1.xboxlive.com", "assets1.xboxlive.cn"),
                    "xvcf2.xboxlive.com" => url.Replace("xvcf2.xboxlive.com", "assets2.xboxlive.cn"),
                    _ => url.Replace(".xboxlive.com", ".xboxlive.cn"),
                };
            }
            else if (tsmi.Name == "tsmCopyUrl3")
            {
                Match result = Regex.Match(url, @"http://[^\.]+\.xboxlive\.com/(\d{1,2}|Z)/(.+)");
                if (result.Success) url = "http://xbasset" + result.Groups[1].Value.Replace("Z", "0") + ".blob.core.windows.net/" + result.Groups[2].Value;
            }
            Clipboard.SetDataObject(url);
            if (lvGame.SelectedItems[0].ForeColor == Color.Red)
            {
                MessageBox.Show("�����µİ汾�������������ѹ�ʱ��", "��ʾ��Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void TsmAllUrl_Click(object sender, EventArgs e)
        {
            string? wuCategoryId = lvGame.Tag.ToString();
            if (string.IsNullOrEmpty(wuCategoryId)) return;
            lvGame.Tag = null;
            ListViewItem last = new(new string[] { "", "", "", "���Ժ�..." });
            lvGame.Items.Add(last);
            List<String> filter = new();
            foreach (ListViewItem item in lvGame.Items)
            {
                string? filename = item.SubItems[3].Tag?.ToString();
                if (string.IsNullOrEmpty(filename)) continue;
                filter.Add(filename.ToLower());
            }
            Task.Factory.StartNew(() =>
            {
                string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage2?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload");
                XboxPackage.App? json = null;
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    try
                    {
                        json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                    }
                    catch { }
                }
                List<ListViewItem> list = new();
                if (json != null && json.Code != null && json.Code == "200" && json.Data != null)
                {
                    json.Data.Sort((x, y) => string.Compare(x.Name, y.Name));
                    foreach (var item in json.Data)
                    {
                        if (!string.IsNullOrEmpty(item.Url))
                        {
                            AppPackage appPackage = new()
                            {
                                Url = item.Url,
                                Date = DateTime.Now
                            };
                            dicAppPackage.AddOrUpdate(item.Name.ToLower(), appPackage, (oldkey, oldvalue) => appPackage);
                        }
                        if (!filter.Contains(item.Name.ToLower()))
                        {
                            ListViewItem lvi = new(new string[] { "", "", ClassMbr.ConvertBytes(item.Size), item.Name })
                            {
                                Tag = "App"
                            };
                            lvi.SubItems[3].Tag = item.Name;
                            list.Add(lvi);
                        }
                    }
                }
                this.Invoke(new Action(() =>
                {
                    if (last.Index != -1)
                    {
                        lvGame.Items.RemoveAt(lvGame.Items.Count - 1);
                        if (list.Count > 0) lvGame.Items.AddRange(list.ToArray());
                    }
                }));
            });
        }
        #endregion

        #region ѡ�-����
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x219)
            {
                switch (m.WParam.ToInt32())
                {
                    case 0x8000: //U�̲���
                    case 0x8004: //U��ж��
                        LinkRefreshDrive_LinkClicked(null, null);
                        break;
                    default:
                        break;
                }
            }
            base.WndProc(ref m);
        }

        private void CbDrive_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (cbDrive.Items.Count >= 1)
            {
                string driverName = cbDrive.Text;
                DriveInfo driveInfo = new(driverName);
                if (driveInfo.DriveType == DriveType.Removable)
                {
                    if (driveInfo.IsReady && driveInfo.DriveFormat == "NTFS")
                    {
                        List<string> listStatus = new();
                        if (File.Exists(driverName + "$ConsoleGen8Lock"))
                            listStatus.Add(rbXboxOne.Text + " �ع�");
                        else if (File.Exists(driverName + "$ConsoleGen8"))
                            listStatus.Add(rbXboxOne.Text + " ����");
                        if (File.Exists(driverName + "$ConsoleGen9Lock"))
                            listStatus.Add(rbXboxSeries.Text + " �ع�");
                        else if (File.Exists(driverName + "$ConsoleGen9"))
                            listStatus.Add(rbXboxSeries.Text + " ����");
                        if (listStatus.Count >= 1)
                            labelStatusDrive.Text = "��ǰU��״̬��" + string.Join(", ", listStatus.ToArray());
                        else
                            labelStatusDrive.Text = "��ǰU��״̬��δת��";
                    }
                    else
                    {
                        labelStatusDrive.Text = "��ǰU��״̬������NTFS��ʽ";
                    }
                }
            }
        }

        private void LinkRefreshDrive_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs? e)
        {
            cbDrive.Items.Clear();
            DriveInfo[] driverList = Array.FindAll(DriveInfo.GetDrives(), a => a.DriveType == DriveType.Removable);
            if (driverList.Length >= 1)
            {
                cbDrive.Items.AddRange(driverList);
                cbDrive.SelectedIndex = 0;
            }
            else
            {
                labelStatusDrive.Text = "��ǰU��״̬��û���ҵ�U��";
            }
        }

        private void LinkUsbDevice_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormUsbDevice dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
            LinkRefreshDrive_LinkClicked(sender, e);
        }

        private void ButConsoleRegionUnlock_Click(object sender, EventArgs e)
        {
            ConsoleRegion(true);
        }

        private void ButConsoleRegionLock_Click(object sender, EventArgs e)
        {
            ConsoleRegion(false);
        }

        private void ConsoleRegion(bool unlock)
        {
            if (cbDrive.Items.Count == 0)
            {
                MessageBox.Show("�����U�̡�", "û���ҵ�U��", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            labelStatusDrive.Text = "��ǰU��״̬�������У����Ժ�...";
            string driverName = cbDrive.Text;
            DriveInfo driveInfo = new(driverName);
            if (driveInfo.DriveType == DriveType.Removable)
            {
                if (!driveInfo.IsReady || driveInfo.DriveFormat != "NTFS")
                {
                    string show, caption, cmd;
                    if (driveInfo.IsReady && driveInfo.DriveFormat == "FAT32")
                    {
                        show = "��ǰU�̸�ʽ " + driveInfo.DriveFormat + "���Ƿ��U��ת��Ϊ NTFS ��ʽ��\n\nע�⣬���U������Ҫ�������ȱ���!\n\n��ǰU��λ�ã� " + driverName + "��������" + ClassMbr.ConvertBytes(Convert.ToUInt64(driveInfo.TotalSize)) + "\nȡ��ת���밴\"��(N)\"";
                        caption = "ת��U�̸�ʽ";
                        cmd = "convert " + Regex.Replace(driverName, @"\\$", "") + " /fs:ntfs /x";
                    }
                    else
                    {
                        show = "��ǰU�̸�ʽ " + (driveInfo.IsReady ? driveInfo.DriveFormat : "RAW") + "���Ƿ��U�̸�ʽ��Ϊ NTFS��\n\n���棬��ʽ����ɾ��U���е������ļ�!\n���棬��ʽ����ɾ��U���е������ļ�!\n���棬��ʽ����ɾ��U���е������ļ�!\n\n��ǰU��λ�ã� " + driverName + "��������" + (driveInfo.IsReady ? ClassMbr.ConvertBytes(Convert.ToUInt64(driveInfo.TotalSize)) : "δ֪") + "\nȡ����ʽ���밴\"��(N)\"";
                        caption = "��ʽ��U��";
                        cmd = "format " + Regex.Replace(driverName, @"\\$", "") + " /fs:ntfs /q";
                    }
                    if (MessageBox.Show(show, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        string outputString;
                        using Process p = new();
                        p.StartInfo.FileName = "cmd.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();

                        p.StandardInput.WriteLine(cmd);
                        p.StandardInput.WriteLine("exit");

                        p.StandardInput.Close();
                        outputString = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();
                    }
                }
                if (driveInfo.IsReady && driveInfo.DriveFormat == "NTFS")
                {
                    string[] files = { "$ConsoleGen8", "$ConsoleGen8Lock", "$ConsoleGen9", "$ConsoleGen9Lock" };
                    foreach (string file in files)
                    {
                        if (File.Exists(driverName + "\\" + file))
                        {
                            File.Delete(driverName + "\\" + file);
                        }
                    }
                    if (rbXboxOne.Checked)
                    {
                        using (File.Create(driverName + (unlock ? "$ConsoleGen8" : "$ConsoleGen8Lock"))) { }
                    }
                    else if (rbXboxSeries.Checked)
                    {
                        using (File.Create(driverName + (unlock ? "$ConsoleGen9" : "$ConsoleGen9Lock"))) { }
                    }
                    if (Regex.IsMatch(driveInfo.VolumeLabel, @"[^\x00-\xFF]")) //��꺬�з�Ӣ���ַ�
                    {
                        driveInfo.VolumeLabel = "";
                    }
                }
                else
                {
                    MessageBox.Show("U�̲���NTFS��ʽ�������¸�ʽ��NTFS��ʽ����ת����", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                CbDrive_SelectedIndexChanged(null, null);
            }
            else
            {
                labelStatusDrive.Text = "��ǰU��״̬��" + driverName + " �豸������";
            }
        }

        private void CbAppxDrive_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (cbAppxDrive.Tag == null) return;
            DataTable dt = (DataTable)cbAppxDrive.Tag;
            string drive = cbAppxDrive.Text, gamesPath;
            string? storePath;
            bool error = false;
            DataRow? dr = dt.Rows.Find(drive);
            if (dr != null)
            {
                storePath = dr["StorePath"].ToString();
                if (Convert.ToBoolean(dr["IsOffline"]))
                {
                    error = true;
                    storePath += " (����)";
                }
            }
            else
            {
                error = true;
                storePath = "(δ֪����)";
            }
            if (File.Exists(drive + "\\.GamingRoot"))
            {
                try
                {
                    using FileStream fs = new(drive + "\\.GamingRoot", FileMode.Open, FileAccess.Read, FileShare.Read);
                    using BinaryReader br = new(fs);
                    if (ClassMbr.ByteToHex(br.ReadBytes(0x8)) == "5247425801000000")
                    {
                        gamesPath = drive + Encoding.GetEncoding("UTF-16").GetString(br.ReadBytes((int)fs.Length - 0x8)).Trim('\0');
                        if (!Directory.Exists(gamesPath))
                        {
                            error = true;
                            gamesPath += " (�ļ��в�����)";
                        }
                    }
                    else
                    {
                        error = true;
                        gamesPath = drive + " (�ļ���δ֪)";
                    }
                }
                catch (Exception ex)
                {
                    error = true;
                    gamesPath = drive + " (" + ex.Message + ")";
                }
            }
            else
            {
                error = true;
                gamesPath = drive + " (�ļ���δ֪)";
            }
            linkFixAppxDrive.Visible = error;
            labelInstallationLocation.ForeColor = error ? Color.Red : Color.Green;
            labelInstallationLocation.Text = $"Ӧ�ð�װĿ¼��{storePath}\r\n��Ϸ��װĿ¼��{gamesPath}";
        }

        private void LinkAppxRefreshDrive_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs? e)
        {
            cbAppxDrive.Items.Clear();
            cbAppxDrive.Tag = null;
            DriveInfo[] driverList = Array.FindAll(DriveInfo.GetDrives(), a => a.DriveType == DriveType.Fixed && a.IsReady && a.DriveFormat == "NTFS");
            if (driverList.Length >= 1)
            {
                cbAppxDrive.Items.AddRange(driverList);
                cbAppxDrive.SelectedIndex = 0;
            }
            ThreadPool.QueueUserWorkItem(delegate { GetAppxVolume(); });
        }

        private void LinkFixAppxDrive_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
            if (service == null || service.Status != ServiceControllerStatus.Running)
            {
                MessageBox.Show("û�м�⵽��Ϸ����(Gaming Services)������������Ϸ������ִ�д˲�����", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            string drive = cbAppxDrive.Text, path;
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (drive == Directory.GetDirectoryRoot(dir))
                path = dir + "\\WindowsApps";
            else
                path = drive + "WindowsApps";
            try
            {
                using Process p = new();
                p.StartInfo.FileName = @"powershell.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("Add-AppxVolume -Path \"" + path + "\"");
                p.StandardInput.WriteLine("Mount-AppxVolume -Volume \"" + path + "\"");
                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("����PowerShellʧ�ܣ�������Ϣ��" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            bool fixGamingRoot = false;
            if (!File.Exists(drive + "\\.GamingRoot"))
            {
                fixGamingRoot = true;
            }
            else
            {
                using FileStream fs = new(drive + "\\.GamingRoot", FileMode.Open, FileAccess.Read, FileShare.Read);
                using BinaryReader br = new(fs);
                if (ClassMbr.ByteToHex(br.ReadBytes(0x8)) == "5247425801000000")
                {
                    string gamesPath = drive + Encoding.GetEncoding("UTF-16").GetString(br.ReadBytes((int)fs.Length - 0x8)).Trim('\0');
                    if (!Directory.Exists(gamesPath))
                    {
                        fixGamingRoot = true;
                    }
                }
            }
            if (fixGamingRoot)
            {
                if (service != null)
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(10000);
                    try
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        }
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        }
                    }
                    catch { }
                }
            }
            MessageBox.Show("��װλ���޸�����ɡ�", "��ʾ��Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ThreadPool.QueueUserWorkItem(delegate { GetAppxVolume(); });
        }

        private void GetAppxVolume()
        {
            string outputString = "";
            try
            {
                using Process p = new();
                p.StartInfo.FileName = "powershell.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("Get-AppxVolume");
                p.StandardInput.Close();
                outputString = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch { }
            DataTable dt = new();
            DataColumn dcDirectoryRoot = dt.Columns.Add("DirectoryRoot", typeof(String));
            dt.Columns.Add("StorePath", typeof(String));
            dt.Columns.Add("IsOffline", typeof(Boolean));
            dt.PrimaryKey = new DataColumn[] { dcDirectoryRoot };
            Match result = Regex.Match(outputString, @"(?<Name>\\\\\?\\Volume\{\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\})\s+(?<PackageStorePath>.+)\s+(?<IsOffline>True|False)\s+(?<IsSystemVolume>True|False)");
            while (result.Success)
            {
                string storePath = result.Groups["PackageStorePath"].Value.Trim();
                string directoryRoot = Directory.GetDirectoryRoot(storePath);
                bool isOffline = result.Groups["IsOffline"].Value == "True";
                DataRow? dr = dt.Rows.Find(directoryRoot);
                if (dr == null)
                {
                    dr = dt.NewRow();
                    dr["DirectoryRoot"] = directoryRoot;
                    dr["StorePath"] = storePath;
                    dr["IsOffline"] = isOffline;
                    dt.Rows.Add(dr);
                }
                result = result.NextMatch();
            }
            cbAppxDrive.Tag = dt;
            this.Invoke(new Action(() =>
            {
                CbAppxDrive_SelectedIndexChanged(null, null);
            }));
        }

        private void ButAppxOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Title = "Open an Xbox Package"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            tbAppxFilePath.Text = ofd.FileName;
        }

        private void ButAppxInstall_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbAppxFilePath.Text)) return;
            ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
            if (service == null || service.Status != ServiceControllerStatus.Running)
            {
                MessageBox.Show("û�м�⵽��Ϸ����(Gaming Services)������������Ϸ������ִ�д˲�����", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            if (linkFixAppxDrive.Visible)
            {
                if (MessageBox.Show("��װĿ¼���������⣬�Ƿ�Ҫ������װ��", "��ʾ", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            }
            string filepath = tbAppxFilePath.Text;
            tbAppxFilePath.Clear();
            string cmd;
            if (Path.GetFileName(filepath) == "AppxManifest.xml")
            {
                /*
                �ƹ�΢���̵�Ӧ����ɲ���Ӧ��
                ʹ��˵����
                1������������ѡ�� ϵͳ->����->��˽�Ͱ�ȫ��->������ѡ��
                2�������ػ����� .appx ���� .appxbundle �ļ���ѹ������Ŀ¼
                3��ѡ�� AppxManifest.xml �����װ
                ���������������ڷ� eappx/eappxbundle ��װ����
                ΢���Ѿ���ʾ������һ��Ԥ�ڵĹ��ܣ��������ܲ���򲹶���
                */
                string appSignature = Path.GetDirectoryName(filepath) + "\\AppxSignature.p7x";
                if (File.Exists(appSignature))
                {
                    File.Move(appSignature, appSignature + ".bak");
                }
                cmd = "-noexit \"Add-AppPackage -Register '" + filepath + "'\necho ����ű�ִ����ϣ����û��������Ҫ����ֱ�ӹرմ˴���\"";
            }
            else
            {
                OperatingSystem os = Environment.OSVersion;
                if (os.Version.Major == 10 && os.Version.Build >= 22000)
                {
                    cmd = "-noexit \"Add-AppPackage -AllowUnsigned -Path '" + filepath + "' -Volume '" + cbAppxDrive.Text + "'\necho ����ű�ִ����ϣ����û��������Ҫ����ֱ�ӹرմ˴��ڡ�\"";
                }
                else
                {
                    cmd = "-noexit \"Add-AppPackage -Path '" + filepath + "' -Volume '" + cbAppxDrive.Text + "'\necho ����ű�ִ����ϣ����û��������Ҫ����ֱ�ӹرմ˴��ڡ�\"";
                }
            }
            try
            {
                using Process p = new();
                p.StartInfo.FileName = @"powershell.exe";
                p.StartInfo.Arguments = cmd;
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("��װ΢���̵�Ӧ�����ʧ�ܣ�������Ϣ��" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkRestartGamingServices_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkRestartGamingServices.Enabled = linkReInstallGamingServices.Enabled = false;
            ThreadPool.QueueUserWorkItem(delegate { ReStartGamingServices(); });
        }

        private void ReStartGamingServices()
        {
            bool bTimeOut = false, bDone = false;
            ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
            if (service != null)
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(10000);
                try
                {
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    }
                    if (service.Status != ServiceControllerStatus.Running)
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        if (service.Status == ServiceControllerStatus.Running) bDone = true;
                        else bTimeOut = true;
                    }
                    else bTimeOut = true;
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("������Ϸ�������\n������Ϣ��" + ex.Message, "������Ϸ�������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        linkRestartGamingServices.Enabled = linkReInstallGamingServices.Enabled = true;
                    }));
                    return;
                }
            }
            this.Invoke(new Action(() =>
            {
                if (bTimeOut)
                    MessageBox.Show("������Ϸ����ʱ����ѡ����װ��Ϸ����", "��ʾ��Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (bDone)
                    MessageBox.Show("������Ϸ��������ɡ�", "��ʾ��Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("�Ҳ�����Ϸ���񣬿���û�а�װ��", "��ʾ��Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                linkRestartGamingServices.Enabled = linkReInstallGamingServices.Enabled = true;
            }));
        }

        private void LinkReInstallGamingServices_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 10)
            {
                MessageBox.Show("ֻ֧��Win10�����ϰ汾����ϵͳ��", "����ϵͳ�汾����", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (MessageBox.Show("��ȷ���Ƿ�Ҫ��װ��Ϸ����", "��װ��Ϸ����", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                linkReInstallGamingServices.Enabled = linkRestartGamingServices.Enabled = false;
                linkReInstallGamingServices.Text = "��ȡ��Ϸ����Ӧ����������";
                ThreadPool.QueueUserWorkItem(delegate { ReInstallGamingServices(); });
            }
        }

        private void LinkAppGamingServices_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tbGameUrl.Text = "9MWPM2CQNLHN";
            ButGame_Click(sender, EventArgs.Empty);
            tabControl1.SelectedTab = tabStore;
        }

        private void ReInstallGamingServices()
        {
            XboxPackage.Data? data = null;
            string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage?WuCategoryId=f2ea4abe-4e1e-48ff-9022-a8a758303181", "GET", null, null, null, 30000, "XboxDownload");
            if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
            {
                XboxPackage.App? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.Code != null && json.Code == "200")
                {
                    data = json.Data.Where(x => x.Name.ToLower().EndsWith(".appxbundle")).FirstOrDefault();
                }
            }
            if (data != null)
            {
                this.Invoke(new Action(() =>
                {
                    linkReInstallGamingServices.Text = "������Ϸ����Ӧ�ð�װ��";
                }));
                string filePath = Path.GetTempPath() + data.Name;
                if (File.Exists(filePath))
                    File.Delete(filePath);
                using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(data.Url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                    try
                    {
                        using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                        fs.Write(buffer, 0, buffer.Length);
                        fs.Flush();
                        fs.Close();
                    }
                    catch { }
                }
                else
                {
                    string msg = response != null ? "����ʧ�ܣ�������Ϣ��" + response.ReasonPhrase : "����ʧ��";
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                if (File.Exists(filePath))
                {
                    this.Invoke(new Action(() =>
                    {
                        linkReInstallGamingServices.Text = "��װ��Ϸ����";
                    }));
                    try
                    {
                        using Process p = new();
                        p.StartInfo.FileName = @"powershell.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.StandardInput.WriteLine("Get-AppxPackage Microsoft.GamingServices | Remove-AppxPackage -AllUsers");
                        p.StandardInput.WriteLine("Add-AppxPackage \"" + filePath + "\"");
                        p.StandardInput.WriteLine("exit");
                        p.WaitForExit();
                    }
                    catch { }
                    this.Invoke(new Action(() =>
                    {
                        linkReInstallGamingServices.Text = "������ʱ�ļ�";
                        MessageBox.Show("��װ��Ϸ��������ɡ�", "��ʾ��Ϣ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));

                    DateTime t1 = DateTime.Now.AddSeconds(30);
                    bool completed = false;
                    while (!completed)
                    {
                        ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
                        if (service != null && service.Status == ServiceControllerStatus.Running)
                            completed = true;
                        else if (DateTime.Compare(t1, DateTime.Now) <= 0)
                            break;
                        else
                            Thread.Sleep(100);
                    }
                    File.Delete(filePath);
                    this.Invoke(new Action(() =>
                    {
                        linkReInstallGamingServices.Text = "һ����װ��Ϸ����";
                        linkReInstallGamingServices.Enabled = linkRestartGamingServices.Enabled = true;
                    }));
                    if (!completed)
                    {
                        try
                        {
                            Process.Start("ms-windows-store://pdp/?productid=9mwpm2cqnlhn");
                        }
                        catch { }
                    }
                    return;
                }
            }
            try
            {
                using Process p = new();
                p.StartInfo.FileName = @"powershell.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("get-appxpackage Microsoft.GamingServices | remove-AppxPackage -allusers");
                p.StandardInput.WriteLine("start ms-windows-store://pdp/?productid=9mwpm2cqnlhn");
                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
            }
            catch { }
            this.Invoke(new Action(() =>
            {
                linkReInstallGamingServices.Text = "һ����װ��Ϸ����";
                linkReInstallGamingServices.Enabled = linkRestartGamingServices.Enabled = true;
            }));
        }
        #endregion
    }
}