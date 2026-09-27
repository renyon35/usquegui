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
    public bool AutoStart = true;
    public bool RunAtStartup;
    public bool TunMode;
    public string TunInterfaceName = "usque";
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
            AutoStart = AutoStart,
            RunAtStartup = RunAtStartup,
            TunMode = TunMode,
            TunInterfaceName = TunInterfaceName,
            OnConnect = OnConnect,
            OnDisconnect = OnDisconnect
        };
    }

    public string BuildArguments(bool redactPassword) {
        List<string> args = new List<string>();
        if (TunMode) {
            // nativetun 不支持 -b/-p/-u/-w/-d/-t/-l 等代理相关参数，只接受隧道参数
            args.Add("nativetun");
            AddValue(args, "-c", ConfigFile);
            AddValue(args, "-n", TunInterfaceName);
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
    private CheckBox autoStartBox;
    private CheckBox runAtStartupBox;
    private CheckBox tunModeBox;
    private TextBox tunNameBox;
    private TextBox previewBox;
    private ComboBox modeBox;

    public ProxySettings ResultSettings {
        get { return result; }
    }

    public SettingsForm(ProxySettings settings) {
        result = settings.Copy();
        Text = "设置";
        Size = new Size(640, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = SystemColors.Control;
        ForeColor = SystemColors.ControlText;
        Font = SystemFonts.MessageBoxFont;
        if (ProxyLauncher.AppIcon != null) Icon = ProxyLauncher.AppIcon;

        TabControl tabs = new TabControl {
            Location = new Point(12, 12),
            Size = new Size(600, 342)
        };
        tabs.TabPages.Add(BuildProxyTab());
        tabs.TabPages.Add(BuildDnsTab());
        tabs.TabPages.Add(BuildTunnelTab());
        tabs.TabPages.Add(BuildStartupTab());
        tabs.TabPages.Add(BuildTunTab());

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
            Location = new Point(90, 184),
            Width = 130
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
        AddNote(page, "HTTP 模式使用 usque http-proxy，SOCKS5 模式使用 usque socks。", 16, 236);
        AddNote(page, "用户名和密码需同时填写。留空表示代理不需要认证。", 16, 260);
        AddNote(page, "配置文件为相对路径，相对于启动器所在目录。", 16, 284);
        return page;
    }

    private TabPage BuildTunTab() {
        TabPage page = new TabPage("全局");
        page.AutoScroll = true;

        Label tunTitle = new Label {
            Text = "TUN 全局代理（nativetun）",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 16)
        };
        page.Controls.Add(tunTitle);
        tunModeBox = AddCheck(page, "启用全局代理，接管本机全部流量", result.TunMode, 16, 46);
        tunNameBox = AddText(page, "TUN 网卡名称 (-n)", result.TunInterfaceName, 16, 76, 200, false);

        Label reqTitle = new Label {
            Text = "使用前提",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 132)
        };
        page.Controls.Add(reqTitle);
        AddNote(page, "1. 目录内必须有 wintun.dll（从 wintun.net 下载，与本程序放在一起）。", 16, 160);
        AddNote(page, "2. 必须以管理员权限运行，否则无法创建网卡和添加路由。", 16, 184);
        AddNote(page, "3. 开启后 usque 走 nativetun 子命令，代理端口/认证/监听地址不再生效。", 16, 208);
        AddNote(page, "4. 需要 wintun.dll 才能启动；缺失时程序会提示并拒绝开启。", 16, 232);

        Label warnTitle = new Label {
            Text = "路由与风险",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 272)
        };
        page.Controls.Add(warnTitle);
        AddNote(page, "开启时会保存当前默认路由，再添加指向 TUN 的默认路由。", 16, 300);
        AddNote(page, "关闭时删除 TUN 默认路由并还原原默认路由。", 16, 324);
        AddNote(page, "程序异常退出（崩溃/断电）可能残留路由，可用「清理路由」按钮修复。", 16, 348);
        AddNote(page, "MASQUE 端点会加一条走原网关的主机路由，避免隧道自己把自己绕进去。", 16, 372);
        return page;
    }

    private TabPage BuildStartupTab() {
        TabPage page = new TabPage("启动");
        page.AutoScroll = true;

        Label autoStartTitle = new Label {
            Text = "程序启动行为",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 16)
        };
        page.Controls.Add(autoStartTitle);
        autoStartBox = AddCheck(page, "启动程序时自动开启代理", result.AutoStart, 16, 46);
        AddNote(page, "开启后，本程序一运行就自动连接 WARP 并监听代理端口，无需手动点「启动」。", 36, 70);

        Label runTitle = new Label {
            Text = "Windows 开机自启动",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 112)
        };
        page.Controls.Add(runTitle);
        runAtStartupBox = AddCheck(page, "开机时自动启动本程序", result.RunAtStartup, 16, 142);
        AddNote(page, "勾选并「保存」后立即生效，取消勾选并保存即移除。", 36, 166);

        Label infoTitle = new Label {
            Text = "实现说明",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 208)
        };
        page.Controls.Add(infoTitle);
        AddNote(page, "写入当前用户注册表项，不需要管理员权限：", 16, 236);
        AddNote(page, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", 36, 260);
        AddNote(page, "值名称：WARPProxyLauncher", 36, 284);
        AddNote(page, "值数据：proxy.exe 的完整路径（自动跟随程序位置）", 36, 308);
        AddNote(page, "登录 Windows 后自动启动。若要开机即启动（无需登录），需改用计划任务。", 16, 344);
        AddNote(page, "当前状态：" + (ProxyLauncher.IsRunAtStartupEnabled() ? "已启用" : "未启用"), 16, 380);
        AddNote(page, "也可在托盘图标上右键切换「开机自启动」。", 16, 404);
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
        autoStartBox.Checked = settings.AutoStart;
        runAtStartupBox.Checked = settings.RunAtStartup;
        tunModeBox.Checked = settings.TunMode;
        tunNameBox.Text = settings.TunInterfaceName;
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
        settings.AutoStart = autoStartBox.Checked;
        settings.RunAtStartup = runAtStartupBox.Checked;
        settings.TunMode = tunModeBox.Checked;
        settings.TunInterfaceName = string.IsNullOrEmpty(tunNameBox.Text.Trim()) ? "usque" : tunNameBox.Text.Trim();
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

    static string IconPath = Path.Combine(BaseDir, "app.ico");
    internal static Icon AppIcon = LoadAppIcon();
    static Icon TrayIconImage = null;
    static IntPtr AppIconHandle = IntPtr.Zero;

    static readonly string ExePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "WARPProxyLauncher";

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
    static CheckBox SystemProxyToggle;
    static CheckBox TunToggle;
    static Label TunHint;
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
        LoadRunAtStartup();

        MainForm = new Form {
            Text = "WARP 代理",
            Size = new Size(384, 258),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText,
            Font = SystemFonts.MessageBoxFont
        };
        if (AppIcon != null) MainForm.Icon = AppIcon;

        Label title = new Label {
            Text = "WARP 代理启动器",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            ForeColor = SystemColors.ControlText,
            AutoSize = true,
            Location = new Point(12, 14)
        };
        ProxyLabel = new Label {
            Text = ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort,
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(12, 44)
        };
        BtnStart = new Button {
            Text = "启动",
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            Size = new Size(72, 30),
            Location = new Point(12, 108),
            FlatStyle = FlatStyle.Standard
        };
        BtnStop = new Button {
            Text = "停止",
            Size = new Size(72, 30),
            Location = new Point(92, 108),
            FlatStyle = FlatStyle.Standard,
            Enabled = false
        };
        BtnToggleLog = new Button {
            Text = "日志",
            Size = new Size(72, 30),
            Location = new Point(276, 108),
            FlatStyle = FlatStyle.Standard
        };
        TunToggle = new CheckBox {
            Text = "全局代理 关",
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(150, 32),
            Location = new Point(12, 152),
            FlatStyle = FlatStyle.Standard
        };
        SystemProxyToggle = new CheckBox {
            Text = "系统代理 关",
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(150, 32),
            Location = new Point(176, 152),
            FlatStyle = FlatStyle.Standard
        };
        TunHint = new Label {
            Text = "",
            ForeColor = SystemColors.GrayText,
            AutoSize = false,
            Size = new Size(356, 18),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(12, 190)
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
        if (AppIcon != null) LogForm.Icon = AppIcon;
        LogForm.Padding = new Padding(12);
        LogForm.Controls.Add(LogBox);
        LogForm.FormClosing += (s, e) => {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                LogForm.Hide();
                BtnToggleLog.Text = "日志";
            }
        };
        BtnToggleLog.Click += (s, e) => {
            if (LogForm.Visible) {
                LogForm.Hide();
                BtnToggleLog.Text = "日志";
            } else {
                LogForm.Show();
                LogForm.WindowState = FormWindowState.Normal;
                LogForm.Activate();
                BtnToggleLog.Text = "收起";
            }
        };
        Progress = new ProgressBar {
            Location = new Point(12, 80),
            Size = new Size(356, 8),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Visible = false
        };
        BtnSettings = new Button {
            Text = "设置",
            Size = new Size(72, 30),
            Location = new Point(196, 108),
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
        SystemProxyToggle.Click += (s, e) => {
            ToggleSystemProxy(SystemProxyToggle.Checked);
        };
        TunToggle.Click += (s, e) => {
            ToggleTunMode(TunToggle.Checked);
        };
        MainForm.Controls.AddRange(new Control[] { title, ProxyLabel, Progress, BtnStart, BtnStop, BtnSettings, BtnToggleLog, TunToggle, SystemProxyToggle, TunHint });

        TrayIcon = new NotifyIcon {
            Icon = CreateTrayIcon(),
            Text = "WARP 代理 - " + ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort,
            Visible = true
        };
        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (s, e) => { MainForm.Show(); MainForm.WindowState = FormWindowState.Normal; });
        ToolStripMenuItem startupItem = new ToolStripMenuItem("开机自启动");
        startupItem.CheckOnClick = true;
        startupItem.Checked = IsRunAtStartupEnabled();
        startupItem.Click += (s, e) => {
            bool wanted = startupItem.Checked;
            bool ok = SetRunAtStartup(wanted, true);
            if (!ok) {
                startupItem.Checked = !wanted;
                Log("开机自启动设置失败，请检查注册表权限");
            }
            SaveSettings();
        };
        trayMenu.Items.Add(startupItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem sysProxyItem = new ToolStripMenuItem("系统代理");
        sysProxyItem.CheckOnClick = false;
        sysProxyItem.Click += (s, e) => {
            ToggleSystemProxy(!IsSystemProxyPointingHere());
        };
        trayMenu.Items.Add(sysProxyItem);
        ToolStripMenuItem tunItem = new ToolStripMenuItem("全局代理");
        tunItem.CheckOnClick = false;
        tunItem.Click += (s, e) => { ToggleTunMode(!Config.TunMode); };
        trayMenu.Items.Add(tunItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, (s, e) => { Cleanup(); Application.Exit(); });
        TrayIcon.ContextMenuStrip = trayMenu;
        trayMenu.Opening += (s, e) => {
            startupItem.Checked = IsRunAtStartupEnabled();
            bool on = IsSystemProxyPointingHere();
            sysProxyItem.Text = on ? "关闭系统代理" : "开启系统代理";
            sysProxyItem.Checked = on;
            tunItem.Checked = Config.TunMode;
            tunItem.Text = Config.TunMode ? "关闭全局代理" : "开启全局代理";
        };
        TrayIcon.DoubleClick += (s, e) => { MainForm.Show(); MainForm.WindowState = FormWindowState.Normal; };
        MainForm.FormClosing += (s, e) => {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                MainForm.Hide();
            }
        };

        ApplyProxyLabel();
        SyncSystemProxyToggle();
        SyncTunToggle();
        Log("就绪。点击「启动」开始。");
        MainForm.Shown += (s, e) => {
            if (Config.AutoStart) {
                Log("已启用自动启动，正在开启代理...");
                BtnStart.PerformClick();
            }
        };
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
                ApplyRunAtStartup(Config.RunAtStartup);
                UpdateSystemProxyStatus();
                Log("代理设置已保存");
            }
        } catch (Exception ex) {
            Log("设置窗口出错: " + ex.Message);
        }
    }

    static string ModeName() {
        if (Config.TunMode) return "全局(TUN)";
        return Config.Mode == "socks" ? "SOCKS5" : "HTTP";
    }

    // ==================== TUN 全局代理（nativetun） ====================

    static string WintunDll { get { return Path.Combine(BaseDir, "wintun.dll"); } }

    [System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError = true)]
    static extern int ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters,
        string lpDirectory, int nShowCmd);

    internal static bool IsAdministrator() {
        try {
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        } catch {
            return false;
        }
    }

    /// <summary>以管理员权限重新启动自身（弹 UAC），当前实例退出。</summary>
    internal static bool RelaunchElevated() {
        try {
            string exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // ShellExecute 的 runas 动词会触发 UAC 提权
            int result = ShellExecute(IntPtr.Zero, "runas", exe, "", BaseDir, 1);
            if (result <= 32) {
                LogSafe("提权启动失败，返回码: " + result + "（可能被 UAC 拒绝）");
                return false;
            }
            LogSafe("已以管理员权限重新启动");
            return true;
        } catch (Exception ex) {
            LogSafe("提权启动出错: " + ex.Message);
            return false;
        }
    }

    /// <summary>TUN 模式前置检查：返回 null 表示通过，否则返回错误说明。</summary>
    internal static string CheckTunPrerequisites() {
        if (!File.Exists(WintunDll))
            return "缺少 wintun.dll。" + Environment.NewLine +
                   "请从 https://www.wintun.net/ 下载（amd64 版），把 wintun.dll 解压到：" + Environment.NewLine + BaseDir;
        if (!IsAdministrator())
            return "需要管理员权限才能创建 TUN 网卡和修改路由。";
        return null;
    }

    /// <summary>读取当前 IPv4 默认路由（网关 + 接口索引）。</summary>
    internal static bool TryGetDefaultRoute(out string gateway, out string ifIndex) {
        gateway = null; ifIndex = null;
        try {
            ProcessStartInfo psi = new ProcessStartInfo("route", "print -4 0.0.0.0") {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            };
            Process p = Process.Start(psi);
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            // 目标路由行形如: 0.0.0.0  0.0.0.0  192.168.1.1  192.168.1.1  25
            foreach (string raw in output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)) {
                string line = raw.Trim();
                if (!line.StartsWith("0.0.0.0")) continue;
                string[] parts = line.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;
                gateway = parts[2];
                ifIndex = parts[4];
                return true;
            }
        } catch (Exception ex) {
            LogSafe("读取默认路由失败: " + ex.Message);
        }
        return false;
    }

    /// <summary>读取 TUN 网卡的接口索引与网关。</summary>
    internal static bool TryGetTunInterface(out string ifIndex, out string gateway) {
        ifIndex = null; gateway = null;
        string name = string.IsNullOrEmpty(Config.TunInterfaceName) ? "usque" : Config.TunInterfaceName;
        try {
            ProcessStartInfo psi = new ProcessStartInfo("netsh", "interface ipv4 show interfaces") {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            };
            Process p = Process.Start(psi);
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            foreach (string raw in output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)) {
                if (raw.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                string[] parts = raw.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1) continue;
                ifIndex = parts[0];
                break;
            }
            if (ifIndex == null) return false;

            psi = new ProcessStartInfo("netsh", "interface ipv4 show config name=\"" + name + "\"") {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            };
            p = Process.Start(psi);
            string cfg = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            foreach (string raw in cfg.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)) {
                string line = raw.Trim();
                int idx = line.IndexOf("Default Gateway", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string g = line.Substring(colon + 1).Trim();
                if (g.Length > 0) { gateway = g; break; }
            }
            return true;
        } catch (Exception ex) {
            LogSafe("读取 TUN 网卡信息失败: " + ex.Message);
            return false;
        }
    }

    static int RunRoute(string args) {
        try {
            ProcessStartInfo psi = new ProcessStartInfo("route", args) {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
            };
            Process p = Process.Start(psi);
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit(8000);
            return p.ExitCode;
        } catch (Exception ex) {
            LogSafe("执行 route 失败: " + ex.Message);
            return -1;
        }
    }

    /// <summary>添加指向 TUN 的默认路由，并保证端点走原网关。</summary>
    internal static bool SetupTunRoutes(string endpoint) {
        string gw, idx;
        if (!TryGetDefaultRoute(out gw, out idx)) {
            LogSafe("警告: 未能读取当前默认路由，跳过自动保存。");
        } else {
            SavedGateway = gw;
            SavedIfIndex = idx;
            LogSafe("已保存原默认路由: 网关 " + gw + " 接口 " + idx);
        }

        string tunIdx, tunGw;
        if (!TryGetTunInterface(out tunIdx, out tunGw)) {
            LogSafe("未能找到 TUN 网卡（" + Config.TunInterfaceName + "），无法添加路由。");
            return false;
        }
        LogSafe("TUN 网卡: 接口 " + tunIdx + " 网关 " + (tunGw ?? "自动"));

        // 1) 先让 MASQUE 端点走原网关，否则隧道数据自己也会被吸进隧道
        if (!string.IsNullOrEmpty(endpoint) && SavedGateway != null && SavedIfIndex != null) {
            int rc = RunRoute("add " + endpoint + " mask 255.255.255.255 " + SavedGateway + " metric 1 if " + SavedIfIndex);
            LogSafe("端点路由 " + endpoint + " → " + SavedGateway + " : " + (rc == 0 ? "成功" : "返回码 " + rc));
        }

        // 2) 默认路由指向 TUN
        string via = string.IsNullOrEmpty(tunGw) ? "0.0.0.0" : tunGw;
        int rc2 = RunRoute("add 0.0.0.0 mask 0.0.0.0 " + via + " metric 1 if " + tunIdx);
        LogSafe("默认路由 → TUN 接口 " + tunIdx + " : " + (rc2 == 0 ? "成功" : "返回码 " + rc2));
        TunRouteActive = rc2 == 0;
        return TunRouteActive;
    }

    /// <summary>删除 TUN 默认路由并还原原默认路由。</summary>
    internal static void TeardownTunRoutes() {
        string tunIdx, tunGw;
        if (TryGetTunInterface(out tunIdx, out tunGw)) {
            string via = string.IsNullOrEmpty(tunGw) ? "0.0.0.0" : tunGw;
            RunRoute("delete 0.0.0.0 mask 0.0.0.0 " + via + " if " + tunIdx);
            LogSafe("已删除 TUN 默认路由");
        }
        if (SavedGateway != null && SavedIfIndex != null) {
            int rc = RunRoute("add 0.0.0.0 mask 0.0.0.0 " + SavedGateway + " metric 1 if " + SavedIfIndex);
            LogSafe("已还原原默认路由: " + SavedGateway + " 接口 " + SavedIfIndex + " : " + (rc == 0 ? "成功" : "返回码 " + rc));
        }
        TunRouteActive = false;
    }

    static string SavedGateway;
    static string SavedIfIndex;
    internal static bool TunRouteActive;

    // ==================== 系统代理（WinINET） ====================

    const string InternetSettingsPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    const int INTERNET_OPTION_REFRESH = 37;

    internal static class SystemProxy {
        public static bool Enabled;
        public static string Server;
    }

    internal static void ReadSystemProxy() {
        SystemProxy.Enabled = false;
        SystemProxy.Server = "";
        try {
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(InternetSettingsPath, false)) {
                if (key == null) return;
                object enabled = key.GetValue("ProxyEnable");
                object server = key.GetValue("ProxyServer");
                SystemProxy.Enabled = enabled != null && Convert.ToInt32(enabled) != 0;
                SystemProxy.Server = server == null ? "" : server.ToString();
            }
        } catch { }
    }

    internal static string SystemProxyStateText() {
        ReadSystemProxy();
        if (!SystemProxy.Enabled) return "未开启";
        if (string.IsNullOrEmpty(SystemProxy.Server)) return "已开启（未指定服务器）";
        return "已开启 → " + SystemProxy.Server;
    }

    /// <summary>把系统代理指向本程序。SOCKS5 模式使用 socks= 前缀，HTTP 模式使用 host:port。</summary>
    internal static bool SetSystemProxy(string host, int port, bool socksMode, bool silent) {
        try {
            // 0.0.0.0 是监听地址而非可连接地址，系统代理必须指向回环地址
            string target = host;
            if (target == "0.0.0.0" || target == "::" || target == "*" || string.IsNullOrEmpty(target)) target = "127.0.0.1";
            string serverValue = socksMode ? "socks=" + target + ":" + port : target + ":" + port;

            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(InternetSettingsPath, true)) {
                if (key == null) {
                    if (!silent) LogSafe("设置系统代理失败: 无法打开 Internet Settings 注册表项");
                    return false;
                }
                // 只在第一次覆盖前备份原始值，便于清除时还原
                if (!BackupDone) {
                    BackupProxyEnable = key.GetValue("ProxyEnable");
                    BackupProxyServer = key.GetValue("ProxyServer");
                    BackupProxyOverride = key.GetValue("ProxyOverride");
                    BackupDone = true;
                    if (!silent) LogSafe("已记录系统代理原始设置 (ProxyEnable=" + (BackupProxyEnable ?? "无") +
                        ", ProxyServer=" + (BackupProxyServer ?? "无") + ")");
                }

                key.SetValue("ProxyEnable", 1, Microsoft.Win32.RegistryValueKind.DWord);
                key.SetValue("ProxyServer", serverValue, Microsoft.Win32.RegistryValueKind.String);
                // 本地地址不走代理，避免访问本机服务被绕进隧道
                key.SetValue("ProxyOverride", "<local>", Microsoft.Win32.RegistryValueKind.String);
            }
            NotifyProxyChanged();
            if (!silent) LogSafe("系统代理已设置为 " + serverValue);
            return true;
        } catch (Exception ex) {
            if (!silent) LogSafe("设置系统代理失败: " + ex.Message);
            return false;
        }
    }

    /// <summary>关闭系统代理。若本次运行改过设置，则还原为原始值。</summary>
    internal static bool ClearSystemProxy(bool silent) {
        try {
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(InternetSettingsPath, true)) {
                if (key == null) {
                    if (!silent) LogSafe("清除系统代理失败: 无法打开 Internet Settings 注册表项");
                    return false;
                }
                if (BackupDone) {
                    RestoreValue(key, "ProxyEnable", BackupProxyEnable);
                    RestoreValue(key, "ProxyServer", BackupProxyServer);
                    RestoreValue(key, "ProxyOverride", BackupProxyOverride);
                    BackupDone = false;
                    BackupProxyEnable = null;
                    BackupProxyServer = null;
                    BackupProxyOverride = null;
                    if (!silent) LogSafe("系统代理已清除（已还原为修改前的设置）");
                } else {
                    key.SetValue("ProxyEnable", 0, Microsoft.Win32.RegistryValueKind.DWord);
                    if (!silent) LogSafe("系统代理已清除");
                }
            }
            NotifyProxyChanged();
            return true;
        } catch (Exception ex) {
            if (!silent) LogSafe("清除系统代理失败: " + ex.Message);
            return false;
        }
    }

    static void RestoreValue(Microsoft.Win32.RegistryKey key, string name, object original) {
        if (original == null) {
            if (key.GetValue(name) != null) key.DeleteValue(name, false);
            return;
        }
        if (original is int) key.SetValue(name, original, Microsoft.Win32.RegistryValueKind.DWord);
        else key.SetValue(name, original, Microsoft.Win32.RegistryValueKind.String);
    }

    static bool BackupDone;
    static object BackupProxyEnable;
    static object BackupProxyServer;
    static object BackupProxyOverride;

    /// <summary>通知系统立即应用代理设置变更，无需重启或重新登录。</summary>
    static void NotifyProxyChanged() {
        try {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        } catch { }
    }

    /// <summary>开关驱动:打开则指向本程序，关闭则清除/还原。</summary>
    static void ToggleSystemProxy(bool enable) {
        bool socksMode = Config.Mode == "socks";
        bool ok;
        if (enable) {
            ok = SetSystemProxy(ProxyHost, ProxyPort, socksMode, false);
            if (ok) {
                Log("系统代理已开启 → " + (socksMode ? "socks=" : "") + NormalizeHost(ProxyHost) + ":" + ProxyPort);
                if (!Started) Log("提示: 代理尚未启动，请先点击「启动」，否则系统代理无法连通。");
            } else {
                MessageBox.Show("开启系统代理失败，请查看日志。", "系统代理", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        } else {
            ok = ClearSystemProxy(false);
            if (ok) Log("系统代理已关闭");
            else MessageBox.Show("关闭系统代理失败，请查看日志。", "系统代理", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        SyncSystemProxyToggle();
    }

    /// <summary>TUN 全局代理开关。开启即切到 nativetun 模式并接管全部流量。</summary>
    static void ToggleTunMode(bool enable) {
        if (enable) {
            string problem = CheckTunPrerequisites();
            if (problem != null) {
                MessageBox.Show(problem, "全局代理", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogSafe("全局代理未开启: " + problem.Replace("\r\n", " "));
                SyncTunToggle();
                return;
            }
            if (!Config.TunMode) {
                Config.TunMode = true;
                SaveSettings();
                LogSafe("已切换到 TUN 全局代理模式");
            }
            ApplyProxyLabel();
            // 切到 TUN 后原有的 HTTP/SOCKS 代理端口不再监听，系统代理指向它会断网
            if (IsSystemProxyPointingHere()) {
                ClearSystemProxy(true);
                LogSafe("切换到全局代理，已自动关闭系统代理");
            }
            if (Started) {
                MessageBox.Show("模式已切换，请先「停止」再重新「启动」以生效。", "全局代理",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                LogSafe("已启用全局代理，点击「启动」开始接管全部流量");
            }
        } else {
            if (Config.TunMode) {
                Config.TunMode = false;
                SaveSettings();
                LogSafe("已关闭 TUN 全局代理模式");
            }
            if (Started) {
                LogSafe("正在停止全局代理并还原路由...");
                StopProxy();
            } else if (TunRouteActive) {
                TeardownTunRoutes();
            }
            ApplyProxyLabel();
        }
        SyncTunToggle();
        SyncSystemProxyToggle();
    }

    /// <summary>同步全局代理开关与提示文字。</summary>
    static void SyncTunToggle() {
        if (MainForm == null || MainForm.IsDisposed) return;
        if (MainForm.InvokeRequired) {
            try { MainForm.Invoke(new Action(SyncTunToggle)); } catch { }
            return;
        }
        bool on = Config.TunMode;
        if (TunToggle != null && !TunToggle.IsDisposed) {
            TunToggle.Checked = on;
            TunToggle.Text = on ? "全局代理 开" : "全局代理 关";
        }
        if (TunHint == null || TunHint.IsDisposed) return;
        if (on) {
            if (!File.Exists(WintunDll)) TunHint.Text = "缺少 wintun.dll，无法创建 TUN 网卡";
            else if (!IsAdministrator()) TunHint.Text = "已启用，但需管理员权限运行才能生效";
            else TunHint.Text = Started ? "已接管全部流量" : "已启用，点击「启动」生效";
            TunHint.ForeColor = (File.Exists(WintunDll) && IsAdministrator()) ? SystemColors.ControlText : Color.Firebrick;
        } else {
            TunHint.Text = "";
        }
    }

    /// <summary>让开关反映系统代理的真实状态（以注册表为准）。</summary>
    static void SyncSystemProxyToggle() {
        if (MainForm == null || MainForm.IsDisposed) return;
        if (MainForm.InvokeRequired) {
            try { MainForm.Invoke(new Action(SyncSystemProxyToggle)); } catch { }
            return;
        }
        ReadSystemProxy();
        if (SystemProxyToggle == null || SystemProxyToggle.IsDisposed) return;
        SystemProxyToggle.Checked = IsSystemProxyPointingHere();
        SystemProxyToggle.Text = SystemProxyToggle.Checked ? "系统代理 开" : "系统代理 关";
    }

    /// <summary>判断当前系统代理是否指向本程序监听的地址与端口。</summary>
    internal static bool IsSystemProxyPointingHere() {
        ReadSystemProxy();
        if (!SystemProxy.Enabled || string.IsNullOrEmpty(SystemProxy.Server)) return false;
        string s = SystemProxy.Server;
        if (s.StartsWith("socks=", StringComparison.OrdinalIgnoreCase)) s = s.Substring(6);
        // 形如 host:port，可能带 http= 等协议前缀
        int idx = s.LastIndexOf(':');
        if (idx < 0) return false;
        string host = s.Substring(0, idx).Trim().ToLowerInvariant();
        string portText = s.Substring(idx + 1).Trim();
        int port;
        if (!int.TryParse(portText, out port) || port != ProxyPort) return false;
        if (host.Contains("=")) host = host.Substring(host.LastIndexOf('=') + 1);
        if (host.StartsWith("[")) host = host.Trim('[', ']');
        return host == "127.0.0.1" || host == "localhost" || host == "::1" || host == NormalizeHost(ProxyHost).ToLowerInvariant();
    }

    static string NormalizeHost(string host) {
        if (host == "0.0.0.0" || host == "::" || host == "*" || string.IsNullOrEmpty(host)) return "127.0.0.1";
        return host;
    }

    static void UpdateSystemProxyStatus() {
        SyncSystemProxyToggle();
    }

    static object ReadRunValue() {
        try {
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, false)) {
                if (key == null) return null;
                return key.GetValue(RunValueName);
            }
        } catch {
            return null;
        }
    }

    internal static bool IsRunAtStartupEnabled() {
        return ReadRunValue() != null;
    }

    static void LoadRunAtStartup() {
        Config.RunAtStartup = IsRunAtStartupEnabled();
    }

    static void ApplyRunAtStartup(bool enabled) {
        if (enabled == IsRunAtStartupEnabled()) return;
        SetRunAtStartup(enabled, false);
    }

    internal static bool SetRunAtStartup(bool enabled, bool silent) {
        try {
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, true)) {
                if (key == null) {
                    if (!silent) LogSafe("开机自启动设置失败: 无法打开注册表 Run 项");
                    return false;
                }
                if (enabled) {
                    key.SetValue(RunValueName, "\"" + ExePath + "\"");
                    if (!silent) LogSafe("已启用开机自启动: " + ExePath);
                } else {
                    if (key.GetValue(RunValueName) != null) key.DeleteValue(RunValueName, false);
                    if (!silent) LogSafe("已关闭开机自启动");
                }
                Config.RunAtStartup = enabled;
                return true;
            }
        } catch (Exception ex) {
            if (!silent) LogSafe("开机自启动设置失败: " + ex.Message);
            return false;
        }
    }

    internal static Icon LoadAppIcon() {
        try {
            if (!string.IsNullOrEmpty(IconPath) && File.Exists(IconPath)) return new Icon(IconPath);
        } catch (Exception ex) {
            Console.WriteLine("加载图标失败: " + ex.Message);
        }
        try {
            IntPtr handle = LoadImage(IntPtr.Zero, "#32512", IMAGE_ICON, 0, 0, LR_DEFAULTSIZE | LR_SHARED);
            if (handle != IntPtr.Zero) {
                AppIconHandle = handle;
                return Icon.FromHandle(handle);
            }
        } catch { }
        return SystemIcons.Application;
    }

    const int IMAGE_ICON = 1;
    const int LR_DEFAULTSIZE = 0x00000040;
    const int LR_SHARED = 0x00008000;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    static extern IntPtr LoadImage(IntPtr hinst, string lpszName, int uType, int cxDesired, int cyDesired, int fuLoad);

    static Icon CreateTrayIcon() {
        if (AppIcon == null) return SystemIcons.Application;
        try {
            TrayIconImage = new Icon(AppIcon, SystemInformation.SmallIconSize);
            return TrayIconImage;
        } catch {
            return AppIcon;
        }
    }

    static void ApplyProxyLabel() {
        if (Config.TunMode) {
            string ifName = string.IsNullOrEmpty(Config.TunInterfaceName) ? "usque" : Config.TunInterfaceName;
            string state = File.Exists(WintunDll) ? (IsAdministrator() ? "" : " (需管理员权限)") : " (缺少 wintun.dll)";
            ProxyLabel.Text = "全局代理: TUN 网卡 " + ifName + "，接管全部流量" + state;
            return;
        }
        string scope = ProxyHost == "0.0.0.0" ? "局域网可访问" : "仅本机";
        ProxyLabel.Text = ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort + " (" + scope + ")";
    }

    static void Log(string msg) {
        if (LogBox == null || LogBox.IsDisposed) return;
        if (LogBox.InvokeRequired) {
            LogBox.Invoke(new Action<string>(Log), msg);
            return;
        }
        LogBox.AppendText(DateTime.Now.ToString("[HH:mm:ss] ") + msg + Environment.NewLine);
        LogBox.SelectionStart = LogBox.TextLength;
        LogBox.ScrollToCaret();
    }

    static void LogSafe(string msg) {
        try {
            Log(msg);
        } catch { }
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
        if (Config.TunMode) {
            StartTunTunnel();
            return;
        }
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

    /// <summary>TUN 全局代理启动流程：检查前置条件 → 启动 nativetun → 配置路由。</summary>
    static void StartTunTunnel() {
        string problem = CheckTunPrerequisites();
        if (problem != null) {
            Log("错误: " + problem.Replace("\r\n", " "));
            ShowError(problem);
            return;
        }
        Log("以管理员权限运行，wintun.dll 已就绪");

        string args = Config.BuildArguments(false);
        Log("启动 WARP 全局代理 (nativetun)，网卡: " + Config.TunInterfaceName);
        Log("命令: usque.exe " + Config.BuildArguments(true));
        WarpProcess = RunUsque(args, BaseDir);
        Thread.Sleep(500);
        if (WarpProcess == null || WarpProcess.HasExited) {
            Log("错误: nativetun 进程已退出，退出代码: " + (WarpProcess == null ? -1 : WarpProcess.ExitCode));
            Log("标准输出: " + GetWarpOutput());
            Log("错误输出: " + GetWarpError());
            ShowError("TUN 全局代理启动失败，进程意外退出。请查看日志确认 wintun.dll 是否匹配。");
            return;
        }

        Log("等待 TUN 网卡就绪...");
        bool ready = false;
        for (int i = 0; i < 20; i++) {
            Thread.Sleep(1000);
            string idx, gw;
            if (TryGetTunInterface(out idx, out gw)) {
                Log("TUN 网卡已就绪: 接口 " + idx + (string.IsNullOrEmpty(gw) ? "" : " 网关 " + gw));
                ready = true;
                break;
            }
        }
        if (!ready) {
            Log("警告: 未检测到 TUN 网卡，可能创建失败，跳过路由配置");
            ShowError("TUN 网卡未出现。请确认 wintun.dll 版本与系统架构匹配（通常为 amd64），并且已用管理员权限运行。");
            return;
        }

        string endpoint = ReadEndpointFromConfig();
        if (string.IsNullOrEmpty(endpoint)) Log("提示: 未能从 config.json 读取端点地址，跳过端点直连路由");
        else Log("MASQUE 端点: " + endpoint);

        if (SetupTunRoutes(endpoint)) {
            Log("全局代理已生效：所有流量经 TUN 网卡走 WARP");
        } else {
            Log("警告: 默认路由未成功添加，全局代理可能未真正生效");
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

    /// <summary>从 config.json 读取 MASQUE 端点地址，用于添加直连路由。</summary>
    static string ReadEndpointFromConfig() {
        try {
            string path = ResolvePath(Config.ConfigFile);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path, Encoding.UTF8);
            string key = Config.UseIpv6 ? "endpoint_v6" : "endpoint_v4";
            if (Config.UseHttp2) key = Config.UseIpv6 ? "endpoint_h2_v6" : "endpoint_h2_v4";
            int idx = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return null;
            int firstQuote = json.IndexOf('"', colon + 1);
            if (firstQuote < 0) return null;
            int secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0) return null;
            string value = json.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();
            return value.Length == 0 ? null : value;
        } catch (Exception ex) {
            LogSafe("读取端点地址失败: " + ex.Message);
            return null;
        }
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
        // TUN 模式必须先撤路由再退出，否则会断网
        if (Config.TunMode || TunRouteActive) {
            Log("正在还原路由...");
            TeardownTunRoutes();
        }
        // 代理已停,若系统代理仍指向本程序会导致浏览器无法上网,自动清除
        if (IsSystemProxyPointingHere()) {
            ClearSystemProxy(true);
            Log("检测到系统代理指向本程序且代理已停止，已自动清除系统代理");
            RunOnUiThread(UpdateSystemProxyStatus);
        }
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
                    // TUN 进程没了但默认路由还指着它，会直接断网，必须立即撤掉
                    if (Config.TunMode || TunRouteActive) {
                        Log("检测到 TUN 进程退出，正在还原路由以免断网...");
                        TeardownTunRoutes();
                    }
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
        // 退出前清除指向本程序的系统代理，否则代理一停浏览器就上不了网
        if (IsSystemProxyPointingHere()) {
            ClearSystemProxy(true);
            LogSafe("退出前已自动清除指向本程序的系统代理");
        }
        // TUN 模式退出前必须撤掉默认路由，否则整机断网
        if (Config.TunMode || TunRouteActive) {
            LogSafe("退出前正在还原 TUN 路由...");
            TeardownTunRoutes();
        }
        if (TrayIcon != null) {
            TrayIcon.Visible = false;
            TrayIcon.Dispose();
        }
        if (TrayIconImage != null) {
            TrayIconImage.Dispose();
            TrayIconImage = null;
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
            lines.Add("AutoStart=" + Config.AutoStart);
            lines.Add("RunAtStartup=" + Config.RunAtStartup);
            lines.Add("TunMode=" + Config.TunMode);
            lines.Add("TunInterfaceName=" + Config.TunInterfaceName);
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
        if (bool.TryParse(GetValue(values, "AutoStart", ""), out flag)) Config.AutoStart = flag;
        if (bool.TryParse(GetValue(values, "RunAtStartup", ""), out flag)) Config.RunAtStartup = flag;
        if (bool.TryParse(GetValue(values, "TunMode", ""), out flag)) Config.TunMode = flag;
        Config.TunInterfaceName = GetValue(values, "TunInterfaceName", Config.TunInterfaceName);
        Config.OnConnect = GetValue(values, "OnConnect", Config.OnConnect);
        Config.OnDisconnect = GetValue(values, "OnDisconnect", Config.OnDisconnect);
        ProxyHost = Config.Bind;
        ProxyPort = Config.Port;
        LoadRunAtStartup();
    }

    static string GetValue(Dictionary<string, string> values, string key, string fallback) {
        string value;
        return values.TryGetValue(key, out value) ? value : fallback;
    }
}
