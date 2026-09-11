using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace KeepAwake
{
    /// <summary>
    /// 写入系统电源/锁屏策略（持久生效，不依赖本进程存活）。
    /// 核心：允许息屏，但动鼠标唤醒时不必重新登录。
    /// </summary>
    internal static class PowerTweaks
    {
        private const uint SPI_SETSCREENSAVESECURE = 0x0077;
        private const uint SPI_SETSCREENSAVEACTIVE = 0x0011;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        // 电源方案 GUID：唤醒时是否锁定控制台
        private const string ConsoleLockGuid = "0e796bdb-100d-47d6-a2d5-f7d2daa51f51";
        private const string SubNoneGuid = "FEA3413E-7E05-4911-9A71-700331F1C294";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        public sealed class ApplyResult
        {
            public bool Ok;
            public string Message;
        }

        public static ApplyResult Apply(
            bool wakeWithoutLogin,
            bool disableSleep,
            bool neverTurnOffMonitor)
        {
            var log = new StringBuilder();
            bool ok = true;

            try
            {
                if (wakeWithoutLogin)
                {
                    ok &= DisableLockOnWake(log);
                }

                if (disableSleep)
                {
                    ok &= RunPowerCfg("/change standby-timeout-ac 0", log);
                    ok &= RunPowerCfg("/change standby-timeout-dc 0", log);
                    ok &= RunPowerCfg("/change hibernate-timeout-ac 0", log);
                    ok &= RunPowerCfg("/change hibernate-timeout-dc 0", log);
                    log.AppendLine("已关闭睡眠 / 休眠超时（插电与电池）。");
                }

                if (neverTurnOffMonitor)
                {
                    ok &= RunPowerCfg("/change monitor-timeout-ac 0", log);
                    ok &= RunPowerCfg("/change monitor-timeout-dc 0", log);
                    log.AppendLine("已关闭显示器自动关闭（永久亮屏）。");
                }

                RunPowerCfg("/S SCHEME_CURRENT", log);
                log.AppendLine();
                log.AppendLine("以上为系统级设置，关掉本程序后仍然有效。");
                log.AppendLine("效果：屏幕可正常息屏；动一下鼠标/键盘应直接点亮，无需登录。");
            }
            catch (Exception ex)
            {
                ok = false;
                log.AppendLine("异常：" + ex.Message);
            }

            return new ApplyResult { Ok = ok, Message = log.ToString().TrimEnd() };
        }

        public static ApplyResult RestoreDefaults()
        {
            var log = new StringBuilder();
            bool ok = true;

            try
            {
                ok &= SetConsoleLock(true, log);
                SetRequirePasswordOnWake(true, log);
                // 恢复「一段时间后需登录」由系统自行管理：删除强制的 0 秒策略
                TryDeleteInactivityTimeout(log);

                ok &= RunPowerCfg("/change monitor-timeout-ac 10", log);
                ok &= RunPowerCfg("/change monitor-timeout-dc 5", log);
                ok &= RunPowerCfg("/change standby-timeout-ac 30", log);
                ok &= RunPowerCfg("/change standby-timeout-dc 15", log);
                RunPowerCfg("/S SCHEME_CURRENT", log);
                log.AppendLine("已恢复较常见默认（唤醒需密码、显示器约 10 分钟）。可在系统设置中再调。");
            }
            catch (Exception ex)
            {
                ok = false;
                log.AppendLine("异常：" + ex.Message);
            }

            return new ApplyResult { Ok = ok, Message = log.ToString().TrimEnd() };
        }

        private static bool DisableLockOnWake(StringBuilder log)
        {
            bool ok = true;

            // 1) 电源选项：「唤醒时需要密码」= 否（息屏/睡眠唤醒都不锁）
            ok &= SetConsoleLock(false, log);

            // 2) 注册表 PromptPasswordOnResume
            SetRequirePasswordOnWake(false, log);

            // 3) Win10/11「离开后多久需要重新登录」= 从不
            SetInactivityTimeout(0, log);

            // 4) 关闭「屏保时显示登录界面」
            SetScreenSaverSecure(false, log);
            try
            {
                SystemParametersInfo(SPI_SETSCREENSAVESECURE, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            catch
            {
            }

            // 5) 关闭动态锁（手机走远自动锁）
            DisableDynamicLock(log);

            // 6) 策略层再写一遍 CONSOLELOCK，防止被组策略外的默认覆盖
            WriteConsoleLockPolicy(0, log);

            log.AppendLine("已关闭：息屏/睡眠后唤醒需重新登录。");
            return ok;
        }

        private static bool SetConsoleLock(bool requireLock, StringBuilder log)
        {
            string v = requireLock ? "1" : "0";
            bool ok = true;
            ok &= RunPowerCfg("-setacvalueindex SCHEME_CURRENT SUB_NONE CONSOLELOCK " + v, log);
            ok &= RunPowerCfg("-setdcvalueindex SCHEME_CURRENT SUB_NONE CONSOLELOCK " + v, log);
            // GUID 写法作补充（失败不致命）
            RunPowerCfg("-setacvalueindex SCHEME_CURRENT " + SubNoneGuid + " " + ConsoleLockGuid + " " + v, log);
            RunPowerCfg("-setdcvalueindex SCHEME_CURRENT " + SubNoneGuid + " " + ConsoleLockGuid + " " + v, log);
            log.AppendLine(requireLock
                ? "电源方案：唤醒时锁定 = 是"
                : "电源方案：唤醒时锁定 = 否");
            return ok;
        }

        private static void WriteConsoleLockPolicy(int value, StringBuilder log)
        {
            try
            {
                string path = @"SOFTWARE\Policies\Microsoft\Power\PowerSettings\" + ConsoleLockGuid;
                using (var key = Registry.LocalMachine.CreateSubKey(path))
                {
                    if (key != null)
                    {
                        key.SetValue("ACSettingIndex", value, RegistryValueKind.DWord);
                        key.SetValue("DCSettingIndex", value, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                log.AppendLine("CONSOLELOCK 策略写入失败：" + ex.Message);
            }
        }

        private static void SetScreenSaverSecure(bool secure, StringBuilder log)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        key.SetValue("ScreenSaveActive", "0", RegistryValueKind.String);
                        key.SetValue("ScreenSaverIsSecure", secure ? "1" : "0", RegistryValueKind.String);
                    }
                }
                log.AppendLine("已关闭屏保登录保护。");
            }
            catch (Exception ex)
            {
                log.AppendLine("屏保注册表失败：" + ex.Message);
            }
        }

        private static void SetInactivityTimeout(int seconds, StringBuilder log)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                        key.SetValue("InactivityTimeoutSecs", seconds, RegistryValueKind.DWord);
                }
                log.AppendLine("「离开后需重新登录」已设为从不（InactivityTimeoutSecs=" + seconds + "）。");
            }
            catch (Exception ex)
            {
                log.AppendLine("InactivityTimeoutSecs 失败：" + ex.Message);
            }
        }

        private static void TryDeleteInactivityTimeout(StringBuilder log)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("InactivityTimeoutSecs", false); }
                        catch { }
                    }
                }
                log.AppendLine("已清除 InactivityTimeoutSecs 强制策略。");
            }
            catch (Exception ex)
            {
                log.AppendLine("清除 InactivityTimeoutSecs 失败：" + ex.Message);
            }
        }

        private static void SetRequirePasswordOnWake(bool require, StringBuilder log)
        {
            try
            {
                int v = require ? 1 : 0;
                using (var key = Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Power"))
                {
                    if (key != null)
                        key.SetValue("PromptPasswordOnResume", v, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Policies\Microsoft\Windows\Control Panel\Desktop"))
                {
                    if (key != null)
                        key.SetValue("ScreenSaverIsSecure", require ? "1" : "0", RegistryValueKind.String);
                }
                log.AppendLine(require ? "已开启唤醒需密码。" : "已关闭唤醒需密码。");
            }
            catch (Exception ex)
            {
                log.AppendLine("PromptPasswordOnResume 失败：" + ex.Message);
            }
        }

        private static void DisableDynamicLock(StringBuilder log)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    // 0 = 关动态锁（部分版本键名不同，写了无害）
                    if (key != null)
                        key.SetValue("EnableGoodbye", 0, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\SignInState"))
                {
                    // 占位：部分系统无此键
                }
                log.AppendLine("已尝试关闭动态锁。");
            }
            catch (Exception ex)
            {
                log.AppendLine("动态锁设置跳过：" + ex.Message);
            }
        }

        private static bool RunPowerCfg(string args, StringBuilder log)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(0),
                    StandardErrorEncoding = Encoding.GetEncoding(0)
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return false;
                    p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit(15000);
                    if (p.ExitCode != 0)
                    {
                        log.AppendLine("powercfg " + args + " → 退出码 " + p.ExitCode);
                        if (!string.IsNullOrWhiteSpace(stderr))
                            log.AppendLine(stderr.Trim());
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                log.AppendLine("powercfg 失败：" + ex.Message);
                return false;
            }
        }
    }
}
