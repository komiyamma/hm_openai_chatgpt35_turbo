using HmNetCOM;

namespace HmOpenAIChatGpt
{
    class AppForm : Form
    {
        const string NewLine = "\r\n";

        IOutputWriter output;
        IInputReader input;
        int DisplayDpi = 96;
        string model = "";
        int nTopMost = 0;
        int iMaxTokens = 2000;
        int iAutoMsgListRemove = 1;

        public AppForm(string key, string model, int maxtokens, int topmost, int remove_auto_messagelist, IInputReader input, IOutputWriter output)
        {
            // 「入力」や「出力」の対象を外部から受け取り
            this.output = output;
            this.input = input;
            this.model = model;
            this.iAutoMsgListRemove = remove_auto_messagelist;

            this.iMaxTokens = maxtokens;
            this.nTopMost = topmost;

            try
            {
                SetForm();
                SetOkButton();
                SetCancelButton();
                SetChatClearButton();
                SetTextEdit();
                SetCheckBox();
                SetOpenAI(key);
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());
            }

        }

        // フォーム属性の設定
        void SetForm()
        {
            if (this.DeviceDpi > 96)
            {
                DisplayDpi = this.DeviceDpi;
            }

            this.Text = "*-- HmChatGPT --*";
            this.Width = (int)((500 * DisplayDpi) / 96);
            this.Height = (int)((210 * DisplayDpi) / 96);
            this.FormClosing += AppForm_FormClosing;
            // this.AutoScaleMode = AutoScaleMode.Dpi;
        }

        private void AppForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (ai == null)
            {
                return;
            }

            try
            {
                // 中断したことと同じことをしておく
                BtnCancel_Click(null, e);
            }
            catch (Exception)
            {
                // ここは必ず例外でるので不要。
            }

            try
            {
                // メッセージリストのオートリムーバーのキャンセルをしておく。(うーんぐだぐだだなー)
                OpenAIChatMain.CancelMessageListCancelToken();
            } catch(Exception) { }
        }

        private TextBox? tb;
        void SetTextEdit()
        {
            int top = 28;
            if (btnOk != null) {
                top = btnOk.Bottom + 4;
            }

            tb = new TextBox()
            {
                Multiline = true,
                Top = top,
                Left = 2,
                Height = this.ClientSize.Height - top,
                Width = this.ClientSize.Width - 4,
                ScrollBars = ScrollBars.Both,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            tb.KeyDown += Tb_KeyDown;
            this.Controls.Add(tb);

            UpdateTextBox();
        }

        // 秀丸で選択中のテキストがある状態でマクロが実行されたら、TextBoxで受け取る。
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

                // 見えてない時は、以下はエラーを履くので、tryでくくっておく
                try {
                    this.WindowState = FormWindowState.Normal;

                    this.ActiveControl = tb;
                    tb.Focus();
                    tb.Select(tb.Text.Length, 0); // カーソルの位置を末尾に配置しておく。
                    // このフォームウィンドウのフォーカスをアクティブにする。
                    // 秀丸に瞬間アクティブを取られるかもしれないが、0.5秒間に２度トライする。
                    Task.Run(() =>
                    {
                        Thread.Sleep(250);
                        this.Activate();
                        Thread.Sleep(250);
                        this.Activate();
                    });
                }
                catch (Exception) {
                }
            }
        }

        // キーボードで「CTRL+リターン」だと、ボタンをクリックしたことと同じこととする。
        private void Tb_KeyDown(object? sender, KeyEventArgs e)
        {
            // リターンキーが押されていて
            if (e.KeyCode == Keys.Return)
            {
                // CTRLキーも押されている時だけ、送信ボタンを押した相当にする。
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    // 送信ボタンが有効なら
                    if (btnOk != null && btnOk.Enabled)
                    {
                        BtnOk_Click(null, e);
                    }
                }
            }
        }

        // 送信ボタン
        private Button? btnOk;
        void SetOkButton()
        {
            btnOk = new Button()
            {
                Text = "送信 (Ctrl+⏎)",
                UseVisualStyleBackColor = true,
                Top = 2,
                Left = 2,
                Width = DisplayDpi,
                Height = (int)(DisplayDpi / 4)
            };

            btnOk.Click += BtnOk_Click;
            this.Controls.Add(btnOk);

        }

        // 会話履歴クリアボタン
        private Button? btnChatClear;
        void SetChatClearButton()
        {
            btnChatClear = new Button()
            {
                Text = "会話履歴クリア",
                UseVisualStyleBackColor = true,
                Top = 2,
                Width = (int)(DisplayDpi * 1.25),
                Height = (int)(DisplayDpi / 4),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            btnChatClear.Location = new Point(ClientSize.Width - btnChatClear.Width - 2, 2);

            btnChatClear.Click += BtnChatClear_Click;
            this.Controls.Add(btnChatClear);

        }

        // 中断ボタン
        private Button? btnCancel;

        void SetCancelButton()
        {
            int left = 100;
            if (btnOk != null)
            {
                left = btnOk.Right + 2;
            }
            btnCancel = new Button()
            {
                Text = "中断",
                UseVisualStyleBackColor = true,
                Top = 2,
                Left = left,
                Width = DisplayDpi,
                Height = (int)(DisplayDpi / 4)
            };

            btnCancel.Enabled = false;
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);

        }

        // 送信したらフリーズ時間や解答時間が長いことがあるので、中断用にCancellationTokenSource/CancellationTokenを用意
        static CancellationTokenSource? cs;

        // 中断ボタンおしたら中断用にCancellationTokenSourceにCancel発行する
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

        // 入力された質問を処理する。
        // AIに質問内容を追加し、TextBox⇒アウトプット枠へとメッセージを移動したかのような表示とする。
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
                output.WriteLine(trim + NewLine);
            }
            if (tb != null)
            {
                tb.Text = "";
            }
        }

        // ChatGPTの解答を得る。中断できるようにCancellationTokenを渡す。
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
                if (ex.Message == "The operation was canceled." || ex.Message == "A task was canceled.")
                {
                    if (ai != null)
                    {
                        output.WriteLine(ai.GetAssistanceAnswerCancelMsg());
                    }
                }
                else
                {
                    output.WriteLine(ex.Message);
                }
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
                if (btnChatClear != null)
                {
                    btnChatClear.Enabled = true;
                }

            }

        }

        // 送信ボタンを押すと、質問内容をAIに登録、答えを得る処理へ
        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (ai == null) { return; }

            try
            {
                if (btnOk != null) { btnOk.Enabled = false; }
                if (btnChatClear != null) { btnChatClear.Enabled = false; }
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

        // 送信ボタンを押すと、質問内容をAIに登録、答えを得る処理へ
        private void BtnChatClear_Click(object? sender, EventArgs e)
        {
            if (ai == null) { return; }

            try
            {
                OpenAIChatMain.InitMessages();
                output.WriteLine("-- 会話履歴をクリア --");
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

        CheckBox? cb;

        void SetCheckBox()
        {
            if (btnCancel != null) { 
                cb = new CheckBox();
                cb.Text = "常に手前";
                cb.Left = btnCancel.Right + (int)(10*DisplayDpi/96);
                cb.Height = btnCancel.Height;
                cb.Top = btnCancel.Top;
                cb.Width = (int)(btnCancel.Width);
                cb.CheckAlign = ContentAlignment.MiddleLeft;
                cb.TextAlign = ContentAlignment.MiddleLeft;
                cb.CheckedChanged += Cb_CheckedChanged;
                if (nTopMost > 0)
                {
                    cb.Checked = true;
                }

                this.Controls.Add(cb);
            }
        }

        private void Cb_CheckedChanged(object? sender, EventArgs e)
        {
            if (cb != null)
            {
                if (cb.Checked)
                {
                    this.TopMost = true;
                }
                else
                {
                    this.TopMost = false;
                }
            }
        }


        // aiの処理。キレイには切り分けられてないが、Modelに近い。
        OpenAIChatMain? ai;

        // 最初生成
        void SetOpenAI(string key)
        {
            try
            {
                ai = new OpenAIChatMain(key, model, iMaxTokens, output, iAutoMsgListRemove);
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                output.WriteLine(err);
            }
        }
    }
}
