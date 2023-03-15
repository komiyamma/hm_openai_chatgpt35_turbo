using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using HmNetCOM;

namespace HmOpenAIChatGpt35Turbo
{
    class AppForm : Form
    {
        const string NewLine = "\r\n";

        public AppForm(string key = "")
        {
            try
            {
                SetForm();
                SetTextEdit();
                SetOkButton();
                SetCancelButton();
                SetOpenAI(key);
            }
            catch (Exception ex)
            {
                var msg = ex.ToString().Replace("\n", NewLine);
                Hm.OutputPane.Output(msg);
            }

        }

        void SetForm()
        {
            this.Width = 500;
            this.Height = 210;
            this.FormClosing += AppForm_FormClosing;
        }

        private void AppForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (ai == null)
            {
                return;
            }

            try
            {
                BtnCancel_Click(null, e);
            }
            catch (Exception)
            {
                // ここは必ず例外でるので不要。
            }
        }

        private TextBox? tb;
        void SetTextEdit()
        {
            tb = new TextBox()
            {
                Multiline = true,
                Width = this.Width,
                Top = 24,
                Height = 150,
                Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
            };
            tb.KeyDown += Tb_KeyDown;
            this.Controls.Add(tb);

            tb.Focus();
        }

        private void Tb_KeyDown(object? sender, KeyEventArgs e)
        {
            // リターンキーが押されていて
            if (e.KeyCode == Keys.Return)
            {
                // CTRLキーも押されている時だけ、ボタンを押した相当にする。
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    BtnOk_Click(null, e);
                }
            }
        }

        private Button? btnOk;
        void SetOkButton()
        {
            btnOk = new Button()
            {
                Text = "送信 (Ctrl+⏎)",
                UseVisualStyleBackColor = true,
                Top = 2,
                Left = 2,
                Width = 96,
                Height = 20
            };

            btnOk.Click += BtnOk_Click;
            this.Controls.Add(btnOk);

        }

        private Button? btnCancel;

        void SetCancelButton()
        {
            btnCancel = new Button()
            {
                Text = "中断",
                UseVisualStyleBackColor = true,
                Top = 2,
                Left = 100,
                Width = 96,
                Height = 20
            };

            btnCancel.Enabled = false;
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);

        }

        static CancellationTokenSource? cs;

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (ai != null)
            {
                if (cs != null)
                {
                    cs.Cancel();
                }
            }
        }

        private void PostQuestion()
        {
            var trim = tb?.Text.TrimEnd();
            if (String.IsNullOrEmpty(trim))
            {
                return;
            }

            if (ai != null)
            {
                ai.AddQuestion(trim);
                Hm.OutputPane.Output(trim + NewLine);
            }
            if (tb != null)
            {
                tb.Text = "";
            }
        }

        private async Task GetAnswer()
        {
            try
            {
                if (btnCancel != null)
                {
                    btnCancel.Enabled = true;
                }

                cs = new();
                if (ai != null)
                {

                    await Task.Run(async () =>
                    {
                        await ai.AddAnswer(cs.Token);
                    }, cs.Token);
                }

            }
            catch(OperationCanceledException)
            {
                // キャンセルトークン経由なら正規の中断だろうからなにもしない
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                err = err.Replace("\n", NewLine);
                Hm.OutputPane.Output(err + "\r\n");
            }

            finally
            {
                if (btnCancel != null)
                {
                    btnCancel.Enabled = false;
                }
            }

        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (ai == null) { return; }

            try
            {
                if (btnOk != null) { btnOk.Enabled = false; }
                PostQuestion();
                _ = GetAnswer();

            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                err = err.Replace("\n", NewLine);
                Hm.OutputPane.Output(err + "\r\n");
            }
            finally
            {
                if (btnOk != null) { btnOk.Enabled = true; }
            }
        }
        OpenAIChatMain? ai;

        void SetOpenAI(string key)
        {
            try
            {
                ai = new OpenAIChatMain(key);
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                err = err.Replace("\n", NewLine);
                Hm.OutputPane.Output(err + "\r\n");
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                string? msg = e.Data;
                if (msg != null)
                {
                    msg = msg.Replace("\n", NewLine);
                    Hm.OutputPane.Output(msg + NewLine);
                }
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                err = err.Replace("\n", NewLine);
                Hm.OutputPane.Output(err + "\r\n");
            }
        }
    }
}
