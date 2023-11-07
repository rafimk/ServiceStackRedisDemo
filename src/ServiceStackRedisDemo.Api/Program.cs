using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using ServiceStackRedisDemo.Api.Models;
using StackExchange.Redis;
using System;

class Program
{
    static IConfigurationRoot configuration;

    static void Main()
    {
        // Load configuration from appsettings.json or any other source
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Initialize a timer to run the job at midnight
        var midnight = GetNextMidnight();
        // var timer = new Timer(TriggerJob, null, midnight - DateTime.Now, TimeSpan.FromHours(24));
        RemoveResisData();
        TriggerJob();

        Console.WriteLine("-------------------------------------------");
        RetreiveData(12);
        Console.WriteLine("-------------------------------------------");
        RetreiveData(16);
        Console.WriteLine("-------------------------------------------");
        // Keep the application running
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    static DateTime GetNextMidnight()
    {
        var now = DateTime.Now;
        var nextMidnight = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
        return nextMidnight;
    }

    static void TriggerJob()
    {
        Console.WriteLine("Triggering the job...");

        // Retrieve email templates from the database
        List<EmailTemplate> emailTemplates = RetrieveEmailTemplates();

        // Collect user data from the user table
        List<UserData> userData = CollectUserData();

        // Store the data in Redis cache based on hourly slots
        StoreDataInRedis(emailTemplates, userData);

        Console.WriteLine("Job completed.");
    }

    static List<EmailTemplate> RetrieveEmailTemplates()
    {
        return new List<EmailTemplate>
        {
            new EmailTemplate("WelcomeEmail", "Welcome to Our Service", "Dear User, welcome to our service!"),
            new EmailTemplate("PromotionEmail", "Special Promotion", "Hello User, check out our latest promotion!"),
            new EmailTemplate("ReminderEmail", "Don't Forget!", "Hi User, this is a reminder for your upcoming event.")
        };
    }

    static List<UserData> CollectUserData()
    {
        return new List<UserData> 
        {
            new UserData("user001", "Alice", "alice@example.com", "America/New_York"),
            new UserData("user002", "Bob", "bob@example.com", "Europe/London"),
            new UserData("user003", "Charly", "charly@example.com", "America/New_York"),
            new UserData("user004", "Moly", "moly@example.com", "Europe/London"),
        };
    }

    static void StoreDataInRedis(List<EmailTemplate> emailTemplates, List<UserData> userData)
    {
        // Connect to the Redis server
        using (var redisConnection = ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisConnection")))
        {
            var redisDatabase = redisConnection.GetDatabase();

            // Calculate the current hour
            var currentHour = DateTime.Now.Hour;

            var groupedUsers = userData.GroupBy(user => user.TimeZoneId);

            // Iterate through the grouped data
            foreach (var group in groupedUsers)
            {
               
                var slot = CalculateHourlySlot(currentHour, group.Key);
                var key = $"Slot:{slot}";
                Console.WriteLine($"TimeZone: {group.Key} : Slot : {key} ");

                // Serialize user data

                foreach (var user in group)
                {
                    Console.WriteLine($"User: {user.UserId}, Name: {user.UserName}, Email: {user.Email}");
                    var userData1Json = JsonConvert.SerializeObject(user);
                    redisDatabase.ListLeftPush(key, userData1Json);
                }
            }

            //foreach (var user in userData)
            //{
            //    // Calculate the time difference between the user's local time and 8:00 AM
            //    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
            //    var userTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
            //    var timeDifference = (int)(userTime.TimeOfDay.TotalHours - 8.0);

                //    // Calculate the slot based on the user's local time
                //    var slot = (currentHour + timeDifference) % 24;

                //    // Serialize and store the data in the Redis cache
                //    var key = $"Slot:{slot}";
                //    var value = new { EmailTemplate = emailTemplates, UserData = user };
                //    var jsonValue = JsonConvert.SerializeObject(value);
                //    redisDatabase.StringSet(key, jsonValue);
                //}
        }
    }

    static void RetreiveData(int slot)
    {
        using (var redisConnection = ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisConnection")))
        {
            var redisDatabase = redisConnection.GetDatabase();
            var key = $"Slot:{slot}";

            var storedUserData = redisDatabase.ListRange(key, 0, -1);

            foreach (var data in storedUserData)
            {
                var user = JsonConvert.DeserializeObject<UserData>(data);
                Console.WriteLine($" Slot : {key} User: {user.UserId}, Name: {user.UserName}, Time Zone: {user.TimeZoneId}");
            }
        }
    }

    static void RemoveResisData()
    {
        using (var redisConnection = ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisConnection")))
        {
            var redisDatabase = redisConnection.GetDatabase();
            string keyPattern = "Slot:*";

            // Use the Keys method to retrieve keys that match the pattern
            var keysToDelete = redisDatabase.Multiplexer.GetServer(redisDatabase.Multiplexer.GetEndPoints().First()).Keys(pattern: keyPattern);

            // Use the Delete method to delete the matched keys
            foreach (var key in keysToDelete)
            {
                redisDatabase.KeyDelete(key);
                Console.WriteLine($"Deleted key: {key}");
            }

        }
    }

    static int CalculateHourlySlot(int currentHour, string timeZoneId)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var userTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
        var timeDifference = (int)(userTime.TimeOfDay.TotalHours - 8.0);

        // Calculate the slot based on the user's local time
        var slot = (currentHour + timeDifference) % 24;
        return slot;
    }
}



