using System;
using System.Security.Principal;
using System.Windows.Forms;

namespace KeepAwake
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!IsElevated())
            {
                MessageBox.Show(
                    "本程序需要管理员权限才能修改电源与锁屏策略。\n请在 UAC 提示中选择「是」。",
                    "KeepAwake",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "程序启动失败：\n" + ex.Message +
                    "\n\n请安装 .NET Framework 4.0 或更高版本（推荐 4.8）。",
                    "KeepAwake",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool IsElevated()
        {
            try
            {
                using (var id = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(id);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
