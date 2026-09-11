using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeepAwake
{
    internal sealed class MainForm : Form
    {
        private readonly CheckBox _chkNoLogin;
        private readonly CheckBox _chkSleep;
        private readonly CheckBox _chkMonitor;
        private readonly Button _btnApply;
        private readonly Button _btnRestore;
        private readonly TextBox _txtLog;
        private readonly Label _lblStatus;

        public MainForm()
        {
            Text = "KeepAwake — 息屏唤醒免登录";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(540, 420);
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(245, 247, 250);
            ForeColor = Color.FromArgb(30, 34, 40);

            var title = new Label
            {
                Text = "KeepAwake",
                Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
                Location = new Point(24, 16),
                AutoSize = true,
                ForeColor = Color.FromArgb(20, 90, 160)
            };

            var subtitle = new Label
            {
                Text = "写入系统设置后即可关闭本程序 · Win7 / 10 / 11 · 需管理员",
                Location = new Point(26, 54),
                AutoSize = true,
                ForeColor = Color.FromArgb(90, 98, 110)
            };

            var tip = new Label
            {
                Text = "目标效果：过几分钟屏幕熄灭也没关系，动一下鼠标直接亮屏，不用再点登录。",
                Location = new Point(26, 78),
                Size = new Size(490, 36),
                ForeColor = Color.FromArgb(50, 70, 90)
            };

            _chkNoLogin = MakeCheck("息屏 / 睡眠后唤醒：不必重新登录（推荐，默认开启）", 24, 120);
            _chkSleep = MakeCheck("额外：关闭睡眠 / 休眠（可选）", 24, 152);
            _chkMonitor = MakeCheck("额外：显示器永不关闭 / 永久亮屏（可选，一般不需要）", 24, 184);
            _chkNoLogin.Checked = true;
            _chkSleep.Checked = false;
            _chkMonitor.Checked = false;

            _btnApply = new Button
            {
                Text = "一键应用（持久生效）",
                Location = new Point(24, 230),
                Size = new Size(200, 38),
                FlatStyle = FlatStyle.System
            };
            _btnApply.Click += OnApply;

            _btnRestore = new Button
            {
                Text = "恢复常见默认",
                Location = new Point(240, 230),
                Size = new Size(140, 38),
                FlatStyle = FlatStyle.System
            };
            _btnRestore.Click += OnRestore;

            _lblStatus = new Label
            {
                Text = "就绪。点一次即可，不需要一直开着本软件。",
                Location = new Point(26, 280),
                Size = new Size(490, 22),
                ForeColor = Color.FromArgb(40, 120, 70)
            };

            _txtLog = new TextBox
            {
                Location = new Point(24, 308),
                Size = new Size(490, 90),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(tip);
            Controls.Add(_chkNoLogin);
            Controls.Add(_chkSleep);
            Controls.Add(_chkMonitor);
            Controls.Add(_btnApply);
            Controls.Add(_btnRestore);
            Controls.Add(_lblStatus);
            Controls.Add(_txtLog);
        }

        private static CheckBox MakeCheck(string text, int x, int y)
        {
            return new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private void OnApply(object sender, EventArgs e)
        {
            if (!_chkNoLogin.Checked && !_chkSleep.Checked && !_chkMonitor.Checked)
            {
                MessageBox.Show("请至少勾选一项。", "KeepAwake", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _btnApply.Enabled = false;
            _btnRestore.Enabled = false;
            try
            {
                var result = PowerTweaks.Apply(
                    _chkNoLogin.Checked,
                    _chkSleep.Checked,
                    _chkMonitor.Checked);

                _txtLog.Text = result.Message;
                _lblStatus.Text = result.Ok
                    ? "已写入系统。可以关掉本程序，设置继续有效。"
                    : "部分步骤失败，请看下方日志（需管理员）。";
                _lblStatus.ForeColor = result.Ok
                    ? Color.FromArgb(40, 120, 70)
                    : Color.FromArgb(170, 60, 40);
            }
            finally
            {
                _btnApply.Enabled = true;
                _btnRestore.Enabled = true;
            }
        }

        private void OnRestore(object sender, EventArgs e)
        {
            var ask = MessageBox.Show(
                "将恢复「唤醒需密码」等较常见默认。\n确定继续？",
                "KeepAwake",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (ask != DialogResult.Yes) return;

            var result = PowerTweaks.RestoreDefaults();
            _txtLog.Text = result.Message;
            _lblStatus.Text = result.Ok ? "已恢复常见默认。" : "恢复时有错误，请看日志。";
            _lblStatus.ForeColor = result.Ok
                ? Color.FromArgb(40, 120, 70)
                : Color.FromArgb(170, 60, 40);
        }
    }
}
