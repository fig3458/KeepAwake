# KeepAwake

Windows 一键关闭「息屏/睡眠后需要重新登录」的小工具。

闲置几分钟屏幕可以正常熄灭；动一下鼠标或键盘直接亮屏进入桌面，不必再点登录、输密码。

设置写入系统电源方案与注册表，**点一次即可，不依赖程序常驻**。

兼容 **Windows 7 / 10 / 11**，启动时通过 UAC 请求管理员权限。

## 功能

- 息屏 / 睡眠唤醒后不必重新登录（默认）
- 可选：关闭睡眠 / 休眠
- 可选：显示器永不关闭（永久亮屏）
- 一键恢复较常见的系统默认值

## 下载与使用

1. 下载本仓库，或到 [Releases](https://github.com/fig3458/KeepAwake/releases) 下载已编译的 `KeepAwake.exe`
2. 双击运行，UAC 选择「是」
3. 保持默认勾选，点击「一键应用（持久生效）」
4. 可以关掉软件，设置继续有效

## 自行编译

需要已安装 .NET Framework 4.0+（Win10/11 通常自带）。

```bat
build.bat
```

生成文件：

- `bin\KeepAwake.exe`
- `dist\KeepAwake.exe`

## 源码结构

| 文件 | 说明 |
|------|------|
| `Program.cs` | 入口 |
| `MainForm.cs` | GUI |
| `PowerTweaks.cs` | `powercfg` + 注册表改电源/锁屏策略 |
| `app.manifest` | `requireAdministrator` |
| `build.bat` | 用系统自带 `csc.exe` 编译单文件 exe |

## 说明

- 公司域控 / 组策略环境可能强制锁屏，会覆盖本机修改
- 本工具仅修改本机电源与登录相关策略，请在自己有权管理的电脑上使用

## License

MIT

## 许可证

[MIT](LICENSE)
