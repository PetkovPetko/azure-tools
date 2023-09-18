using System;

namespace AzureNetTools
{
    internal static class Settings
    {
        public static string[] Connections => Environment.GetEnvironmentVariable("AzureStorageConnections").Split("||");

        public static string Container => Environment.GetEnvironmentVariable("CheckedContainers");

        public static string WebhookUrl => Environment.GetEnvironmentVariable("TEAMS_WEBHOOK_URL");
    }
}
