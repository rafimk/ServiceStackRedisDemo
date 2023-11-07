namespace ServiceStackRedisDemo.Api.Models;

public class UserData
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string TimeZoneId { get; set; } // Time zone identifier, e.g., "America/New_York"

    public UserData(string userId, string userName, string email, string timeZoneId)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
        TimeZoneId = timeZoneId;
    }
}