﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hidemaru;

namespace HmChatGpt35Turbo
{
    class AppForm : Form
    {
        const string NewLine = "\r\n";

        public AppForm()
        {
            try
            {
                SetForm();
                SetTextEdit();
                SetButton();
                SetProcess();
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

        private void AppForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (proc == null)
            {
                return;
            }

            try
            {
                if (sw != null)
                {
                    sw.WriteLine("チャットを終了");
                }

                if (sw != null)
                {
                    if (proc != null)
                    {
                        sw.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                // ここは必ず例外でるので不要。
            }

            try
            {
                if (proc != null)
                {
                    proc.Close();
                    proc.Kill();
                }

            }
            catch (Exception ex)
            {
                // ここは必ず例外でるので不要。
            }
        }

        private TextBox tb;
        void SetTextEdit()
        {
            tb = new TextBox();
            tb.Multiline = true;
            tb.Width = this.Width;
            tb.Top = 24;
            tb.Height = 150;
            tb.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            tb.KeyDown += Tb_KeyDown;
            this.Controls.Add(tb);
        }

        private void Tb_KeyDown(object sender, KeyEventArgs e)
        {
            // リターンキーが押されていて
            if (e.KeyCode == Keys.Return)
            {
                // CTRLキーも押されている時だけ、ボタンを押した相当にする。
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    Btn_Click(null, e);
                }
            }
        }

        private Button btn;
        void SetButton()
        {
            btn = new Button();
            btn.Text = "送信 (Ctrl+↵)";
            btn.UseVisualStyleBackColor = true;
            this.Controls.Add(btn);
            btn.Top = 2;
            btn.Left = 2;
            btn.Width = 90;
            btn.Height = 20;

            btn.Click += Btn_Click;
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            if (proc == null) { return; }

            try
            {
                if (String.IsNullOrEmpty(tb.Text))
                {
                    return;
                }

                if (sw != null)
                {
                    sw.WriteLine(tb.Text);
                    Hm.OutputPane.Output(tb.Text + NewLine);
                    tb.Text = "";
                }
            }
            catch (Exception ex)
            {
                Hm.OutputPane.Output(ex.Message + NewLine + ex.StackTrace);
            }
        }

        private Process proc;
        private StreamWriter sw;
        void SetProcess()
        {
            try
            {
                proc = new Process();
                var info = new ProcessStartInfo();
                var dllpath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var dirpath = System.IO.Path.GetDirectoryName(dllpath);
                info.FileName = Path.Combine(dirpath, "hm_openai_chatgpt35turbo_outprocess.exe");
                info.RedirectStandardOutput = true;
                info.RedirectStandardInput = true;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                proc.StartInfo = info;

                proc.OutputDataReceived += Process_OutputDataReceived;

                proc.Start();
                proc.BeginOutputReadLine();
                System.Windows.Forms.Application.DoEvents();
                // process.BeginErrorReadLine();
                sw = proc.StandardInput;
            }
            catch (Exception ex)
            {
                Hm.OutputPane.Output(ex.Message + NewLine + ex.StackTrace);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                string msg = e.Data;
                if (msg != null)
                {
                    msg = msg.Replace("\n", NewLine);
                    Hm.OutputPane.Output(msg + NewLine);
                }
            }
            catch (Exception ex)
            {
                Hm.OutputPane.Output(ex.Message + NewLine + ex.StackTrace);
            }
        }
    }

    public class HmChatGpt35Turbo
    {
        public static Form form;

        public static IntPtr CreateForm()
        {
            if (form == null || !form.Visible)
            {
                form = new AppForm();
            }

            form.Show();
            return (IntPtr)1;
        }

        public static IntPtr OnDetachMethod()
        {
            if (form != null)
            {
                form.Close();
                form = null;
            }

            return (IntPtr)1;
        }

        public static void Main(String[] args)
        {
            try
            {
                if (form == null || !form.Visible)
                {
                    form = new AppForm();
                }

                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}