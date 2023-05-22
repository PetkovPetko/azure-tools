using System;

namespace AzureNetTools
{
    internal static class Settings
    {
        public static string Connection => Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        
        public static string[] Containers => Environment.GetEnvironmentVariable("CheckedContainers").Split(",");

        public static class SendGrid
        {
            public static string ApiKey => Environment.GetEnvironmentVariable("SendGrid_ApiKey");
            
            public static class Sender
            {
                public static string Address => Environment.GetEnvironmentVariable("SendGrid_Sender_Address");
                public static string Name => Environment.GetEnvironmentVariable("SendGrid_Sender_Name");
            }
        }
    }
}
