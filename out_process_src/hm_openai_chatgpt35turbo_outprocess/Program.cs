
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using static OpenAI.GPT3.ObjectModels.Models;


class OpenAIKeyNotFoundException : KeyNotFoundException
{
    public OpenAIKeyNotFoundException(string msg) : base(msg) { }
}

class OpenAIServiceNotFoundException : Exception
{
    public OpenAIServiceNotFoundException(string msg) : base(msg) { }
}

class Program
{
    const string OpenAIEnvironmentVariableName = "OPENAI_KEY";
    const string ErrorMessageNoOpenAIKey = "キーが環境変数にありません。:\n";

    // OpenAIのキーの取得
    static string? GetOpenAIKey()
    {
        string? key = Environment.GetEnvironmentVariable(OpenAIEnvironmentVariableName);
        if (String.IsNullOrEmpty(key))
        {
            throw new OpenAIKeyNotFoundException("OPENAI_KEY");
        }
        return key;
    }


    const string QuestionPromptMessages = "-- 質問をどうぞ --\n";
    const string NoQuestionMessage = "質問内容が無い\n";

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

    const string ErrorMessageNoOpenAIService = "OpenAIのサービスに接続できません。:\n";
    // OpenAIサービスのインスタンス。一応保持
    static OpenAIService? openAiService = null;

    static OpenAIService? ConnectOpenAIService(string key)
    {
        try { 
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
            throw new OpenAIKeyNotFoundException("OPENAI_KEY");
        }

        openAiService = ConnectOpenAIService(key);
        if (openAiService == null)
        {
            throw new OpenAIServiceNotFoundException("OpenAIService");
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
            MaxTokens = 1000
        };

        var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(options);
        return completionResult;
    }

    const string AssistanceAnswerCompleteMsg = "\n\n-- 完了 --\n\n";
    const string ErrorMsgUnknown = "Unknown Error:\n";
    // チャットの反復
    static async Task RepeatChat()
    {
        while (true)
        {
            string? question = GetQuestion();
            if (question == null)
            {
                break;
            }
            Console.WriteLine("");
            messageList.Add(ChatMessage.FromUser(question));
            var completionResult = ReBuildPastChatContents();

            string answer_sum = "";
            await foreach (var completion in completionResult)
            {
                if (completion.Successful)
                {
                    string? str = completion.Choices.FirstOrDefault()?.Message.Content;
                    answer_sum += str ?? "";
                    Console.Write(str);
                }
                else
                {
                    if (completion.Error == null)
                    {
                        throw new Exception(ErrorMsgUnknown);
                    }

                    Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                    Console.Error.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                }
            }

            messageList.Add(ChatMessage.FromAssistance(answer_sum));
            Console.WriteLine(AssistanceAnswerCompleteMsg);
        }

    }

    // メイン
    public static async Task Main(String[] args)
    {
        try
        {
            GetOpenAIKey(); // かまし
            await RepeatChat();
        }
        catch(OpenAIKeyNotFoundException ex)
        {
            Console.WriteLine(ErrorMessageNoOpenAIKey + ex);
            Console.Error.WriteLine(ErrorMessageNoOpenAIKey + ex);
        }
        catch (OpenAIServiceNotFoundException ex)
        {
            Console.WriteLine(ErrorMessageNoOpenAIService + ex);
            Console.Error.WriteLine(ErrorMessageNoOpenAIService + ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ErrorMsgUnknown + ex);
            Console.Error.WriteLine(ErrorMsgUnknown + ex);
        }
    }
}


