﻿using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormSniProxy : Form
    {
        private readonly Dictionary<string, string[]> dicService = new()
        {
            { "Steam 商店社区", new string[] { "store.steampowered.com", "api.steampowered.com", "login.steampowered.com", "help.steampowered.com", "checkout.steampowered.com", "steamcommunity.com" } },
            { "GitHub", new string[] { "*github.blog", "*github.com", "*github.io", "*githubassets.com", "*githubstatus.com", "*githubusercontent.com" } },
            { "Wikipedia", new string[] { "*wikipedia.org, *wikimedia.org, *mediawiki.org, *wikibooks.org, *wikidata.org, *wikinews.org, *wikiquote.org, *wikisource.org, *wikiversity.org, *wikivoyage.org, *wiktionary.org, *wikimediafoundation.org, *wmfusercontent.org, *wikifunctions.org, w.wiki | 2a02:ec80:600:ed1a::1, 2a02:ec80:300:ed1a::1, 2620:0:860:ed1a::1, 2620:0:861:ed1a::1, 2a02:ec80:700:ed1a::1, 185.15.59.224, 185.15.58.224, 208.80.153.224, 195.200.68.224", "upload.wikimedia.org | 2620:0:861:ed1a::2:b, 2620:0:860:ed1a::2:b, 2620:0:863:ed1a::2:b, 2a02:ec80:300:ed1a::2:b, 2001:df2:e500:ed1a::2:b, 2a02:ec80:600:ed1a::2:b, 2a02:ec80:700:ed1a::2:b, 185.15.59.240, 185.15.58.240, 208.80.153.240, 208.80.154.240, 195.200.68.240" } },
            { "Pixiv", new string[] { "*pixiv.net -> pixiv.net", "*.pximg.net -> pximg.net" } },
            //{ "Unidentified", new string[] { "www.youporn.com", "www.xvideos.com", "www.t66y.com" } },
        };

        public FormSniProxy()
        {
            InitializeComponent();

            string SniProxyFilePath = Path.Combine(Form1.resourceDirectory, "SniProxy.json");
            if (File.Exists(SniProxyFilePath))
            {
                List<List<object>>? SniProxy = null;
                try
                {
                    SniProxy = JsonSerializer.Deserialize<List<List<object>>>(File.ReadAllText(SniProxyFilePath));
                }
                catch { }
                if (SniProxy != null)
                {
                    StringBuilder sb = new();
                    foreach (var item in SniProxy)
                    {
                        if (item.Count == 3)
                        {
                            JsonElement jeHosts = (JsonElement)item[0];
                            if (jeHosts.ValueKind != JsonValueKind.Array) continue;
                            string? hosts = string.Join(", ", jeHosts.EnumerateArray().Select(x => x.GetString()?.Trim()));
                            if (string.IsNullOrEmpty(hosts)) continue;
                            string? fakeHost = item[1]?.ToString()?.Trim();
                            string? ip = item[2]?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(fakeHost) && string.IsNullOrEmpty(ip))
                                sb.AppendLine(hosts);
                            else if (!string.IsNullOrEmpty(fakeHost) && !string.IsNullOrEmpty(ip))
                                sb.AppendLine(hosts + " | " + fakeHost + " | " + ip);
                            else
                                sb.AppendLine(hosts + " | " + fakeHost + ip);
                        }
                    }
                    textBox1.Text = sb.ToString();
                }
            }

            checkedListBox1.Items.AddRange(dicService.Keys.ToArray());

            int total = 0;
            List<int> ls = new();
            foreach (string part in Properties.Settings.Default.SniProxys.Split(','))
            {
                ls.Add(int.Parse(part));
            }
            for (int i = 0; i <= DnsListen.dohs.GetLongLength(0) - 1; i++)
            {
                checkedListBox2.Items.Add(DnsListen.dohs[i, 0]);
                if (ls.Contains(i))
                {
                    checkedListBox2.SetItemCheckState(i, CheckState.Checked);
                    total++;
                }
            }
            groupBox3.Text = Regex.Replace(groupBox3.Text, @"\d+", total.ToString());
            cbSniProxysIPv6.Checked = Properties.Settings.Default.SniProxysIPv6;
            cbSniPorxyOptimized.Checked = Properties.Settings.Default.SniPorxyOptimized;
            nudSniPorxyExpired.Value = Properties.Settings.Default.SniPorxyExpired;
            if (!Form1.bIPv6Support)
            {
                Font font = cbSniProxysIPv6.Font;
                cbSniProxysIPv6.Font = new Font(font.FontFamily, font.Size, FontStyle.Strikeout, GraphicsUnit.Point);
                cbSniProxysIPv6.ForeColor = Color.Red;
            }
        }

        private void FormSniProxy_Load(object sender, EventArgs e)
        {
            checkedListBox2.ItemCheck += CheckedListBox2_ItemCheck;
        }

        private void CheckedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string? key = checkedListBox1.Items[e.Index].ToString();
            if (!string.IsNullOrEmpty(key) && dicService.TryGetValue(key, out string[]? hosts1))
            {
                StringBuilder sb = new();
                foreach (string host in textBox1.Text.Trim().ReplaceLineEndings().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.IsNullOrEmpty(host)) continue;
                    if (!Array.Exists(hosts1, element => element.Equals(host)))
                    {
                        sb.AppendLine(host);
                    }
                }
                string hosts2 = sb.ToString();
                if (e.NewValue == CheckState.Checked)
                {
                    string hosts = string.Join(Environment.NewLine, hosts1) + Environment.NewLine;
                    textBox1.Text = hosts + hosts2;
                    textBox1.Focus();
                    textBox1.Select(0, hosts.Length - 2);
                }
                else textBox1.Text = hosts2;
            }
        }

        private void CheckedListBox2_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            // 在状态改变后计算总数
            this.BeginInvoke(new Action(() =>
            {
                groupBox3.Text = Regex.Replace(groupBox3.Text, @"\d+", checkedListBox2.CheckedItems.Count.ToString());
            }));
        }

        private void LinkTest_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormDoH dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private async void LinkTestIPv6_LinkClickedAsync(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkTestIPv6.Enabled = false;
            Form1.bIPv6Support = await ClassWeb.TestIPv6();
            if (Form1.bIPv6Support)
            {
                Font font = cbSniProxysIPv6.Font;
                cbSniProxysIPv6.Font = new Font(font.FontFamily, font.Size);
                cbSniProxysIPv6.ForeColor = Color.Empty;
                MessageBox.Show("当前网络可以使用IPv6。", "检测IPv6", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
            else
            {
                Font font = cbSniProxysIPv6.Font;
                cbSniProxysIPv6.Font = new Font(font.FontFamily, font.Size, FontStyle.Strikeout, GraphicsUnit.Point);
                cbSniProxysIPv6.ForeColor = Color.Red;
                MessageBox.Show("当前网络不支持IPv6。", "检测IPv6", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            foreach (var proxy in HttpsListen.dicSniProxy.Values)
            {
                proxy.IPs = null;
            }
            linkTestIPv6.Enabled = true;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string ipPattern = @"^((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|" +                                                                     //匹配包含8组1到4个十六进制数字、由冒号分隔的IPv6地址。
                               @"(([0-9A-Fa-f]{1,4}:){1,7}:)|" +                                                                                        //匹配以零压缩开头的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,6}:[0-9A-Fa-f]{1,4})|" +                                                                        //匹配带有一组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,5}(:[0-9A-Fa-f]{1,4}){1,2})|" +                                                                 //匹配带有两组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,4}(:[0-9A-Fa-f]{1,4}){1,3})|" +                                                                 //匹配带有三组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,3}(:[0-9A-Fa-f]{1,4}){1,4})|" +                                                                 //匹配带有四组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,2}(:[0-9A-Fa-f]{1,4}){1,5})|" +                                                                 //匹配带有五组零的地址
                               @"([0-9A-Fa-f]{1,4}:((:[0-9A-Fa-f]{1,4}){1,6}))|" +                                                                      //匹配带有六组零的地址
                               @"(:((:[0-9A-Fa-f]{1,4}){1,7}|:))|" +                                                                                    //匹配以零开头并包含七组零或一个冒号的地址
                               @"(::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))|" +  //匹配包含嵌入IPv4地址的IPv6地址，带有可选的前导零和可选的ffff组
                               @"(([0-9A-Fa-f]{1,4}:){1,4}:((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))|" +     //匹配包含最多四组后跟嵌入IPv4地址的IPv6地址
                               @"((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))$";                                //匹配IPv4地址
            Regex reIP = new(ipPattern);

            List<List<object>> lsSniProxy = new();
            foreach (string str in textBox1.Text.Trim().ReplaceLineEndings().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string[] proxy = str.Split('|');
                if (proxy.Length == 0) continue;
                ArrayList arrHost = new();
                string sni = string.Empty;
                List<IPAddress>? lsIPv6 = new(), lsIPv4 = new();
                if (proxy.Length >= 1)
                {
                    foreach (string host in proxy[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        string _host = Regex.Replace(host.ToLower().Trim(), @"^(https?://)?([^/|:]+).*$", "$2").Trim();
                        if (!string.IsNullOrEmpty(_host))
                        {
                            arrHost.Add(Regex.Replace(_host, @"\s*->\s*", " -> "));
                        }
                    }
                }
                if (proxy.Length == 2)
                {
                    proxy[1] = proxy[1].Trim();
                    string[] _ips = proxy[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (_ips.Length == 1)
                    {
                        if (reIP.IsMatch(proxy[1]) && IPAddress.TryParse(proxy[1], out var ip))
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !lsIPv6.Contains(ip))
                                lsIPv6.Add(ip);
                            else if (ip.AddressFamily == AddressFamily.InterNetwork && !lsIPv4.Contains(ip))
                                lsIPv4.Add(ip);
                        }
                        else sni = Regex.Replace(proxy[1].ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                    }
                    else
                    {
                        foreach (string _ip in _ips)
                        {
                            if (IPAddress.TryParse(_ip.Trim(), out var ip))
                            {
                                if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !lsIPv6.Contains(ip))
                                    lsIPv6.Add(ip);
                                else if (ip.AddressFamily == AddressFamily.InterNetwork && !lsIPv4.Contains(ip))
                                    lsIPv4.Add(ip);
                            }
                        }
                    }
                }
                else if (proxy.Length >= 3)
                {
                    sni = Regex.Replace(proxy[1].ToLower().Trim(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                    string[] _ips = proxy[2].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    foreach (string _ip in _ips)
                    {
                        if (IPAddress.TryParse(_ip.Trim(), out var ip))
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !lsIPv6.Contains(ip))
                                lsIPv6.Add(ip);
                            else if (ip.AddressFamily == AddressFamily.InterNetwork && !lsIPv4.Contains(ip))
                                lsIPv4.Add(ip);
                        }
                    }
                }
                if (arrHost.Count >= 1)
                {
                    lsSniProxy.Add(new List<object> { arrHost, sni, String.Join(", ", lsIPv6.Union(lsIPv4).Take(16).ToList<IPAddress>()) });
                }
            }
            string SniProxyFilePath = Path.Combine(Form1.resourceDirectory, "SniProxy.json");
            if (lsSniProxy.Count >= 1)
            {
                if (!Directory.Exists(Form1.resourceDirectory)) Directory.CreateDirectory(Form1.resourceDirectory);
                File.WriteAllText(SniProxyFilePath, JsonSerializer.Serialize(lsSniProxy, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (File.Exists(SniProxyFilePath))
            {
                File.Delete(SniProxyFilePath);
            }

            List<int> ls = new();
            for (int i = 0; i <= checkedListBox2.Items.Count - 1; i++)
            {
                if (checkedListBox2.GetItemChecked(i))
                    ls.Add(i);
            }
            if (ls.Count == 0) ls.Add(3);
            Properties.Settings.Default.SniProxys = string.Join(',', ls.ToArray());
            Properties.Settings.Default.SniProxysIPv6 = cbSniProxysIPv6.Checked;
            Properties.Settings.Default.SniPorxyOptimized = cbSniPorxyOptimized.Checked;
            Properties.Settings.Default.SniPorxyExpired = (int)nudSniPorxyExpired.Value;
            Properties.Settings.Default.Save();
            HttpsListen.CreateCertificate();
            this.Close();
        }
    }
}
