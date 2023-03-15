using System.Runtime.InteropServices;

namespace HmOpenAIChatGpt35Turbo
{

    [ComVisible(true)]
    [Guid("BCCBE82C-56E1-4056-AE7C-3C4F62806732")]
    public class HmChatGpt35Turbo
    {
        private static AppForm? form;

        public int CreateForm(string key = "")
        {
            if (form == null || !form.Visible)
            {
                form = new AppForm(key);
            }

            form.Show();
            return 1;
        }

        // 秀丸のバージョンによって引数を渡してくるものと渡してこないものがあるので、デフォルト引数は必要。
        // (引数がないと、引数ミスマッチということで、呼び出し自体されない)
        public int DestroyForm(int result = 0)
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
