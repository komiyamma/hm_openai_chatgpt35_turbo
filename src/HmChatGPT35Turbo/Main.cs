﻿
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using HmNetCOM;

namespace HmOpenAIChatGpt35Turbo
{
    class OpenAIKeyNotFoundException : KeyNotFoundException
    {
        public OpenAIKeyNotFoundException(string msg) : base(msg) { }
    }

    class OpenAIServiceNotFoundException : Exception
    {
        public OpenAIServiceNotFoundException(string msg) : base(msg) { }
    }

    class OpenAIChatMain
    {
        const string NewLine = "\r\n";

        const string OpenAIKeyEnvironmentVariableName = "OPENAI_KEY";
        static string? OpenAIKeyOverWriteVariable = null; // 直接APIの値を上書き指定している場合(マクロなどからの直接の引き渡し)
        const string ErrorMessageNoOpenAIKey = OpenAIKeyEnvironmentVariableName + "キーが環境変数にありません。:" + NewLine;

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

        const string ChatEndMessage = "チャットを終了";
        // 質問内容の取得
        static string? GetQuestion()
        {
            string? question = "";

            Console.Write(QuestionPromptMessages);
            question = Console.ReadLine();

            if (String.IsNullOrEmpty(question))
            {
                return null;
            }

            if (question.ToUpper() == ChatEndMessage)
            {
                return null;
            }

            return question;
        }

        const string ErrorMessageNoOpenAIService = "OpenAIのサービスに接続できません。:" + NewLine;
        // OpenAIサービスのインスタンス。一応保持
        static OpenAIService? openAiService = null;

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

        // OpenAIサービスのインスタンスのクリア。多分disconnectみたいなメソッドはない。
        static void ClearOpenAIService()
        {
            openAiService = null;
        }

        // OpenAIサービスのインスタンスのクリア。多分disconnectみたいなメソッドはない。
        static List<ChatMessage> messageList = new();

        const string ChatGPTStartSystemMessage = "You are a helpful assistant.";
        // チャットのエンジンやオプション。過去のチャット内容なども渡す。
        static IAsyncEnumerable<ChatCompletionCreateResponse> ReBuildPastChatContents()
        {
            var key = GetOpenAIKey();
            if (key == null)
            {
                throw new OpenAIKeyNotFoundException(ErrorMessageNoOpenAIKey);
            }

            openAiService = ConnectOpenAIService(key);
            if (openAiService == null)
            {
                throw new OpenAIServiceNotFoundException(ErrorMessageNoOpenAIService);
            }

            var list = new List<ChatMessage>();
            list.Add(ChatMessage.FromSystem(ChatGPTStartSystemMessage));

            foreach (var mes in messageList)
            {
                list.Add(mes);
            }

            var options = new ChatCompletionCreateRequest
            {
                Messages = list,
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 2000
            };

            var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(options);
            return completionResult;
        }


        const string AssistanceAnswerCompleteMsg = NewLine + "-- 完了 --" + NewLine;
        const string ErrorMsgUnknown = "Unknown Error:" + NewLine;
        // チャットの反復

        const string AssistanceAnswerCancelMsg = NewLine + "-- ChatGPTの回答を途中キャンセルしました --" + NewLine;
        public async Task AddAnswer(CancellationToken ct)
        {
            string answer_sum = "";
            var completionResult = ReBuildPastChatContents();

            await foreach (var completion in completionResult)
            {
                if (ct.IsCancellationRequested)
                {
                    await completionResult.GetAsyncEnumerator().DisposeAsync();
                    Hm.OutputPane.Output(AssistanceAnswerCancelMsg);
                    throw new OperationCanceledException(AssistanceAnswerCancelMsg);
                }

                // キャンセルされてたら OperationCanceledException を投げるメソッド
                ct.ThrowIfCancellationRequested();

                if (completion.Successful)
                {
                    string? str = completion.Choices.FirstOrDefault()?.Message.Content;
                    if (str != null)
                    {
                        str = str.Replace("\n", NewLine);
                        answer_sum += str ?? "";
                    }
                    Hm.OutputPane.Output(str);
                }
                else
                {
                    if (completion.Error == null)
                    {
                        throw new Exception(ErrorMsgUnknown);
                    }

                    Hm.OutputPane.Output($"{completion.Error.Code}: {completion.Error.Message}" + "\r\n");
                }
            }

            messageList.Add(ChatMessage.FromAssistant(answer_sum));
            Hm.OutputPane.Output(AssistanceAnswerCompleteMsg + "\r\n");
        }

        public OpenAIChatMain(string key="")
        {
            // とりあえず代入。エラーならChatGPTの方が言ってくれる。
            if (key.Length > 0)
            {
                OpenAIKeyOverWriteVariable = key;
            }
            GetOpenAIKey(); // かまし
        }

        public void AddQuestion(string question)
        {
            messageList.Add(ChatMessage.FromUser(question));
        }
    }
}

