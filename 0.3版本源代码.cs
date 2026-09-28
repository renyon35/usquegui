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

    /// <summary>返回实际使用的 TUN 网卡名（空则用默认 usque）。</summary>
    public string InterfaceName() {
        return string.IsNullOrEmpty(TunInterfaceName) ? "usque" : TunInterfaceName.Trim();
    }

    public string BuildArguments(bool redactPassword) {        List<string> args = new List<string>();
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
    static Label TrafficLabel;
    static Button BtnTrafficDetail;
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
            Size = new Size(384, 318),
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
            Size = new Size(112, 32),
            Location = new Point(12, 106),
            FlatStyle = FlatStyle.Standard
        };
        BtnStop = new Button {
            Text = "停止",
            Size = new Size(112, 32),
            Location = new Point(132, 106),
            FlatStyle = FlatStyle.Standard,
            Enabled = false
        };
        BtnSettings = new Button {
            Text = "设置",
            Size = new Size(112, 32),
            Location = new Point(252, 106),
            FlatStyle = FlatStyle.Standard
        };
        // 开关行
        TunToggle = new CheckBox {
            Text = "全局代理 关",
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(174, 32),
            Location = new Point(12, 148),
            FlatStyle = FlatStyle.Standard
        };
        SystemProxyToggle = new CheckBox {
            Text = "系统代理 关",
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(174, 32),
            Location = new Point(194, 148),
            FlatStyle = FlatStyle.Standard
        };
        // 工具行
        BtnTestConn = new Button {
            Text = "测试连接",
            Size = new Size(112, 28),
            Location = new Point(12, 190),
            FlatStyle = FlatStyle.Standard
        };
        BtnToggleLog = new Button {
            Text = "日志",
            Size = new Size(112, 28),
            Location = new Point(132, 190),
            FlatStyle = FlatStyle.Standard
        };
        BtnRestoreNet = new Button {
            Text = "恢复网络",
            Size = new Size(112, 28),
            Location = new Point(252, 190),
            FlatStyle = FlatStyle.Standard
        };
        // 提示行 + 流量行
        TunHint = new Label {
            Text = "",
            ForeColor = SystemColors.GrayText,
            AutoSize = false,
            Size = new Size(356, 16),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(12, 226)
        };
        TrafficLabel = new Label {
            Text = "流量: 未运行",
            ForeColor = SystemColors.GrayText,
            AutoSize = false,
            Size = new Size(268, 20),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(12, 246),
            Cursor = Cursors.Hand
        };
        TrafficLabel.Click += (s, e) => { ShowTrafficDetail(); };
        BtnTrafficDetail = new Button {
            Text = "详情",
            Size = new Size(76, 24),
            Location = new Point(292, 244),
            FlatStyle = FlatStyle.Standard
        };
        BtnTrafficDetail.Click += (s, e) => { ShowTrafficDetail(); };
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
        BtnTestConn.Click += (s, e) => {
            StartConnectivityTest();
        };
        BtnRestoreNet.Click += (s, e) => {
            if (MessageBox.Show("将删除所有指向 TUN 网卡的默认路由、还原原默认路由并停止 usque 进程。" + Environment.NewLine +
                "用于全局代理导致断网时恢复联网。继续？", "一键恢复网络",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            new Thread(() => { EmergencyRestore(); }).Start();
        };
        MainForm.Controls.AddRange(new Control[] { title, ProxyLabel, Progress, BtnStart, BtnStop, BtnSettings, TunToggle, SystemProxyToggle, BtnTestConn, BtnToggleLog, BtnRestoreNet, TunHint, TrafficLabel, BtnTrafficDetail });

        TrayIcon = new NotifyIcon {
            Icon = CreateTrayIcon(),
            Text = "WARP 代理 - " + ModeName() + " 代理: " + ProxyHost + ":" + ProxyPort,
            Visible = true
        };
        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (s, e) => { MainForm.Show(); MainForm.WindowState = FormWindowState.Normal; });
        trayMenu.Items.Add("测试连接", null, (s, e) => { StartConnectivityTest(); });
        trayMenu.Items.Add("一键恢复网络", null, (s, e) => {
            if (MessageBox.Show("删除 TUN 默认路由、还原原路由并停止 usque，用于断网时恢复。继续？",
                "一键恢复网络", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            new Thread(() => { EmergencyRestore(); }).Start();
        });
        trayMenu.Items.Add(new ToolStripSeparator());
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
        string name = Config.InterfaceName();
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

        // 1) 先让 MASQUE 端点走原网关，否则隧道数据自己也会被吸进隧道（形成死循环）
        if (!string.IsNullOrEmpty(endpoint)) {
            if (SavedGateway == null || SavedIfIndex == null) {
                LogSafe("错误: 未能获取原默认路由，无法为 MASQUE 端点添加直连路由");
                LogSafe("已放弃切换默认路由，避免造成隧道自循环断网");
                return false;
            }
            int rc = RunRoute("add " + endpoint + " mask 255.255.255.255 " + SavedGateway + " metric 1 if " + SavedIfIndex);
            LogSafe("端点直连路由 " + endpoint + " → " + SavedGateway + " (接口 " + SavedIfIndex + ") : " + (rc == 0 ? "成功" : "返回码 " + rc));
            if (rc != 0) {
                LogSafe("错误: 端点直连路由添加失败。若继续切换默认路由，隧道流量会绕回隧道导致断网");
                LogSafe("已放弃切换默认路由");
                return false;
            }
        } else {
            LogSafe("警告: 未获取到端点地址，跳过端点直连路由（存在自循环风险）");
        }

        // 2) 默认路由指向 TUN
        string via = string.IsNullOrEmpty(tunGw) ? "0.0.0.0" : tunGw;
        int rc2 = RunRoute("add 0.0.0.0 mask 0.0.0.0 " + via + " metric 1 if " + tunIdx);
        LogSafe("默认路由 → TUN 接口 " + tunIdx + " 网关 " + via + " : " + (rc2 == 0 ? "成功" : "返回码 " + rc2));
        TunRouteActive = rc2 == 0;
        return TunRouteActive;
    }

    /// <summary>清理为 MASQUE 端点添加的直连路由。</summary>
    internal static void CleanupEndpointRoute(string endpoint) {
        if (string.IsNullOrEmpty(endpoint)) return;
        int rc = RunRoute("delete " + endpoint + " mask 255.255.255.255");
        LogSafe("清理端点路由 " + endpoint + " : " + (rc == 0 ? "成功" : "返回码 " + rc));
    }

    /// <summary>删除 TUN 默认路由并还原原默认路由。</summary>
    internal static void TeardownTunRoutes() {
        string tunIdx, tunGw;
        if (TryGetTunInterface(out tunIdx, out tunGw)) {
            string via = string.IsNullOrEmpty(tunGw) ? "0.0.0.0" : tunGw;
            RunRoute("delete 0.0.0.0 mask 0.0.0.0 " + via + " if " + tunIdx);
            LogSafe("已删除 TUN 默认路由");
        }
        // 端点直连路由也要清掉，否则会残留影响后续正常上网
        CleanupEndpointRoute(ReadEndpointFromConfig());
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
        if (!int.TryParse(portText, out port) || port != Config.Port) return false;
        if (host.Contains("=")) host = host.Substring(host.LastIndexOf('=') + 1);
        if (host.StartsWith("[")) host = host.Trim('[', ']');
        return host == "127.0.0.1" || host == "localhost" || host == "::1" || host == NormalizeHost(Config.Bind).ToLowerInvariant();
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
            string ifName = Config.InterfaceName();
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
            if (IsLocalPortOpen(ProxyPort)) {
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
        ResetTrafficBase();
        new Thread(TrafficMonitor).Start();
    }

    /// <summary>TUN 全局代理启动流程：检查前置条件 → 启动 nativetun → 等隧道真正连通 → 配置路由。</summary>
    /// <remarks>
    /// 关键点：网卡出现 ≠ 隧道可用。usque 创建网卡后还要经历 idle→重连→连接 MASQUE 服务器，
    /// 在这段空窗期把默认路由切到 TUN 会造成流量黑洞（断网）。因此必须等到日志出现连接成功标志才加路由。
    /// </remarks>
    static void StartTunTunnel() {
        string problem = CheckTunPrerequisites();
        if (problem != null) {
            Log("错误: " + problem.Replace("\r\n", " "));
            ShowError(problem);
            return;
        }
        Log("以管理员权限运行，wintun.dll 已就绪");

        string args = Config.BuildArguments(false);
        Log("启动 WARP 全局代理 (nativetun)，网卡: " + Config.InterfaceName());
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

        // 阶段一：等网卡创建
        Log("等待 TUN 网卡创建...");
        bool nicReady = false;
        for (int i = 0; i < 20; i++) {
            Thread.Sleep(500);
            string idx, gw;
            if (TryGetTunInterface(out idx, out gw)) {
                Log("TUN 网卡已创建: 接口 " + idx + (string.IsNullOrEmpty(gw) ? "" : " 网关 " + gw));
                nicReady = true;
                break;
            }
            if (WarpProcess.HasExited) break;
        }
        if (!nicReady) {
            Log("错误: 未检测到 TUN 网卡，可能创建失败");
            Log("错误输出: " + GetWarpError());
            ShowError("TUN 网卡未出现。请确认 wintun.dll 版本与系统架构匹配（通常为 amd64），并且已用管理员权限运行。");
            KillExistingUsque();
            return;
        }

        // 阶段二：等隧道真正连上 MASQUE 服务器（不能只看网卡是否出现）
        Log("等待隧道连接 MASQUE 服务器...");
        bool tunnelUp = false;
        for (int i = 0; i < 25; i++) {
            Thread.Sleep(1000);
            if (WarpProcess.HasExited) break;
            if (WarpOutputContains("Connected to MASQUE server")) { Log("检测到隧道已连接 MASQUE 服务器"); tunnelUp = true; break; }
            if (WarpOutputContains("Tunnel established")) { Log("检测到隧道已建立"); tunnelUp = true; break; }
        }

        if (!tunnelUp) {
            if (WarpProcess != null && WarpProcess.HasExited) {
                Log("错误: 隧道进程在连接建立前退出");
            } else {
                Log("错误: 等待隧道连通超时（30 秒）");
            }
            Log("标准输出: " + GetWarpOutput());
            Log("错误输出: " + GetWarpError());
            // 未连通就不动路由，直接停掉，避免把用户网络切进黑洞
            KillExistingUsque();
            WarpProcess = null;
            ShowError("隧道未能成功连接 MASQUE 服务器，已放弃配置全局路由（未改动你的网络设置）。" + Environment.NewLine +
                      "请查看日志中的错误输出，常见原因是网络不通或端点被阻断。");
            return;
        }
        Log("隧道已连通，准备配置全局路由");

        string endpoint = ReadEndpointFromConfig();
        if (string.IsNullOrEmpty(endpoint)) Log("提示: 未能从 config.json 读取端点地址，跳过端点直连路由");
        else Log("MASQUE 端点: " + endpoint);

        // 阶段三：先给端点加直连路由，再切默认路由
        if (SetupTunRoutes(endpoint)) {
            Log("全局代理已生效：所有流量经 TUN 网卡走 WARP");
            // 加完路由做一次联通自检。注意：自检失败不一定代表隧道有问题
            // （本机网络可能本来就访问不了探测点），所以只提示、不自动回滚，避免误伤。
            if (VerifyTunnelConnectivity()) {
                Log("联网自检通过，全局代理工作正常");
            } else {
                Log("警告: 联网自检未通过，但已保留全局路由。");
                Log("提示: 若确实无法上网，请点击「恢复网络」按钮撤销全局代理。");
                RunOnUiThread(() => MessageBox.Show(
                    "全局代理已开启，但联网自检未通过。" + Environment.NewLine + Environment.NewLine +
                    "请打开浏览器试一下能否访问国外网站。" + Environment.NewLine +
                    "如果无法上网，请点击主界面的「恢复网络」按钮立即还原。",
                    "全局代理", MessageBoxButtons.OK, MessageBoxIcon.Warning));
            }
        } else {
            Log("警告: 默认路由未成功添加，全局代理未生效，正在清理已添加的端点路由...");
            CleanupEndpointRoute(endpoint);
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
        ResetTrafficBase();
        new Thread(TrafficMonitor).Start();
    }

    /// <summary>
    /// 一键恢复网络：删除所有指向 TUN 网卡的默认路由、还原保存的原默认路由、停掉 usque。
    /// 用于全局代理异常导致断网时的手动救援，不做任何前置条件判断。
    /// </summary>
    internal static string EmergencyRestoreNetwork() {
        StringBuilder report = new StringBuilder();
        try {
            // 1) 删掉所有指向 TUN 网卡的默认路由（不依赖内存里保存的网关）
            string name = Config.InterfaceName();
            string idx, gw;
            if (TryGetTunInterface(out idx, out gw)) {
                int rc = RunRoute("delete 0.0.0.0 mask 0.0.0.0 if " + idx);
                report.AppendLine("删除 TUN 默认路由 (接口 " + idx + "): " + (rc == 0 ? "成功" : "返回码 " + rc));
            } else {
                report.AppendLine("未找到 TUN 网卡，跳过");
            }

            // 2) 还原保存的原默认路由
            if (SavedGateway != null && SavedIfIndex != null) {
                int rc = RunRoute("add 0.0.0.0 mask 0.0.0.0 " + SavedGateway + " metric 1 if " + SavedIfIndex);
                report.AppendLine("还原原默认路由 " + SavedGateway + " 接口 " + SavedIfIndex + ": " + (rc == 0 ? "成功" : "返回码 " + rc));
            } else {
                report.AppendLine("无已保存的原默认路由（本程序未改过路由）");
            }
            SavedGateway = null; SavedIfIndex = null; TunRouteActive = false;

            // 3) 停掉 usque
            int killed = 0;
            foreach (Process p in Process.GetProcessesByName("usque")) {
                try { p.Kill(); p.WaitForExit(3000); killed++; } catch { }
                p.Dispose();
            }
            report.AppendLine("已结束 usque 进程: " + killed + " 个");

            // 4) 清掉系统代理（若指向本程序）
            if (IsSystemProxyPointingHere()) {
                ClearSystemProxy(true);
                report.AppendLine("已清除指向本程序的系统代理");
            }
            WarpProcess = null;
            Started = false;
            report.AppendLine("当前默认路由:");
            report.Append(DescribeDefaultRoutes());
        } catch (Exception ex) {
            report.AppendLine("恢复过程出错: " + ex.Message);
        }
        return report.ToString();
    }

    /// <summary>列出当前 IPv4 默认路由，便于用户确认网络状态。</summary>
    internal static string DescribeDefaultRoutes() {
        StringBuilder sb = new StringBuilder();
        try {
            ProcessStartInfo psi = new ProcessStartInfo("route", "print -4 0.0.0.0") {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            };
            Process p = Process.Start(psi);
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            foreach (string raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                string line = raw.Trim();
                if (!line.StartsWith("0.0.0.0")) continue;
                sb.AppendLine("  " + line);
            }
        } catch { }
        if (sb.Length == 0) sb.AppendLine("  (未读取到默认路由)");
        return sb.ToString();
    }

    static void EmergencyRestore() {
        Log("正在执行一键恢复网络...");
        string report = EmergencyRestoreNetwork();
        foreach (string line in report.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) Log("  " + line);
        Log("恢复完成");
        SyncTunToggle();
        SyncSystemProxyToggle();
        RunOnUiThread(() => {
            BtnStart.Text = "启动";
            BtnStart.Enabled = true;
            BtnStop.Enabled = false;
            BtnSettings.Enabled = true;
            Progress.Visible = false;
        });
        MessageBox.Show("网络已恢复。" + Environment.NewLine + Environment.NewLine + report,
            "一键恢复网络", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>检查 usque 输出中是否包含指定文本。usque 日志实际走 stderr，两边都要查。</summary>
    static bool WarpOutputContains(string text) {
        lock (WarpLogLock) {
            if (WarpOutput.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return WarpError.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    /// <summary>
    /// 全局路由生效后自检联网是否正常。失败说明路由配置有问题，需要回滚。
    /// 探测点选国内可达地址：本机所在网络可能本来就无法直连国外，用国外地址会误判为失败。
    /// </summary>
    static bool VerifyTunnelConnectivity() {
        string[] probes = { "223.5.5.5", "119.29.29.29", "180.76.76.76" };
        for (int attempt = 1; attempt <= 3; attempt++) {
            foreach (string probe in probes) {
                if (TestDirect(new PingTarget("自检", probe)).Ok) {
                    Log("联网自检通过 (" + probe + ")");
                    return true;
                }
            }
            Log("联网自检未通过 (" + attempt + "/3)，重试...");
            Thread.Sleep(1500);
        }
        return false;
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

    // ==================== 连通性测试 ====================

    internal class PingTarget {
        public string Name;
        public string Host;
        public PingTarget(string name, string host) { Name = name; Host = host; }
    }

    internal class PingResult {
        public string Name;
        public string Host;
        public bool Ok;
        public long Ms = -1;
        public string Detail = "";
    }

    static readonly PingTarget[] TestTargets = new PingTarget[] {
        new PingTarget("X",      "x.com"),
        new PingTarget("Google", "www.google.com"),
        new PingTarget("GitHub", "github.com")
    };

    static Button BtnTestConn;
    static Button BtnRestoreNet;
    static bool Testing;

    /// <summary>按当前实际生效的方式执行连通性测试。</summary>
    internal static List<PingResult> RunConnectivityTest() {
        List<PingResult> results = new List<PingResult>();
        bool proxyListening = IsLocalPortOpen(Config.Port);
        bool tunActive = Config.TunMode && Started;
        bool useProxy = !tunActive && !IsSystemProxyPointingHere() && proxyListening && Started;
        if (useProxy) {
            RunOnUiThread(() => Log("测试方式: 通过本地 " + ModeName() + " 代理 " + NormalizeHost(Config.Bind) + ":" + Config.Port));
        } else if (tunActive) {
            RunOnUiThread(() => Log("测试方式: 全局代理已接管，直接连接"));
        } else if (IsSystemProxyPointingHere()) {
            RunOnUiThread(() => Log("测试方式: 系统代理已开启，直接连接（系统会自动走代理）"));
        } else {
            if (!Started) RunOnUiThread(() => Log("提示: 代理未启动，将测试本机直连的连通性。"));
            else if (!proxyListening) RunOnUiThread(() => Log("提示: 端口 " + Config.Port + " 未监听，将测试本机直连。"));
            else RunOnUiThread(() => Log("测试方式: 本机直连"));
        }

        foreach (PingTarget target in TestTargets) {
            PingResult r = new PingResult { Name = target.Name, Host = target.Host };
            RunOnUiThread(() => Log("正在测试 " + target.Name + " (" + target.Host + ") ..."));
            if (useProxy) r = TestViaProxy(target);
            else r = TestDirect(target);
            results.Add(r);
            PingResult captured = r;
            if (r.Ok) RunOnUiThread(() => Log("  ✓ " + captured.Name + " 连通，耗时 " + captured.Ms + " ms" + captured.Detail));
            else RunOnUiThread(() => Log("  ✗ " + captured.Name + " 失败：" + captured.Detail));
        }
        return results;
    }

    /// <summary>直连测试：TCP 握手到 443，耗时即延迟。</summary>
    static PingResult TestDirect(PingTarget target) {
        PingResult r = new PingResult { Name = target.Name, Host = target.Host };
        Stopwatch sw = Stopwatch.StartNew();
        try {
            System.Net.IPAddress[] addrs = System.Net.Dns.GetHostAddresses(target.Host);
            if (addrs == null || addrs.Length == 0) { r.Detail = "DNS 解析无结果"; return r; }
            System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
            try {
                IAsyncResult ar = client.BeginConnect(addrs[0], 443, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(5000)) { r.Detail = "连接超时(5s)"; return r; }
                client.EndConnect(ar);
                sw.Stop();
                r.Ok = true;
                r.Ms = sw.ElapsedMilliseconds;
            } finally {
                try { client.Close(); } catch { }
            }
        } catch (Exception ex) {
            r.Detail = ex.Message;
        }
        return r;
    }

    /// <summary>通过本地 HTTP/SOCKS 代理测试。HTTP 用 CONNECT 隧道，SOCKS5 走标准握手。</summary>
    static PingResult TestViaProxy(PingTarget target) {
        PingResult r = new PingResult { Name = target.Name, Host = target.Host };
        Stopwatch sw = Stopwatch.StartNew();
        System.Net.Sockets.TcpClient client = null;
        try {
            client = new System.Net.Sockets.TcpClient();
            // 统一以 Config 为准，避免 ProxyHost/ProxyPort 静态字段未同步时连错端口
            string proxyHost = NormalizeHost(Config.Bind);
            int proxyPort = Config.Port;
            IAsyncResult ar = client.BeginConnect(proxyHost, proxyPort, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(5000)) { r.Detail = "无法连接本地代理 " + proxyHost + ":" + proxyPort; return r; }
            client.EndConnect(ar);
            client.ReceiveTimeout = 10000;
            client.SendTimeout = 10000;
            System.Net.Sockets.NetworkStream stream = client.GetStream();

            if (Config.Mode == "socks") {
                if (!Socks5Handshake(stream, target.Host, 443, ref r)) return r;
            } else {
                string req = "CONNECT " + target.Host + ":443 HTTP/1.1\r\nHost: " + target.Host + ":443\r\n" +
                             "User-Agent: WARP-Proxy-Test\r\n";
                if (!string.IsNullOrEmpty(Config.Username) && !string.IsNullOrEmpty(Config.Password)) {
                    string cred = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.Username + ":" + Config.Password));
                    req += "Proxy-Authorization: Basic " + cred + "\r\n";
                }
                req += "\r\n";
                byte[] reqBytes = Encoding.ASCII.GetBytes(req);
                stream.Write(reqBytes, 0, reqBytes.Length);
                stream.Flush();

                string statusLine = ReadLine(stream, 4096);
                if (string.IsNullOrEmpty(statusLine)) { r.Detail = "代理无响应"; return r; }
                if (statusLine.IndexOf(" 200") < 0) { r.Detail = "代理拒绝: " + statusLine.Trim(); return r; }
            }

            sw.Stop();
            r.Ok = true;
            r.Ms = sw.ElapsedMilliseconds;
            r.Detail = " (经代理)";
        } catch (Exception ex) {
            r.Detail = ex.Message;
        } finally {
            if (client != null) { try { client.Close(); } catch { } }
        }
        return r;
    }

    /// <summary>SOCKS5 握手并建立到目标的连接。成功返回 true。</summary>
    static bool Socks5Handshake(System.Net.Sockets.NetworkStream stream, string host, int port, ref PingResult r) {
        // 1) 方法协商
        bool needAuth = !string.IsNullOrEmpty(Config.Username) && !string.IsNullOrEmpty(Config.Password);
        byte[] greeting = needAuth ? new byte[] { 5, 1, 2 } : new byte[] { 5, 1, 0 };
        stream.Write(greeting, 0, greeting.Length);
        stream.Flush();
        byte[] resp = new byte[2];
        if (!ReadExact(stream, resp, 2)) { r.Detail = "SOCKS5 协商无响应"; return false; }
        if (resp[0] != 5) { r.Detail = "SOCKS5 版本不匹配"; return false; }

        // 2) 认证
        if (resp[1] == 2) {
            byte[] u = Encoding.UTF8.GetBytes(Config.Username);
            byte[] p = Encoding.UTF8.GetBytes(Config.Password);
            byte[] auth = new byte[3 + u.Length + p.Length];
            auth[0] = 1; auth[1] = (byte)u.Length;
            Array.Copy(u, 0, auth, 2, u.Length);
            auth[2 + u.Length] = (byte)p.Length;
            Array.Copy(p, 0, auth, 3 + u.Length, p.Length);
            stream.Write(auth, 0, auth.Length);
            stream.Flush();
            byte[] aresp = new byte[2];
            if (!ReadExact(stream, aresp, 2)) { r.Detail = "SOCKS5 认证无响应"; return false; }
            if (aresp[1] != 0) { r.Detail = "SOCKS5 认证失败(用户名或密码错误)"; return false; }
        } else if (resp[1] == 0xFF) {
            r.Detail = "SOCKS5 代理要求认证，但未配置用户名密码";
            return false;
        }

        // 3) CONNECT 请求：把域名交给代理解析（远程 DNS）
        byte[] hostBytes = Encoding.ASCII.GetBytes(host);
        byte[] conn = new byte[7 + hostBytes.Length];
        conn[0] = 5; conn[1] = 1; conn[2] = 0; conn[3] = 3;
        conn[4] = (byte)hostBytes.Length;
        Array.Copy(hostBytes, 0, conn, 5, hostBytes.Length);
        conn[5 + hostBytes.Length] = (byte)(port >> 8);
        conn[6 + hostBytes.Length] = (byte)(port & 0xFF);
        stream.Write(conn, 0, conn.Length);
        stream.Flush();

        byte[] head = new byte[4];
        if (!ReadExact(stream, head, 4)) { r.Detail = "SOCKS5 连接无响应"; return false; }
        if (head[1] != 0) { r.Detail = "SOCKS5 连接失败，错误码 " + head[1]; return false; }
        // 跳过绑定的地址
        int skip = 0;
        if (head[3] == 1) skip = 4;
        else if (head[3] == 4) skip = 16;
        else if (head[3] == 3) skip = ReadOne(stream);
        if (skip > 0) {
            byte[] junk = new byte[skip + 2];
            ReadExact(stream, junk, junk.Length);
        }
        return true;
    }

    static bool ReadExact(System.Net.Sockets.NetworkStream stream, byte[] buf, int len) {
        int got = 0;
        while (got < len) {
            int n = stream.Read(buf, got, len - got);
            if (n <= 0) return false;
            got += n;
        }
        return true;
    }

    static int ReadOne(System.Net.Sockets.NetworkStream stream) {
        int b = stream.ReadByte();
        return b < 0 ? 0 : b;
    }

    /// <summary>按字节读取一行（用于解析 HTTP 响应状态行，避免 ReadLine 缓冲吞掉后续数据）。</summary>
    static string ReadLine(System.Net.Sockets.NetworkStream stream, int limit) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < limit; i++) {
            int b = stream.ReadByte();
            if (b < 0) break;
            if (b == '\n') break;
            if (b == '\r') continue;
            sb.Append((char)b);
        }
        return sb.ToString();
    }

    /// <summary>一键测试入口：后台执行并在完成后汇总弹窗。</summary>
    static void StartConnectivityTest() {
        if (Testing) return;
        Testing = true;
        if (BtnTestConn != null) {
            RunOnUiThread(() => { BtnTestConn.Enabled = false; BtnTestConn.Text = "测试中..."; });
        }
        new Thread(() => {
            List<PingResult> results;
            try {
                results = RunConnectivityTest();
            } catch (Exception ex) {
                LogSafe("测试过程出错: " + ex.Message);
                results = new List<PingResult>();
            }
            Testing = false;
            RunOnUiThread(() => {
                if (BtnTestConn != null) { BtnTestConn.Enabled = true; BtnTestConn.Text = "测试连接"; }
                ShowTestSummary(results);
            });
        }).Start();
    }

    static void ShowTestSummary(List<PingResult> results) {
        if (results == null || results.Count == 0) {
            MessageBox.Show("测试未能执行，请查看日志。", "测试连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        StringBuilder sb = new StringBuilder();
        int okCount = 0;
        foreach (PingResult r in results) {
            if (r.Ok) { okCount++; sb.AppendLine("✓ " + r.Name + "    " + r.Ms + " ms"); }
            else sb.AppendLine("✗ " + r.Name + "    " + r.Detail);
        }
        sb.AppendLine();
        if (okCount == results.Count) {
            sb.AppendLine("全部连通 (" + okCount + "/" + results.Count + ")");
        } else if (okCount == 0) {
            sb.AppendLine("全部失败 (0/" + results.Count + ")");
            sb.AppendLine("建议：确认代理已「启动」，或检查端口与认证设置。");
        } else {
            sb.AppendLine("部分连通 (" + okCount + "/" + results.Count + ")");
        }
        MessageBox.Show(sb.ToString(), "测试连接结果", MessageBoxButtons.OK,
            okCount == results.Count ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
            if (TrafficLabel != null && !TrafficLabel.IsDisposed) TrafficLabel.Text = "流量: 未运行";
        });
        RunOnUiThread(() => {
            BtnStart.Text = "启动";
            BtnStart.Enabled = true;
            BtnSettings.Enabled = true;
            Progress.Visible = false;
        });
    }

    // ==================== 流量统计 ====================

    // 说明：不用进程 IO 计数器（GetProcessIoCounters）。
    // 实测 usque 下载 9.54MB 后其 ReadTransferCount 仍为 0——Go 程序的网络收发
    // 走 WSARecv/WSASend，不计入进程文件 IO 计数，该数据源对本场景无效。
    // 改用 .NET 的 NetworkInterface 统计（底层为 IP Helper API），能反映真实收发量。

    internal class TrafficSnapshot {
        public long Down;      // 累计接收字节
        public long Up;        // 累计发送字节
        public bool Valid;
    }

    static long TrafficDownBase;    // 本次会话起点，用于统计本次运行流量
    static long TrafficUpBase;
    static long LastDownTotal;      // 上一次采样的累计值，用于算实时速率
    static long LastUpTotal;
    static DateTime LastSampleTime = DateTime.MinValue;

    /// <summary>
    /// 读取本机所有活动网卡的累计收发字节数（排除回环）。
    /// 代理模式下流量经物理网卡进出，因此这个值能反映真实转发量。
    /// </summary>
    internal static TrafficSnapshot ReadTraffic() {
        TrafficSnapshot snap = new TrafficSnapshot();
        try {
            long down = 0, up = 0;
            bool got = false;
            foreach (System.Net.NetworkInformation.NetworkInterface ni in
                     System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                try {
                    System.Net.NetworkInformation.IPv4InterfaceStatistics st = ni.GetIPv4Statistics();
                    down += st.BytesReceived;
                    up += st.BytesSent;
                    got = true;
                } catch { }
            }
            if (!got) return snap;
            snap.Down = down;
            snap.Up = up;
            snap.Valid = true;
        } catch {
            return snap;
        }
        return snap;
    }

    /// <summary>本次会话累计流量（已扣除启动时的基数）。</summary>
    internal static void GetSessionTraffic(out long down, out long up) {
        TrafficSnapshot now = ReadTraffic();
        if (!now.Valid) { down = 0; up = 0; return; }
        if (TrafficDownBase == 0 && TrafficUpBase == 0) {
            TrafficDownBase = now.Down;
            TrafficUpBase = now.Up;
        }
        down = Math.Max(0, now.Down - TrafficDownBase);
        up = Math.Max(0, now.Up - TrafficUpBase);
    }

    /// <summary>重置本次会话的流量基数（每次启动代理时调用）。</summary>
    internal static void ResetTrafficBase() {
        TrafficDownBase = 0;
        TrafficUpBase = 0;
        LastDownTotal = 0;
        LastUpTotal = 0;
        LastSampleTime = DateTime.MinValue;
    }

    internal static string FormatBytes(long bytes) {
        if (bytes < 0) bytes = 0;
        if (bytes < 1024) return bytes + " B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return kb.ToString("0.0") + " KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return mb.ToString("0.00") + " MB";
        double gb = mb / 1024.0;
        return gb.ToString("0.00") + " GB";
    }

    internal static string FormatRate(long bytesPerSec) {
        if (bytesPerSec < 0) bytesPerSec = 0;
        return FormatBytes(bytesPerSec) + "/s";
    }

    /// <summary>流量统计线程：每 2 秒刷新一次界面显示（累计量 + 实时速率）。</summary>
    static void TrafficMonitor() {
        while (true) {
            Thread.Sleep(2000);
            if (WarpProcess == null) {
                RunOnUiThread(() => {
                    if (TrafficLabel != null && !TrafficLabel.IsDisposed)
                        TrafficLabel.Text = "流量: 未运行";
                });
                return;
            }
            TrafficSnapshot now = ReadTraffic();
            if (!now.Valid) {
                RunOnUiThread(() => {
                    if (TrafficLabel != null && !TrafficLabel.IsDisposed) TrafficLabel.Text = "流量: 读取失败";
                });
                continue;
            }
            if (TrafficDownBase == 0 && TrafficUpBase == 0) {
                TrafficDownBase = now.Down;
                TrafficUpBase = now.Up;
            }
            long down = Math.Max(0, now.Down - TrafficDownBase);
            long up = Math.Max(0, now.Up - TrafficUpBase);

            // 实时速率
            long rateDown = 0, rateUp = 0;
            if (LastSampleTime != DateTime.MinValue) {
                double secs = (DateTime.Now - LastSampleTime).TotalSeconds;
                if (secs > 0.5) {
                    rateDown = (long)Math.Max(0, (now.Down - LastDownTotal) / secs);
                    rateUp = (long)Math.Max(0, (now.Up - LastUpTotal) / secs);
                }
            }
            LastDownTotal = now.Down;
            LastUpTotal = now.Up;
            LastSampleTime = DateTime.Now;

            string text = "↓ " + FormatBytes(down) + "  ↑ " + FormatBytes(up) +
                          "   (" + FormatRate(rateDown) + ")";
            string tip = "本次运行累计\n下行: " + FormatBytes(down) + "\n上行: " + FormatBytes(up) +
                         "\n\n实时速率\n下行: " + FormatRate(rateDown) + "\n上行: " + FormatRate(rateUp) +
                         "\n\n点击「详情」查看说明";
            RunOnUiThread(() => {
                if (TrafficLabel == null || TrafficLabel.IsDisposed) return;
                TrafficLabel.Text = text;
                TrafficTip.SetToolTip(TrafficLabel, tip);
            });
        }
    }

    static ToolTip TrafficTip = new ToolTip();

    /// <summary>弹出详细流量信息。</summary>
    static void ShowTrafficDetail() {
        if (WarpProcess == null) {
            MessageBox.Show("当前没有正在运行的代理。" + Environment.NewLine +
                "流量统计针对经代理转发的流量，请先点击「启动」。",
                "流量统计", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        TrafficSnapshot now = ReadTraffic();
        if (!now.Valid) {
            MessageBox.Show("无法读取网卡统计信息。", "流量统计", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        long down, up;
        GetSessionTraffic(out down, out up);
        long rateDown = 0, rateUp = 0;
        if (LastSampleTime != DateTime.MinValue) {
            double secs = (DateTime.Now - LastSampleTime).TotalSeconds;
            if (secs > 0.5) {
                rateDown = (long)Math.Max(0, (now.Down - LastDownTotal) / secs);
                rateUp = (long)Math.Max(0, (now.Up - LastUpTotal) / secs);
            }
        }
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("本次运行（自点击「启动」起）");
        sb.AppendLine("  下行    " + FormatBytes(down));
        sb.AppendLine("  上行    " + FormatBytes(up));
        sb.AppendLine("  合计    " + FormatBytes(down + up));
        sb.AppendLine();
        sb.AppendLine("当前速率");
        sb.AppendLine("  下行    " + FormatRate(rateDown));
        sb.AppendLine("  上行    " + FormatRate(rateUp));
        sb.AppendLine();
        sb.AppendLine("说明：统计口径为本机所有活动网卡的收发字节数");
        sb.AppendLine("（不含回环）。代理未运行时这些计数仍会因其他");
        sb.AppendLine("程序上网而变化，因此只统计「启动」之后的增量。");
        MessageBox.Show(sb.ToString(), "流量统计", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        if (MainForm == null || MainForm.IsDisposed) {
            // 没有窗体（例如后台测试）时直接执行，避免空引用
            try { action(); } catch { }
            return;
        }
        if (MainForm.InvokeRequired) {
            MainForm.Invoke(action);
            return;
        }
        action();
    }

    /// <summary>精确探测本地端口是否有服务监听（真实 TCP 连接，避免 netstat 字符串误判）。</summary>
    internal static bool IsLocalPortOpen(int port) {
        System.Net.Sockets.TcpClient client = null;
        try {
            client = new System.Net.Sockets.TcpClient();
            IAsyncResult ar = client.BeginConnect("127.0.0.1", port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(1200)) return false;
            client.EndConnect(ar);
            return true;
        } catch {
            return false;
        } finally {
            if (client != null) { try { client.Close(); } catch { } }
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
