using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using HmNetCOM;

namespace HmOpenAIChatGpt35Turbo
{
    class AppForm : Form
    {
        const string NewLine = "\r\n";

        IOutputWriter output;
        IInputReader input;

        public AppForm(string key, IOutputWriter output, IInputReader input)
        {
            this.output = output;
            this.input = input;

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
                output.WriteLine(ex.ToString());
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
                ScrollBars = ScrollBars.Both,
                Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
            };
            tb.KeyDown += Tb_KeyDown;
            this.Controls.Add(tb);

            UpdateTextBox();
        }

        public void UpdateTextBox()
        {
            if (tb != null)
            {
                string? selectedText = input.ReadText();
                if (String.IsNullOrEmpty(selectedText))
                {
                    // tb.Text = string.Empty;
                }
                else
                {
                    tb.Text = selectedText;
                }

                tb.Focus();
                tb.Select(tb.Text.Length, 0); // カーソルの位置を末尾に配置しておく。
            }
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
                output.WriteLine(trim);
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
            catch (OperationCanceledException ex)
            {
                output.WriteLine(ex.Message);
                // キャンセルトークン経由なら正規の中断だろうからなにもしない
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                output.WriteLine(err);
            }

            finally
            {
                if (btnCancel != null)
                {
                    btnCancel.Enabled = false;
                }
                if (btnOk != null)
                {
                    btnOk.Enabled = true;
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
                output.WriteLine(err);
            }
            finally
            {
            }
        }
        OpenAIChatMain? ai;

        void SetOpenAI(string key)
        {
            try
            {
                ai = new OpenAIChatMain(key, output);
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                output.WriteLine(err);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                string? msg = e.Data;
                if (msg != null)
                {
                    output.WriteLine(msg);
                }
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                output.WriteLine(err);
            }
        }
    }
}
