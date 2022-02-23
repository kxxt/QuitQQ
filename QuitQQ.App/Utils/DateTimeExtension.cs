namespace QuitQQ.App.Utils;

internal static class DateTimeExtension
{
    public static DateTime UnixTimeStampToBeijingDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime().AddHours(8); // Beijing Time
        return dateTime;
    }
}

