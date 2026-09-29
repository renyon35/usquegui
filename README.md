# WARP 代理启动器

一个面向 Windows 的图形界面启动器，把 [usque](https://github.com/Diniboy1123/usque) 的 Cloudflare WARP（MASQUE）隧道和 [mihomo](https://github.com/MetaCubeX/mihomo) 的分流能力，打包成一个双击即用的小工具。

不需要装官方 WARP 客户端，不需要懂命令行。

---

## 功能

- **一键启停**：点「启动」拉起 WARP 隧道与分流内核，点「停止」干净收尾（进程、路由、系统代理一并还原）。
- **启动中可随时取消**：启动过程中「停止」按钮变为「取消启动」，任何阶段都能立刻打断并清理。
- **三种接管方式，可叠加**
  - `系统代理`：改写系统代理设置，浏览器等自动走代理。
  - `全局代理`：usque 原生 TUN，接管全部流量。
  - `Clash 分流`：mihomo 接管，按规则分流，支持按进程指定走向。
- **分应用分流**：在界面上按进程名 / 可执行文件路径指定「走 WARP」或「直连」，规则直接写入 `clash.yaml` 的 `rules:` 段，内核原生匹配。
- **设置面板**：代理、DNS、隧道、启动、全局、Clash 分流 六个分组，覆盖 usque 的常用参数。
- **流量统计 / 日志窗口 / 托盘图标 / 开机自启**。
- **一键恢复网络**：全局代理异常导致断网时，一键删除 TUN 默认路由、还原原路由并停掉隧道进程。

## 系统要求

| 项目 | 要求 |
| --- | --- |
| 系统 | Windows 10 / 11，64 位 |
| 运行时 | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| 权限 | 普通权限可用系统代理 / Clash 分流（非 TUN）；**全局代理与 Clash TUN 需要管理员权限** |

## 快速开始

1. 从 [Releases](../../releases) 下载 `WARP-Proxy-Launcher-v1.0.0-win-x64.zip`，解压到任意目录。
   - 路径建议不含中文与空格，避免个别场景下的兼容问题。
2. 首次使用先注册一次 WARP 账户：双击 `reg.bat`（等价于 `usque.exe register`），会在同目录生成 `config.json`。
   - 程序启动时若发现缺少 `config.json`，也会自动尝试注册一次。
3. 双击 `proxy.exe`。
4. 点「启动」。

> 默认设置下程序一启动就会自动连接（`AutoStart=true`）。不想自动连就到「设置 → 启动」
> 取消勾选「启动程序时自动开启代理」并保存。

## 界面说明

主窗口分四层：

- **按钮行**：`启动` / `停止` / `设置`
- **开关行**：`系统代理` / `全局代理` / `Clash 分流`
- **分应用分流**：只读规则摘要 + `编辑分应用规则…`
- **工具行**：`测试连接` / `日志` / `恢复网络`，底部是状态提示与流量行

## 从源码编译

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```powershell
dotnet build WARPBuild.csproj -c Release
```

产物在 `bin\Release\net8.0-windows\`：

```
WARPBuild.exe                 # 主程序，改名为 proxy.exe 使用
WARPBuild.dll
WARPBuild.deps.json
WARPBuild.runtimeconfig.json
```

把它和 `app.ico`、`reg.bat`，以及第三方的 `usque.exe`、`mihomo.exe`、`geoip.metadb`、`wintun.dll` 放在同一目录即可运行。

若要发布为免安装运行时的版本：

```powershell
dotnet publish WARPBuild.csproj -c Release -r win-x64 --self-contained true
```

## 目录结构

```
├── proxy.cs               # 主程序：窗口、启动流程、TUN 路由、流量统计
├── AppRuleEditor.cs       # 分应用规则编辑器（DataGridView 弹窗）
├── WARPBuild.csproj       # 工程文件
├── app.ico                # 图标
├── reg.bat                # 首次注册 WARP 账户
├── LICENSE
├── THIRD-PARTY-NOTICES.md
└── .gitignore
```

运行时目录（发布包里）：

```
├── proxy.exe                  # 主程序
├── WARPBuild.dll / .deps.json / .runtimeconfig.json
├── app.ico
├── reg.bat
├── usque.exe                  # 第三方：WARP 隧道（MIT）
├── mihomo.exe                 # 第三方：Clash 内核（GPL-3.0）
├── geoip.metadb               # 第三方：mihomo 地理数据
├── wintun.dll                 # 第三方：TUN 驱动（WireGuard LLC）
├── config.json                # 运行期生成，含 WARP 凭据，切勿外传
├── proxy.ini                  # 运行期生成，启动器设置
└── clash.yaml                 # 运行期生成，mihomo 配置
```

## 配置说明

- **`proxy.ini`** —— 启动器自己的设置，首次保存时生成。删掉即恢复默认。
- **`clash.yaml`** —— 由程序根据设置生成。分应用分流规则直接写在 `rules:` 段顶部，内核按 `PROCESS-NAME` / `PROCESS-PATH` 匹配。
- **`config.json`** —— usque 的 WARP 凭据（私钥、access token、分配到的 IP 等）。**这是你的身份凭据，不要上传、不要分享。**
- **`exception-log.txt`** —— 仅在程序发生未捕获异常时生成，用于排查崩溃。

## 分应用分流怎么用

1. 点主界面「编辑分应用规则…」。
2. 选进程（可就近从当前运行的进程里挑），选目标分组（`WARP` 走隧道 / `DIRECT` 直连），保存。
3. 规则会被写进 `clash.yaml`，若是运行中会热重载。

> 注意：分应用分流依赖内核按进程匹配，需要「Clash 分流」处于开启状态；Windows 上按带 `.exe` 的进程名匹配最稳。

## 常见问题

**Q：Clash 分流开着但没生效？**
多半是权限问题。内核创建 TUN 网卡需要管理员权限，请以管理员身份运行 `proxy.exe`。

**Q：启动后浏览器上不了网？**
点主界面「恢复网络」一键还原路由与系统代理，再检查设置。程序在检测到系统代理指向自身而代理已停止时，也会自动清除系统代理。

**Q：解析超时 / 内核报 `context deadline exceeded`？**
把「设置 → DNS」里的 `Clash 解析用 DNS` 换成国内可达的地址（默认 `223.5.5.5` / `119.29.29.29`）。该项是内核自己解析域名用的，和隧道内的 `-d` DNS 相互独立。

**Q：端口被占用？**
程序启动时会尝试释放配置的监听端口；也可以自行在设置里换端口。

## 第三方组件

本项目捆绑 / 依赖以下第三方组件，各自的许可与出处见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)：

| 组件 | 用途 | 许可 |
| --- | --- | --- |
| usque | WARP MASQUE 隧道 | MIT |
| mihomo | Clash 分流内核 | GPL-3.0 |
| geoip.metadb | mihomo 地理数据 | 随 mihomo 数据仓库 |
| wintun.dll | Windows TUN 驱动 | Wintun Prebuilt Binaries License |

## 许可

本仓库自身的代码以 MIT 许可发布，见 [LICENSE](LICENSE)。

## 免责声明

本项目仅为网络工具的技术实现示例，用于学习与研究网络协议。使用者应自行遵守所在国家或地区的法律法规以及网络服务提供商的服务条款。因使用本软件产生的一切后果由使用者自行承担。
