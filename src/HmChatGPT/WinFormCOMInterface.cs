﻿using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using HmNetCOM;

namespace HmOpenAIChatGpt;


[ComVisible(true)]
[Guid("BCCBE82C-56E1-4056-AE7C-3C4F62806732")]
public class HmChatGPT
{
    private static AppForm? form;
    private static HmOutputWriter? output;
    private static HmInputReader? input;

    HmChatGPTSharedMemory sm = new HmChatGPTSharedMemory();

    public long CreateForm(string key = "", string model = "", int maxtokens = 2000, int topmost=0, int remove_auto_messagelist = 1)
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
                form = new AppForm(key, model, maxtokens, topmost, remove_auto_messagelist, input, output);

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
public class HmChatGPTSharedMemory
{
    private static MemoryMappedFile? share_mem;

    public void CreateSharedMemory()
    {
        try
        {
            // 新規にメモリマップを作成して、そこに現在の秀丸ハンドルを数値として入れておく
            if (share_mem == null)
            {
                share_mem = MemoryMappedFile.CreateNew("HmChatGPTSharedMem", 8);
            }
        }
        catch (Exception e)
        {
        }

        try
        {
            using (var share_mem = MemoryMappedFile.OpenExisting("HmChatGPTSharedMem"))
            {
                if (share_mem != null)
                {
                    using (var accessor = share_mem.CreateViewAccessor())
                    {
                        accessor.Write(0, (long)Hm.WindowHandle);
                    }
                }
            }
        }
        catch (Exception e)
        {
        }
    }

    public long GetSharedMemory()
    {
        long value = 0;
        try
        {
            using (var share_mem = MemoryMappedFile.OpenExisting("HmChatGPTSharedMem"))
            {
                using (var accessor = share_mem.CreateViewAccessor())
                {
                    value = accessor.ReadInt64(0);
                }
            }
        }
        catch (Exception)
        {
        }

        return value;
    }

    public void DeleteSharedMemory()
    {
        try
        {
            if (share_mem != null)
            {
                // メモリマップを削除。
                using (var accessor = share_mem.CreateViewAccessor())
                {
                    accessor.Write(0, (long)0);
                }
            }
        }
        catch (Exception)
        {
        }

        try
        {
            if (share_mem != null)
            {
                share_mem.Dispose();
                share_mem = null;
            }
        }
        catch (Exception)
        {
        }
    }


    public long GetFormHideamruHandle()
    {
        return GetSharedMemory();
    }
}
