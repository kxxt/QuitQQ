using Mirai.Net.Sessions.Http.Managers;

namespace QuitQQ.App.Utils;

internal static class FileManagerExtension
{
    public static async Task<Mirai.Net.Data.Shared.File> GetFileInfoWithRetriesAsync(string groupId, string fileId)
    {
        int cnt = 5;
        while (cnt-- > 0)
        {
            try
            {
                return await FileManager.GetFileAsync(groupId, fileId, true);
            }
            catch
            {
                if (cnt <= 0) throw;
            }
        }

        throw new Exception("This place is thought to be unreachable!");
    }
}

