using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using HmNetCOM;

namespace HmOpenAIChatGpt35Turbo
{

    [ComVisible(true)]
    [Guid("BCCBE82C-56E1-4056-AE7C-3C4F62806732")]
    public class HmChatGPT35Turbo
    {
        private static AppForm? form;
        private static HmOutputWriter? output;
        private static HmInputReader? input;

        HmChatGPT35TurboSharedMemory sm = new HmChatGPT35TurboSharedMemory();

        public long CreateForm(string key = "", string model="")
        {
            try
            {
                if (form != null)
                {
                    form.UpdateTextBox();
                }

                if (form == null || !form.Visible)
                {
                    output = new HmOutputWriter();
                    input = new HmInputReader();
                    form = new AppForm(key, model, output, input);

                    sm.CreateSharedMemory();
                }

                form.Show();

                // フォームを前に持ってくるだけ
                form.BringToFront();
            }
            catch (Exception ex)
            {
                string err = ex.Message + "\r\n" + ex.StackTrace;
                output?.WriteLine(err);
            }
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

                sm.DeleteSharedMemory();
            }

            return 1;
        }
    }

    [ComVisible(true)]
    [Guid("9818F69E-A37D-4A03-BCA1-C4C172366473")]
    public class HmChatGPT35TurboSharedMemory
    {
        private static MemoryMappedFile? share_mem;

        public void CreateSharedMemory()
        {
            try
            {
                // 新規にメモリマップを作成して、そこに現在の秀丸ハンドルを数値として入れておく
                share_mem = MemoryMappedFile.CreateNew("HmChatGPT35TurboSharedMem", 8);
                MemoryMappedViewAccessor accessor = share_mem.CreateViewAccessor();
                accessor.Write(0, (long)Hm.WindowHandle);
                accessor.Dispose();
            }
            catch (Exception) { }
        }

        public long GetSharedMemory()
        {
            long value = 0;
            try
            {
                // (主に)違うプロセスからメモリマップの数値を読み込む
                share_mem = MemoryMappedFile.OpenExisting("HmChatGPT35TurboSharedMem");
                MemoryMappedViewAccessor accessor = share_mem.CreateViewAccessor();
                value = accessor.ReadInt64(0);
                accessor.Dispose();
            }
            catch (Exception) { }

            return value;
        }

        public void DeleteSharedMemory()
        {
            try
            {
                if (share_mem != null)
                {
                    // メモリマップを削除。
                    MemoryMappedViewAccessor accessor = share_mem.CreateViewAccessor();
                    accessor.Write(0, (long)0);
                    accessor.Dispose();
                    share_mem.Dispose();
                    share_mem = null;
                }
            }
            catch (Exception) { }
        }


        public long GetFormHideamruHandle()
        {
            return GetSharedMemory();
        }
    }

}
