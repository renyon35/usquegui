using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

class ProxySettings {
    public string Mode = "http-proxy";
    public string Bind = "0.0.0.0";
    public int Port = 1080;
    public string Username = "";
    public string Password = "";
    public string ConfigFile = "config.json";
    public string Dns = "1.1.1.1\n1.0.0.1";
    public string DnsTimeout = "2s";
    public bool LocalDns;
    public bool SystemDns;
    public string SniAddress = "consumer-masque.cloudflareclient.com";
    public string ConnectPort = "443";
    public bool UseHttp2;
    public bool UseIpv6;
    public bool AlwaysReconnect;
    public string ReconnectDelay = "1s";
    public string KeepalivePeriod = "30s";
    public string Mtu = "1280";
    public string InitialPacketSize = "";
    public bool NoTunnelIpv4;
    public bool NoTunnelIpv6;
    public bool Insecure;
    public string OnConnect = "";
    public string OnDisconnect = "";

    public ProxySettings Copy() {
        return new ProxySettings {
            Mode = Mode,
            Bind = Bind,
            Port = Port,
            Username = Username,
            Password = Password,
            ConfigFile = ConfigFile,
            Dns = Dns,
            DnsTimeout = DnsTimeout,
            LocalDns = LocalDns,
            SystemDns = SystemDns,
            SniAddress = SniAddress,
            ConnectPort = ConnectPort,
            UseHttp2 = UseHttp2,
            UseIpv6 = UseIpv6,
            AlwaysReconnect = AlwaysReconnect,
            ReconnectDelay = ReconnectDelay,
            KeepalivePeriod = KeepalivePeriod,
            Mtu = Mtu,
            InitialPacketSize = InitialPacketSize,
            NoTunnelIpv4 = NoTunnelIpv4,
            NoTunnelIpv6 = NoTunnelIpv6,
            Insecure = Insecure,
            OnConnect = OnConnect,
            OnDisconnect = OnDisconnect
        };
    }

    public string BuildArguments(bool redactPassword) {
        List<string> args = new List<string>();
        args.Add(Mode == "socks" ? "socks" : "http-proxy");
        AddValue(args, "-b", Bind);
        AddValue(args, "-p", Port.ToString());
        if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password)) {
            AddValue(args, "-u", Username);
            AddValue(args, "-w", redactPassword ? "********" : Password);
        }
        AddValue(args, "-c", ConfigFile);
        foreach (string dns in Dns.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)) {
            AddValue(args, "-d", dns.Trim());
        }
        AddValue(args, "-t", DnsTimeout);
        if (LocalDns || SystemDns) args.Add("-l");
        if (SystemDns) args.Add("--system-dns");
        AddValue(args, "-s", SniAddress);
        AddValue(args, "-P", ConnectPort);
        if (UseHttp2) args.Add("--http2");
        if (UseIpv6) args.Add("-6");
        if (AlwaysReconnect) args.Add("--always-reconnect");
        AddValue(args, "-r", ReconnectDelay);
        AddValue(args, "-k", KeepalivePeriod);
        AddValue(args, "-m", Mtu);
        AddValue(args, "-i", InitialPacketSize);
        if (NoTunnelIpv4) args.Add("-F");
        if (NoTunnelIpv6) args.Add("-S");
        if (Insecure) args.Add("--insecure");
        AddValue(args, "--on-connect", OnConnect);
        AddValue(args, "--on-disconnect", OnDisconnect);
        return string.Join(" ", args.ToArray());
    }

    private static void AddValue(List<string> args, string name, string value) {
        if (string.IsNullOrEmpty(value)) return;
        args.Add(name);
        args.Add("\"" + value.Replace("\"", "\\\"") + "\"");
    }
}

class SettingsForm : Form {
    private ProxySettings result;
    private TextBox bindBox;
    private TextBox portBox;
    private TextBox usernameBox;
    private TextBox passwordBox;
    private TextBox configBox;
    private TextBox dnsBox;
    private TextBox dnsTimeoutBox;
    private TextBox sniBox;
    private TextBox connectPortBox;
    private TextBox reconnectDelayBox;
    private TextBox keepaliveBox;
    private TextBox mtuBox;
    private TextBox packetSizeBox;
    private TextBox onConnectBox;
    private TextBox onDisconnectBox;
    private CheckBox localDnsBox;
    private CheckBox systemDnsBox;
    private CheckBox http2Box;
    private CheckBox ipv6Box;
    private CheckBox reconnectBox;
    private CheckBox noIpv4Box;
    private CheckBox noIpv6Box;
    private CheckBox insecureBox;
    private TextBox previewBox;
    private ComboBox modeBox;

    public ProxySettings ResultSettings {
        get { return result; }
    }

    public SettingsForm(ProxySettings settings) {
        result = settings.Copy();
        Text = "代理设置";
        Size = new Size(640, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = SystemColors.Control;
        ForeColor = SystemColors.ControlText;
        Font = SystemFonts.MessageBoxFont;

        TabControl tabs = new TabControl {
            Location = new Point(12, 12),
            Size = new Size(600, 342)
        };
        tabs.TabPages.Add(BuildProxyTab());
        tabs.TabPages.Add(BuildDnsTab());
        tabs.TabPages.Add(BuildTunnelTab());

        Label previewLabel = new Label {
            Text = "启动命令预览（密码已隐藏）",
            AutoSize = true,
            Location = new Point(12, 364)
        };
        previewBox = new TextBox {
            ReadOnly = true,
            Location = new Point(12, 384),
            Size = new Size(600, 24),
            Font = new Font("Consolas", 9),
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.WindowText
        };

        Button resetButton = new Button {
            Text = "恢复默认",
            Size = new Size(88, 30),
            Location = new Point(370, 424)
        };
        Button cancelButton = new Button {
            Text = "取消",
            Size = new Size(88, 30),
            Location = new Point(466, 424)
        };
        Button saveButton = new Button {
            Text = "保存",
            Size = new Size(88, 30),
            Location = new Point(562, 424)
        };
        resetButton.Click += (s, e) => {
            SetValues(new ProxySettings());
            UpdatePreview();
        };
        cancelButton.Click += (s, e) => {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        saveButton.Click += (s, e) => {
            ProxySettings settingsValue;
            if (!ReadValues(out settingsValue)) return;
            if (!string.IsNullOrEmpty(settingsValue.Username) != !string.IsNullOrEmpty(settingsValue.Password)) {
                MessageBox.Show("认证用户名和密码必须同时填写。", "代理设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            result = settingsValue;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(new Control[] { tabs, previewLabel, previewBox, resetButton, cancelButton, saveButton });
        AcceptButton = saveButton;
        CancelButton = cancelButton;
        SetValues(result);
        UpdatePreview();
    }

    private TabPage BuildProxyTab() {
        TabPage page = new TabPage("代理");
        page.AutoScroll = true;
        Label modeLabel = new Label {
            Text = "代理模式",
            AutoSize = true,
            Location = new Point(16, 188)
        };
        modeBox = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(16, 208),
            Width = 200
        };
        modeBox.Items.AddRange(new object[] {"HTTP", "SOCKS5"});
        page.Controls.Add(modeLabel);
        page.Controls.Add(modeBox);
        bindBox = AddText(page, "监听地址 (-b)", result.Bind, 16, 20, 240, false);
        portBox = AddText(page, "监听端口 (-p)", result.Port.ToString(), 320, 20, 140, false);
        usernameBox = AddText(page, "认证用户名 (-u)", result.Username, 16, 76, 240, false);
        passwordBox = AddText(page, "认证密码 (-w)", result.Password, 320, 76, 140, false);
        passwordBox.UseSystemPasswordChar = true;
        configBox = AddText(page, "配置文件 (-c)", result.ConfigFile, 16, 132, 444, false);
        AddNote(page, "SOCKS5 模式使用 usque socks，HTTP 模式使用 usque http-proxy。", 240, 212);
        AddNote(page, "用户名和密码需同时填写。留空表示代理不需要认证。", 16, 244);
        AddNote(page, "配置文件为相对路径，相对于启动器所在目录。", 16, 268);
        return page;
    }

    private TabPage BuildDnsTab() {
        TabPage page = new TabPage("DNS");
        page.AutoScroll = true;
        dnsBox = AddText(page, "DNS 服务器 (-d，每行一个)", result.Dns, 16, 20, 270, true);
        dnsBox.Height = 190;
        dnsTimeoutBox = AddText(page, "DNS 超时 (-t，例如 2s)", result.DnsTimeout, 330, 20, 220, false);
        localDnsBox = AddCheck(page, "使用主机 DNS (-l)", result.LocalDns, 330, 80);
        systemDnsBox = AddCheck(page, "使用系统 DNS (--system-dns)", result.SystemDns, 330, 108);
        AddNote(page, "本地 DNS 仅在勾选 -l 或 --system-dns 时生效。", 330, 148);
        return page;
    }

    private TabPage BuildTunnelTab() {
        TabPage page = new TabPage("隧道");
        page.AutoScroll = true;
        sniBox = AddText(page, "SNI (-s)", result.SniAddress, 16, 20, 250, false);
        connectPortBox = AddText(page, "MASQUE 端口 (-P)", result.ConnectPort, 316, 20, 120, false);
        reconnectDelayBox = AddText(page, "重连间隔 (-r)", result.ReconnectDelay, 16, 76, 250, false);
        keepaliveBox = AddText(page, "Keepalive (-k)", result.KeepalivePeriod, 316, 76, 120, false);
        mtuBox = AddText(page, "MTU (-m)", result.Mtu, 16, 132, 120, false);
        packetSizeBox = AddText(page, "初始包大小 (-i)", result.InitialPacketSize, 172, 132, 94, false);
        http2Box = AddCheck(page, "使用 HTTP/2 (--http2)", result.UseHttp2, 316, 130);
        ipv6Box = AddCheck(page, "使用 IPv6 连接 (-6)", result.UseIpv6, 316, 158);
        reconnectBox = AddCheck(page, "隧道断开后始终重连 (--always-reconnect)", result.AlwaysReconnect, 316, 186);
        onConnectBox = AddText(page, "连接后执行 (--on-connect)", result.OnConnect, 16, 188, 250, false);
        onDisconnectBox = AddText(page, "断开后执行 (--on-disconnect)", result.OnDisconnect, 16, 244, 250, false);
        noIpv4Box = AddCheck(page, "禁用隧道内 IPv4 (-F)", result.NoTunnelIpv4, 16, 300);
        noIpv6Box = AddCheck(page, "禁用隧道内 IPv6 (-S)", result.NoTunnelIpv6, 176, 300);
        insecureBox = AddCheck(page, "跳过证书固定 (--insecure)", result.Insecure, 316, 218);
        return page;
    }

    private TextBox AddText(TabPage page, string caption, string value, int x, int y, int width, bool multiline) {
        Label label = new Label {
            Text = caption,
            AutoSize = true,
            Location = new Point(x, y)
        };
        TextBox input = new TextBox {
            Text = value,
            Location = new Point(x, y + 20),
            Width = width,
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
            Height = multiline ? 64 : 23
        };
        page.Controls.Add(label);
        page.Controls.Add(input);
        return input;
    }

    private CheckBox AddCheck(TabPage page, string caption, bool value, int x, int y) {
        CheckBox input = new CheckBox {
            Text = caption,
            Checked = value,
            AutoSize = true,
            Location = new Point(x, y)
        };
        page.Controls.Add(input);
        return input;
    }

    private void AddNote(TabPage page, string caption, int x, int y) {
        Label note = new Label {
            Text = caption,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Location = new Point(x, y)
        };
        page.Controls.Add(note);
    }

    private void SetValues(ProxySettings settings) {
        modeBox.SelectedItem = settings.Mode == "socks" ? "SOCKS5" : "HTTP";
        bindBox.Text = settings.Bind;
        portBox.Text = settings.Port.ToString();
        usernameBox.Text = settings.Username;
        passwordBox.Text = settings.Password;
        configBox.Text = settings.ConfigFile;
        dnsBox.Text = settings.Dns;
        dnsTimeoutBox.Text = settings.DnsTimeout;
        sniBox.Text = settings.SniAddress;
        connectPortBox.Text = settings.ConnectPort;
        reconnectDelayBox.Text = settings.ReconnectDelay;
        keepaliveBox.Text = settings.KeepalivePeriod;
        mtuBox.Text = settings.Mtu;
        packetSizeBox.Text = settings.InitialPacketSize;
        onConnectBox.Text = settings.OnConnect;
        onDisconnectBox.Text = settings.OnDisconnect;
        localDnsBox.Checked = settings.LocalDns;
        systemDnsBox.Checked = settings.SystemDns;
        http2Box.Checked = settings.UseHttp2;
        ipv6Box.Checked = settings.UseIpv6;
        reconnectBox.Checked = settings.AlwaysReconnect;
        noIpv4Box.Checked = settings.NoTunnelIpv4;
        noIpv6Box.Checked = settings.NoTunnelIpv6;
        insecureBox.Checked = settings.Insecure;
    }

    private bool ReadValues(out ProxySettings settings) {
        settings = new ProxySettings();
        int port;
        int connectPort;
        int mtu;
        int packetSize;
        if (!TryInt(portBox.Text, out port) || port < 1 || port > 65535) {
            MessageBox.Show("监听端口必须是 1 到 65535 之间的整数。", "代理设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (!TryInt(connectPortBox.Text, out connectPort) || connectPort < 1 || connectPort > 65535) {
            MessageBox.Show("MASQUE 端口必须是 1 到 65535 之间的整数。", "代理设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (!TryInt(mtuBox.Text, out mtu) || mtu < 68 || mtu > 65535) {
            MessageBox.Show("MTU 必须是 68 到 65535 之间的整数。", "代理设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (!TryInt(packetSizeBox.Text, out packetSize) || (packetSize != 0 && (packetSize < 1200 || packetSize > 65535))) {
            MessageBox.Show("初始包大小必须是 1200 到 65535 之间的整数，或留空。", "代理设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        settings.Mode = modeBox.SelectedItem != null && modeBox.SelectedItem.ToString() == "SOCKS5" ? "socks" : "http-proxy";
        settings.Bind = bindBox.Text.Trim();
        settings.Port = port;
        settings.Username = usernameBox.Text;
        settings.Password = passwordBox.Text;
        settings.ConfigFile = configBox.Text.Trim();
        if (string.IsNullOrEmpty(settings.ConfigFile) || Path.IsPathRooted(settings.ConfigFile)) {
            MessageBox.Show("配置文件必须使用相对路径，例如 config.json。", "代理设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        settings.Dns = dnsBox.Text.Trim();
        settings.DnsTimeout = dnsTimeoutBox.Text.Trim();
        settings.LocalDns = localDnsBox.Checked;
        settings.SystemDns = systemDnsBox.Checked;
        settings.SniAddress = sniBox.Text.Trim();
        settings.ConnectPort = connectPort.ToString();
        settings.UseHttp2 = http2Box.Checked;
        settings.UseIpv6 = ipv6Box.Checked;
        settings.AlwaysReconnect = reconnectBox.Checked;
        settings.ReconnectDelay = reconnectDelayBox.Text.Trim();
        settings.KeepalivePeriod = keepaliveBox.Text.Trim();
        settings.Mtu = mtu.ToString();
        settings.InitialPacketSize = packetSize == 0 ? "" : packetSize.ToString();
        settings.NoTunnelIpv4 = noIpv4Box.Checked;
        settings.NoTunnelIpv6 = noIpv6Box.Checked;
        settings.Insecure = insecureBox.Checked;
        settings.OnConnect = onConnectBox.Text.Trim();
        settings.OnDisconnect = onDisconnectBox.Text.Trim();
        return true;
    }

    private static bool TryInt(string text, out int value) {
        if (string.IsNullOrEmpty(text.Trim())) {
            value = 0;
            return true;
        }
        return int.TryParse(text.Trim(), out value);
    }

    private void UpdatePreview() {
        ProxySettings settings;
        if (ReadValues(out settings)) {
            previewBox.Text = "usque.exe " + settings.BuildArguments(true);
        } else {
            previewBox.Text = "请修正参数后查看命令";
        }
    }
}

class ProxyLauncher {
    static string BaseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    static string UsqueExe = Path.Combine(BaseDir, "usque.exe");
    static string RegBat = Path.Combine(BaseDir, "reg.bat");
    static string SettingsPath = "proxy.ini";
    static ProxySettings Config = new ProxySettings();
    static string ProxyHost = "0.0.0.0";
    static int ProxyPort = 1080;

    static Process WarpProcess = null;
    static bool Started = false;
    static StringBuilder WarpOutput = new StringBuilder();
    static StringBuilder WarpError = new StringBuilder();
    static readonly object WarpLogLock = new object();

    static TextBox LogBox;
    static Label ProxyLabel;
    static Button BtnStart;
    static Button BtnStop;
    static Button BtnSettings;
    static Button BtnToggleLog;
    static ProgressBar Progress;
    static NotifyIcon TrayIcon;
    static Form MainForm;
    static Form LogForm;

    [STAThread]
    static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        LoadSettings();

        MainForm = new Form {
            Text = "WARP 代理",
            Size = new Size(380, 180),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText,
            Font = SystemFonts.MessageBoxFont
        };

        Label title = new Label {
            Text = "WARP 代理启动器",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            ForeColor = SystemColors.ControlText,
            AutoSize = true,
            Location = new Point(12, 12)
        };
        ProxyLabel = new Label {
            Text = ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort,
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(12, 46)
        };
        BtnToggleLog = new Button {
            Text = "显示日志",
            Size = new Size(88, 26),
            Location = new Point(276, 10),
            FlatStyle = FlatStyle.Standard
        };
        LogBox = new TextBox {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.WindowText,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9),
            Dock = DockStyle.Fill
        };
        LogForm = new Form {
            Text = "WARP 代理日志",
            Size = new Size(640, 420),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText,
            Font = SystemFonts.MessageBoxFont
        };
        LogForm.Padding = new Padding(12);
        LogForm.Controls.Add(LogBox);
        LogForm.FormClosing += (s, e) => {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                LogForm.Hide();
                BtnToggleLog.Text = "显示日志";
            }
        };
        BtnToggleLog.Click += (s, e) => {
            if (LogForm.Visible) {
                LogForm.Hide();
                BtnToggleLog.Text = "显示日志";
            } else {
                LogForm.Show();
                LogForm.WindowState = FormWindowState.Normal;
                LogForm.Activate();
                BtnToggleLog.Text = "隐藏日志";
            }
        };
        Progress = new ProgressBar {
            Location = new Point(12, 80),
            Size = new Size(356, 8),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Visible = false
        };
        BtnStart = new Button {
            Text = "启动",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            Size = new Size(100, 32),
            Location = new Point(46, 112),
            FlatStyle = FlatStyle.Standard
        };
        BtnStop = new Button {
            Text = "停止",
            Size = new Size(100, 32),
            Location = new Point(154, 112),
            FlatStyle = FlatStyle.Standard,
            Enabled = false
        };
        BtnSettings = new Button {
            Text = "设置",
            Size = new Size(100, 32),
            Location = new Point(262, 112),
            FlatStyle = FlatStyle.Standard
        };
        BtnStart.Click += (s, e) => {
            if (!Started) {
                BtnStart.Enabled = false;
                BtnStop.Enabled = false;
                BtnSettings.Enabled = false;
                Progress.Visible = true;
                Progress.Style = ProgressBarStyle.Marquee;
                new Thread(Worker).Start();
            }
        };
        BtnStop.Click += (s, e) => {
            StopProxy();
        };
        BtnSettings.Click += (s, e) => {
            ShowSettingsDialog();
        };
        MainForm.Controls.AddRange(new Control[] { title, BtnToggleLog, ProxyLabel, Progress, BtnStart, BtnStop, BtnSettings });

        TrayIcon = new NotifyIcon {
            Icon = SystemIcons.Application,
            Text = "WARP 代理 - " + ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort,
            Visible = true
        };
        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (s, e) => { MainForm.Show(); MainForm.WindowState = FormWindowState.Normal; });
        trayMenu.Items.Add("退出", null, (s, e) => { Cleanup(); Application.Exit(); });
        TrayIcon.ContextMenuStrip = trayMenu;
        TrayIcon.DoubleClick += (s, e) => { MainForm.Show(); MainForm.WindowState = FormWindowState.Normal; };
        MainForm.FormClosing += (s, e) => {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                MainForm.Hide();
            }
        };

        ApplyProxyLabel();
        Log("就绪。点击「启动」开始。");
        Application.Run(MainForm);
    }

    static void ShowSettingsDialog() {
        try {
            using (SettingsForm form = new SettingsForm(Config)) {
                if (form.ShowDialog(MainForm) != DialogResult.OK) return;
                Config = form.ResultSettings;
                ProxyHost = Config.Bind;
                ProxyPort = Config.Port;
                SaveSettings();
                ApplyProxyLabel();
                TrayIcon.Text = "WARP 代理 - " + ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort;
                Log("代理设置已保存");
            }
        } catch (Exception ex) {
            Log("设置窗口出错: " + ex.Message);
        }
    }

    static string ModeName() {
        return Config.Mode == "socks" ? "SOCKS5" : "HTTP";
    }

    static void ApplyProxyLabel() {
        string scope = ProxyHost == "0.0.0.0" ? "局域网可访问" : "仅本机";
        ProxyLabel.Text = ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort + " (" + scope + ")";
    }

    static void Log(string msg) {
        if (LogBox.InvokeRequired) {
            LogBox.Invoke(new Action<string>(Log), msg);
            return;
        }
        LogBox.AppendText(DateTime.Now.ToString("[HH:mm:ss] ") + msg + Environment.NewLine);
        LogBox.SelectionStart = LogBox.TextLength;
        LogBox.ScrollToCaret();
    }

    static void Worker() {
        if (!File.Exists(UsqueExe)) { Log("错误: 未找到 usque.exe"); ShowError("未找到 usque.exe (与启动器同目录)"); return; }
        Log("环境检查通过，目录: " + BaseDir);
        string configPath = ResolvePath(Config.ConfigFile);
        if (!File.Exists(configPath)) {
            if (!File.Exists(RegBat)) {
                Log("错误: 未找到 config.json 且缺少 reg.bat");
                ShowError("未找到 config.json，且缺少 reg.bat 无法注册");
                return;
            }
            Log("首次运行，配置 WARP...");
            RunHidden(RegBat, BaseDir, true);
            Log("WARP 配置完成");
            if (!File.Exists(configPath)) {
                Log("错误: 未找到 config.json");
                ShowError("未找到 config.json，且 WARP 注册未生成配置文件");
                return;
            }
            Log("配置文件就绪: " + configPath);
        }
        KillExistingUsque();
        Log("检查并释放端口 " + ProxyPort + "...");
        FreePort(ProxyPort);
        Thread.Sleep(500);

        string args = Config.BuildArguments(false);
        Log("启动 WARP " + ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort);
        Log("命令: usque.exe " + Config.BuildArguments(true));
        Log("工作目录: " + BaseDir);
        WarpProcess = RunUsque(args, BaseDir);
        Thread.Sleep(500);
        if (WarpProcess == null || WarpProcess.HasExited) {
            Log("错误: WARP 进程已退出，退出代码: " + (WarpProcess == null ? -1 : WarpProcess.ExitCode));
            Log("标准输出: " + GetWarpOutput());
            Log("错误输出: " + GetWarpError());
            ShowError("WARP 代理启动失败，进程意外退出");
            return;
        }
        for (int i = 0; i < 15; i++) {
            Log("等待代理就绪... (" + (i + 1) + "/15)");
            Thread.Sleep(1000);
            if (CheckPort(ProxyPort)) {
                Log("WARP 代理就绪 (监听 " + ProxyHost + ":" + ProxyPort + ")");
                break;
            }
        }
        Started = true;
        RunOnUiThread(() => {
            BtnStart.Text = "运行中...";
            BtnStart.Enabled = false;
            BtnStop.Enabled = true;
            BtnSettings.Enabled = true;
            Progress.Visible = false;
        });
        new Thread(MonitorProcesses).Start();
    }

    static void StopProxy() {
        if (WarpProcess == null) return;
        Log("正在停止 WARP 代理...");
        RunOnUiThread(() => {
            BtnStop.Enabled = false;
            Progress.Visible = true;
            Progress.Style = ProgressBarStyle.Marquee;
        });
        KillExistingUsque();
        WarpProcess = null;
        Started = false;
        Log("WARP 代理已停止");
        RunOnUiThread(() => {
            BtnStart.Text = "启动";
            BtnStart.Enabled = true;
            BtnSettings.Enabled = true;
            Progress.Visible = false;
        });
    }

    static void MonitorProcesses() {
        while (true) {
            Thread.Sleep(2000);
            if (WarpProcess == null) continue;
            try {
                if (WarpProcess.HasExited) {
                    Log("警告: WARP 代理进程已退出 (退出码: " + WarpProcess.ExitCode + ")");
                    Log("标准输出: " + GetWarpOutput());
                    Log("错误输出: " + GetWarpError());
                    WarpProcess = null;
                    Started = false;
                    RunOnUiThread(() => {
                        BtnStart.Text = "启动";
                        BtnStart.Enabled = true;
                        BtnStop.Enabled = false;
                        BtnSettings.Enabled = true;
                        Progress.Visible = false;
                    });
                    return;
                }
            } catch { }
        }
    }

    static void RunOnUiThread(Action action) {
        if (MainForm.InvokeRequired) {
            MainForm.Invoke(action);
            return;
        }
        action();
    }

    static bool CheckPort(int port) {
        try {
            ProcessStartInfo psi = new ProcessStartInfo("cmd", "/c netstat -an") {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            Process process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output.Contains(":" + port);
        } catch {
            return false;
        }
    }

    static Process RunHidden(string cmd, string dir, bool wait) {
        ProcessStartInfo psi = new ProcessStartInfo("cmd", "/c " + cmd) {
            WorkingDirectory = dir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process process = Process.Start(psi);
        if (wait) process.WaitForExit();
        return process;
    }

    static Process RunUsque(string args, string dir) {
        lock (WarpLogLock) {
            WarpOutput.Clear();
            WarpError.Clear();
        }
        ProcessStartInfo psi = new ProcessStartInfo(UsqueExe, args) {
            WorkingDirectory = dir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process process = Process.Start(psi);
        if (process == null) return null;
        process.OutputDataReceived += (s, e) => {
            if (e.Data == null) return;
            lock (WarpLogLock) { WarpOutput.AppendLine(e.Data); }
            Log("[代理] " + e.Data);
        };
        process.ErrorDataReceived += (s, e) => {
            if (e.Data == null) return;
            lock (WarpLogLock) { WarpError.AppendLine(e.Data); }
            Log("[代理错误] " + e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    static string GetWarpOutput() {
        lock (WarpLogLock) { return WarpOutput.ToString(); }
    }

    static string GetWarpError() {
        lock (WarpLogLock) { return WarpError.ToString(); }
    }

    static void ShowError(string msg) {
        RunOnUiThread(() => {
            MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            BtnStart.Enabled = true;
            BtnStop.Enabled = false;
            BtnSettings.Enabled = true;
            Progress.Visible = false;
        });
    }

    static void Cleanup() {
        if (WarpProcess != null && !WarpProcess.HasExited) {
            try { WarpProcess.Kill(); } catch { }
        }
        if (TrayIcon != null) {
            TrayIcon.Visible = false;
            TrayIcon.Dispose();
        }
    }

    static void KillExistingUsque() {
        try {
            foreach (Process process in Process.GetProcessesByName("usque")) {
                Log("发现残留 usque.exe 进程 (PID: " + process.Id + ")，正在终止...");
                process.Kill();
                process.WaitForExit(3000);
                process.Dispose();
            }
        } catch (Exception ex) {
            Log("清理残留进程时出错: " + ex.Message);
        }
    }

    static void FreePort(int port) {
        try {
            ProcessStartInfo psi = new ProcessStartInfo("cmd", "/c netstat -ano | findstr \"LISTENING\" | findstr \":" + port + " \"") {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            Process process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (string.IsNullOrEmpty(output.Trim())) {
                Log("端口 " + port + " 空闲");
                return;
            }
            foreach (string line in output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)) {
                string[] parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;
                int pid;
                if (!int.TryParse(parts[parts.Length - 1], out pid) || pid < 4) continue;
                try {
                    Process owner = Process.GetProcessById(pid);
                    if (owner.ProcessName != "usque") {
                        Log("端口 " + port + " 被占用 (PID: " + pid + ", 进程: " + owner.ProcessName + ")，尝试终止...");
                        owner.Kill();
                        owner.WaitForExit(2000);
                    }
                    owner.Dispose();
                } catch { }
            }
        } catch (Exception ex) {
            Log("释放端口时出错: " + ex.Message);
        }
    }

    static string ResolvePath(string path) {
        if (string.IsNullOrEmpty(path)) return BaseDir;
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(BaseDir, path);
    }

    static void SaveSettings() {
        try {
            List<string> lines = new List<string>();
            lines.Add("Mode=" + Config.Mode);
            lines.Add("Bind=" + Config.Bind);
            lines.Add("Port=" + Config.Port);
            lines.Add("Username=" + Config.Username);
            lines.Add("Password=" + Config.Password);
            lines.Add("ConfigFile=" + Config.ConfigFile);
            lines.Add("Dns=" + Config.Dns.Replace("\r\n", "\n").Replace("\n", "\\n"));
            lines.Add("DnsTimeout=" + Config.DnsTimeout);
            lines.Add("LocalDns=" + Config.LocalDns);
            lines.Add("SystemDns=" + Config.SystemDns);
            lines.Add("SniAddress=" + Config.SniAddress);
            lines.Add("ConnectPort=" + Config.ConnectPort);
            lines.Add("UseHttp2=" + Config.UseHttp2);
            lines.Add("UseIpv6=" + Config.UseIpv6);
            lines.Add("AlwaysReconnect=" + Config.AlwaysReconnect);
            lines.Add("ReconnectDelay=" + Config.ReconnectDelay);
            lines.Add("KeepalivePeriod=" + Config.KeepalivePeriod);
            lines.Add("Mtu=" + Config.Mtu);
            lines.Add("InitialPacketSize=" + Config.InitialPacketSize);
            lines.Add("NoTunnelIpv4=" + Config.NoTunnelIpv4);
            lines.Add("NoTunnelIpv6=" + Config.NoTunnelIpv6);
            lines.Add("Insecure=" + Config.Insecure);
            lines.Add("OnConnect=" + Config.OnConnect);
            lines.Add("OnDisconnect=" + Config.OnDisconnect);
            File.WriteAllLines(ResolvePath(SettingsPath), lines.ToArray(), Encoding.UTF8);
        } catch (Exception ex) {
            Log("保存设置失败: " + ex.Message);
        }
    }

    static void LoadSettings() {
        string settingsPath = ResolvePath(SettingsPath);
        if (!File.Exists(settingsPath)) return;
        Dictionary<string, string> values = new Dictionary<string, string>();
        try {
            foreach (string line in File.ReadAllLines(settingsPath, Encoding.UTF8)) {
                int index = line.IndexOf('=');
                if (index <= 0) continue;
                values[line.Substring(0, index).Trim()] = line.Substring(index + 1);
            }
        } catch {
            return;
        }
        Config.Mode = GetValue(values, "Mode", Config.Mode);
        Config.Bind = GetValue(values, "Bind", Config.Bind);
        int port;
        if (int.TryParse(GetValue(values, "Port", ""), out port)) Config.Port = port;
        Config.Username = GetValue(values, "Username", Config.Username);
        Config.Password = GetValue(values, "Password", Config.Password);
        Config.ConfigFile = GetValue(values, "ConfigFile", Config.ConfigFile);
        Config.Dns = GetValue(values, "Dns", Config.Dns).Replace("\\n", "\r\n");
        Config.DnsTimeout = GetValue(values, "DnsTimeout", Config.DnsTimeout);
        bool flag;
        if (bool.TryParse(GetValue(values, "LocalDns", ""), out flag)) Config.LocalDns = flag;
        if (bool.TryParse(GetValue(values, "SystemDns", ""), out flag)) Config.SystemDns = flag;
        Config.SniAddress = GetValue(values, "SniAddress", Config.SniAddress);
        Config.ConnectPort = GetValue(values, "ConnectPort", Config.ConnectPort);
        if (bool.TryParse(GetValue(values, "UseHttp2", ""), out flag)) Config.UseHttp2 = flag;
        if (bool.TryParse(GetValue(values, "UseIpv6", ""), out flag)) Config.UseIpv6 = flag;
        if (bool.TryParse(GetValue(values, "AlwaysReconnect", ""), out flag)) Config.AlwaysReconnect = flag;
        Config.ReconnectDelay = GetValue(values, "ReconnectDelay", Config.ReconnectDelay);
        Config.KeepalivePeriod = GetValue(values, "KeepalivePeriod", Config.KeepalivePeriod);
        Config.Mtu = GetValue(values, "Mtu", Config.Mtu);
        Config.InitialPacketSize = GetValue(values, "InitialPacketSize", Config.InitialPacketSize);
        if (bool.TryParse(GetValue(values, "NoTunnelIpv4", ""), out flag)) Config.NoTunnelIpv4 = flag;
        if (bool.TryParse(GetValue(values, "NoTunnelIpv6", ""), out flag)) Config.NoTunnelIpv6 = flag;
        if (bool.TryParse(GetValue(values, "Insecure", ""), out flag)) Config.Insecure = flag;
        Config.OnConnect = GetValue(values, "OnConnect", Config.OnConnect);
        Config.OnDisconnect = GetValue(values, "OnDisconnect", Config.OnDisconnect);
        ProxyHost = Config.Bind;
        ProxyPort = Config.Port;
    }

    static string GetValue(Dictionary<string, string> values, string key, string fallback) {
        string value;
        return values.TryGetValue(key, out value) ? value : fallback;
    }
}
