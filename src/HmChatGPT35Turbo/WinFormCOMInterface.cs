using HmNetCOM;
using System.IO.MemoryMappedFiles;
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

        private static MemoryMappedFile? share_mem;
        public long CreateForm(string key = "")
        {
            if (form != null)
            {
                form.UpdateTextBox();
            }

            if (form == null || !form.Visible)
            {
                CreateSharedMemory();

                output = new HmOutputWriter();
                input = new HmInputReader();
                form = new AppForm(key, output, input);
            }

            form.Show();
            return -1;
        }

        private void CreateSharedMemory()
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

        private long GetSharedMemory()
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

        private void DeleteSharedMemory()
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
            } catch(Exception) { }
        }

        public long GetOpenAIFormUsedHideamruHandle()
        {
            return GetSharedMemory();
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

            DeleteSharedMemory();

            return 1;
        }
    }
}
