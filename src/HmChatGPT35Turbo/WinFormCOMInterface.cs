using HmNetCOM;
using System.Runtime.InteropServices;
using System.Threading;

namespace HmOpenAIChatGpt35Turbo
{

    [ComVisible(true)]
    [Guid("BCCBE82C-56E1-4056-AE7C-3C4F62806732")]
    public class HmChatGPT35Turbo
    {
        private static AppForm? form;
        private static HmOutputWriter? output;
        private static HmInputReader? input;

        private long OpeningFormHidemaruHandle()
        {
            string opening_hidemaruhandle = Hm.Macro.StaticVar["HmOpenAIChatGpt35Turbo_HidemaruHandle", 1];
            long result = -1;
            if (long.TryParse(opening_hidemaruhandle, out result))
            {
                // 有効なハンドルが入っている
                if (result > 0)
                {
                    // 現在の秀丸のハンドル異なる。
                    if (result != (long)Hm.WindowHandle)
                    {
                        return result;
                    }
                }
            }
            return -1;
        }
        public long CreateForm(string key = "")
        {
            long hidemaruhandle = OpeningFormHidemaruHandle();
            if (hidemaruhandle != -1)
            {
                return hidemaruhandle;
            }

            if (form != null)
            {
                form.UpdateTextBox();
            }

            if (form == null || !form.Visible)
            {
                output = new HmOutputWriter();
                input = new HmInputReader();
                form = new AppForm(key, output, input);
            }

            form.Show();
            return -1;
        }

        // 秀丸のバージョンによって引数を渡してくるものと渡してこないものがあるので、デフォルト引数は必要。
        // (引数がないと、引数ミスマッチということで、呼び出し自体されない)
        public long DestroyForm(int result = 0)
        {
            if (form != null)
            {
                form.Close();
                form = null;
            }

            return 1;
        }
    }
}
