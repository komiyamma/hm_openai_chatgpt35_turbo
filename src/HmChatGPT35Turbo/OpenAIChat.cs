
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using HmNetCOM;
using OpenAI.GPT3.Interfaces;

namespace HmOpenAIChatGpt35Turbo
{
    // OPENAI_API_KEYが設定されてないよってなエラー
    class OpenAIKeyNotFoundException : KeyNotFoundException
    {
        public OpenAIKeyNotFoundException(string msg) : base(msg) { }
    }

    // OpenAIのサービスに接続できないよ系
    class OpenAIServiceNotFoundException : Exception
    {
        public OpenAIServiceNotFoundException(string msg) : base(msg) { }
    }

    class OpenAIChatMain
    {
        // 出力対象のDI用
        IOutputWriter output;

        const string NewLine = "\r\n";

        const string OpenAIKeyEnvironmentVariableName = "OPENAI_KEY";
        static string? OpenAIKeyOverWriteVariable = null; // 直接APIの値を上書き指定している場合(マクロなどからの直接の引き渡し)
        const string ErrorMessageNoOpenAIKey = OpenAIKeyEnvironmentVariableName + "キーが環境変数にありません。:" + NewLine;

        public OpenAIChatMain(string key, IOutputWriter output)
        {
            // 出力対象のDI用
            this.output = output;

            // とりあえず代入。エラーならChatGPTの方が言ってくれる。
            if (key.Length > 0)
            {
                OpenAIKeyOverWriteVariable = key;
            }
            GetOpenAIKey(); // かまし
        }

        // OpenAIのキーの取得
        static string? GetOpenAIKey()
        {
            if (String.IsNullOrEmpty(OpenAIKeyOverWriteVariable))
            {
                string? key = Environment.GetEnvironmentVariable(OpenAIKeyEnvironmentVariableName);
                if (String.IsNullOrEmpty(key))
                {
                    throw new OpenAIKeyNotFoundException(ErrorMessageNoOpenAIKey);
                }
                return key;
            }
            else
            {
                return OpenAIKeyOverWriteVariable;
            }
        }


        const string QuestionPromptMessages = "-- 質問をどうぞ --" + NewLine + NewLine;
        const string NoQuestionMessage = "質問内容が無い" + NewLine;

        public string GetQuestionMessageEmpty()
        {
            return NoQuestionMessage;
        }

        const string ChatEndMessage = "チャットを終了";
        // 質問内容の取得

        const string ErrorMessageNoOpenAIService = "OpenAIのサービスに接続できません。:" + NewLine;
        // OpenAIサービスのインスタンス。一応保持
        static OpenAIService? openAiService = null;

        // OpenAIへの接続
        static OpenAIService? ConnectOpenAIService(string key)
        {
            try
            {
                var openAiService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = key
                });

                return openAiService;
            }
            catch (Exception)
            {
                throw;
            }
        }


        // OpenAIにわたす会話ログ。基本的にOpenAIは会話の文脈を覚えているので、メッセージログ的なものを渡す必要がある。
        static List<ChatMessage> messageList = new();

        // 最初のシステムメッセージ。
        const string ChatGPTStartSystemMessage = "You are a helpful assistant.";

        // チャットのエンジンやオプション。過去のチャット内容なども渡す。
        static IAsyncEnumerable<ChatCompletionCreateResponse> ReBuildPastChatContents(CancellationToken ct)
        {
            var key = GetOpenAIKey();
            if (key == null)
            {
                throw new OpenAIKeyNotFoundException(ErrorMessageNoOpenAIKey);
            }

            if (openAiService == null)
            {
                openAiService = ConnectOpenAIService(key);
            }
            if (openAiService == null)
            {
                throw new OpenAIServiceNotFoundException(ErrorMessageNoOpenAIService);
            }

            // 最初にシステムからの挨拶メッセージ
            var list = new List<ChatMessage>();
            list.Add(ChatMessage.FromSystem(ChatGPTStartSystemMessage));

            // 次に人間とChatGPTの会話を履歴として追加していく
            foreach (var mes in messageList)
            {
                list.Add(mes);
            }

            // オプション。1000～2000トークンぐらいでセーフティかけておくのがいいだろう。
            // 元々ChatGPTの方でも4000トークンぐらいでセーフティがかかってる模様
            var options = new ChatCompletionCreateRequest
            {
                Messages = list,
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 2000
            };

            // ストリームとして会話モードを確率する。ストリームにすると解答が１文字ずつ順次表示される。
            var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(options, null, ct);
            return completionResult;
        }


        const string AssistanceAnswerCompleteMsg = NewLine + "-- 完了 --" + NewLine;
        const string ErrorMsgUnknown = "Unknown Error:" + NewLine;
        // チャットの反復

        const string AssistanceAnswerCancelMsg = NewLine + "-- ChatGPTの解答を途中キャンセルしました --" + NewLine;
        public string GetAssistanceAnswerCancelMsg()
        {
            return AssistanceAnswerCancelMsg;
        }

        public async Task AddAnswer(CancellationToken ct)
        {
            string answer_sum = "";
            var completionResult = ReBuildPastChatContents(ct);

            // ストリーム型で確立しているので、async的に扱っていく
            await foreach (var completion in completionResult)
            {
                // キャンセルが要求された時、
                if (ct.IsCancellationRequested)
                {
                    // 一応Dispose呼んでおく(CancellationToken渡しているので不要なきもするが...)
                    await completionResult.GetAsyncEnumerator().DisposeAsync();
                    throw new OperationCanceledException(AssistanceAnswerCancelMsg);
                }

                // キャンセルされてたら OperationCanceledException を投げるメソッド
                ct.ThrowIfCancellationRequested();

                // 会話成功なら
                if (completion.Successful)
                {
                    // ちろっと文字列追加表示
                    string? str = completion.Choices.FirstOrDefault()?.Message.Content;
                    if (str != null)
                    {
                        output.Write(str);
                        answer_sum += str ?? "";
                    }
                }
                else
                {
                    // 失敗なら何かエラーと原因を表示
                    if (completion.Error == null)
                    {
                        throw new Exception(ErrorMsgUnknown);
                    }

                    output.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                }
            }

            // 今回の返答ををChatGPTの返答として記録しておく
            messageList.Add(ChatMessage.FromAssistant(answer_sum));

            // 解答が完了したよ～というのを人にわかるように表示
            output.WriteLine(AssistanceAnswerCompleteMsg);
        }

        // 質問内容はそのまま履歴に追加する
        public void AddQuestion(string question)
        {
            messageList.Add(ChatMessage.FromUser(question));
        }
    }
}


