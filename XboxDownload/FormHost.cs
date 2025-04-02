﻿using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormHost : Form
    {
        public String host = string.Empty, ip = string.Empty;

        public FormHost()
        {
            InitializeComponent();

            if (Form1.dpiFactor > 1)
            {
                dataGridView1.RowHeadersWidth = (int)(dataGridView1.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
            }

            List<DataGridViewRow> listDgvr = new();
            for (int i = 0; i <= DnsListen.dohs.GetLongLength(0) - 1; i++)
            {
                DataGridViewRow dgvr = new();
                dgvr.CreateCells(dataGridView1);
                dgvr.Resizable = DataGridViewTriState.False;
                string name = DnsListen.dohs[i, 0];
                dgvr.Cells[0].Value = true;
                dgvr.Cells[1].Value = name;
                dgvr.Cells[0].ToolTipText = null;
                dgvr.Cells[1].ToolTipText = null;
                dgvr.Cells[2].ToolTipText = null;
                dgvr.Cells[3].ToolTipText = null;
                listDgvr.Add(dgvr);
            }
            if (listDgvr.Count >= 1) dataGridView1.Rows.AddRange(listDgvr.ToArray());
        }

        private void FormHost_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
        }

        private void TbHost_Validating(object sender, CancelEventArgs e)
        {
            tbHost.Text = Regex.Replace(Regex.Replace(tbHost.Text.Trim(), @"^(https?://)?([^/|:]+).*$", "$2"), @"\s.*", "").ToLower();
        }

        private void ButConfirm_Click(object sender, EventArgs e)
        {
            string host = tbHost.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("域名不能空", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbHost.Focus();
                return;
            }
            string ip = tbIP.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("IP地址不能空", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbIP.Focus();
                return;
            }
            this.host = host;
            this.ip = ip;
            this.Close();
        }

        private void Dgv_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            Rectangle rectangle = new(e.RowBounds.Location.X, e.RowBounds.Location.Y, dgv.RowHeadersWidth - 1, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), dgv.RowHeadersDefaultCellStyle.Font, rectangle, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow dgvr = dataGridView1.Rows[e.RowIndex];
            string? ip = dgvr.Cells[2].Value?.ToString();
            if (!string.IsNullOrEmpty(ip) && IPAddress.TryParse(ip, out IPAddress? _ip))
            {
                tbIP.Text = _ip.ToString();
                tbIP.Focus();
                tbIP.SelectAll();
                butConfirm.Enabled = true;
            }
        }

        private void CbCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow dgvr in dataGridView1.Rows)
            {
                if (dgvr.IsNewRow) break;
                dgvr.Cells[0].Value = cbCheckAll.Checked;
            }
        }

        private async void ButTest_Click(object sender, EventArgs e)
        {
            string host = tbHost.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("域名不能空", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbHost.Focus();
                return;
            }
            if (!DnsListen.reHosts.IsMatch(host))
            {
                MessageBox.Show("域名不正确", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbHost.Focus();
                return;
            }

            butTest.Enabled = false;
            dataGridView1.ClearSelection();
            bool ipv4 = rbIPv4.Checked;
            Uri uri = new("https://" + host);
            DataGridViewRow[] rows = dataGridView1.Rows.Cast<DataGridViewRow>().Where(row => Convert.ToBoolean(row.Cells[0].Value) == true).ToArray();
            var tasks = rows.Select(dgvr => Task.Run(async () => {
                dgvr.Cells[2].Value = dgvr.Cells[3].Value = dgvr.Cells[4].Value = null;
                dgvr.Cells[2].Style.ForeColor = dgvr.Cells[3].Style.ForeColor = Color.Empty;
                dgvr.Cells[3].ToolTipText = null;
                string dohServer = DnsListen.dohs[dgvr.Index, 1];
                string dohHost = DnsListen.dohs[dgvr.Index, 2];
                Dictionary<string, string>? dohHeaders = null;
                if (!string.IsNullOrEmpty(dohHost))
                {
                    dohHeaders = new Dictionary<string, string> { { "Host", dohHost } };
                }
                string? ip = ClassDNS.DoH(host, dohServer, dohHeaders, ipv4);
                if (this.IsDisposed) return;
                if (IPAddress.TryParse(ip, out IPAddress? address))
                {
                    dgvr.Cells[2].Value = ip;
                    Task[] tasks = new Task[2];
                    tasks[0] = Task.Run(() => {
                        Stopwatch sw = new();
                        sw.Start();
                        bool verified = ClassWeb.ConnectTest(uri, address, true, out string errMessage);
                        sw.Stop();
                        if (this.IsDisposed) return;
                        if (verified)
                        {
                            dgvr.Cells[3].Value = sw.ElapsedMilliseconds.ToString("N0") + " ms";
                            dgvr.Cells[3].Style.ForeColor = Color.Green;
                        }
                        else
                        {
                            dgvr.Cells[3].Value = "×";
                            dgvr.Cells[3].ToolTipText = "Message: " + errMessage;
                            dgvr.Cells[3].Style.ForeColor = Color.Red;
                        }
                    });
                    tasks[1] = Task.Run(() => {
                        string location = ClassDNS.QueryLocation(ip);
                        if (this.IsDisposed) return;
                        dgvr.Cells[4].Value = location;
                    });
                    await Task.WhenAll(tasks);
                }
                else
                {
                    dgvr.Cells[2].Value = "error";
                    dgvr.Cells[2].Style.ForeColor = Color.Red;
                }
            })).ToArray();
            await Task.WhenAll(tasks);
            butTest.Enabled = true;
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/skydevil88/XboxDownload/discussions/96") { UseShellExecute = true });
        }
    }
}
