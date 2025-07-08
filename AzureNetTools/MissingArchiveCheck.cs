using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureNetTools;

public class MissingArchiveCheck
{
    [Function(nameof(MissingArchiveCheck))]
    public async Task RunAsync(
        [TimerTrigger("0 0 10 * * *")] TimerInfo myTimer,
        FunctionContext context)
    {
        var log = context.GetLogger(nameof(MissingArchiveCheck));

        TimeZoneInfo sofiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sofiaTimeZone);

        string message = $"C# Timer trigger function executed at: {localTime}";
        log.LogInformation(message);

        var yesterdayFileName = $"{localTime.AddDays(-1).ToString("yyyyMMdd")}.tgz";
        var containerName = Settings.Container;
        List<Section> sections = new();

        foreach (var connection in Settings.Connections)
        { 
            var defaultCredentials = new DefaultAzureCredential();
            var blobServiceUri = new Uri(connection);
            
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobServiceUri, defaultCredentials);

            CheckIfContainersExists(blobServiceClient, log, containerName);

            var exists = DoesArchiveExist(blobServiceClient, containerName, yesterdayFileName, log);
            if (!exists)
            {
                sections.Add(this.CreateCardSection(yesterdayFileName, containerName, blobServiceClient.AccountName));
            }
        }

        if (sections.Any())
        {
            log.LogInformation("Posting to Teams. Sections = {0}", sections.Count);

            await CreateTeamsCard(sections, localTime, log);
        }
    }

    private void CheckIfContainersExists(BlobServiceClient blobServiceClient, ILogger log, string container)
    {
        var missingContainer = false;

        var containerExists = blobServiceClient.GetBlobContainers().Any(s => s.Name.Equals(container, StringComparison.OrdinalIgnoreCase));
        if (!containerExists)
        {
            missingContainer = true;
            log.LogError($"Container {container} does not exist for storage: {blobServiceClient.AccountName}");
        }

        if (missingContainer)
        {
            throw new Exception("Missing containers, check logs for more information");
        }
    }

    private bool DoesArchiveExist(BlobServiceClient blobServiceClient, string containerName, string fileName, ILogger log)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        return blobClient.Exists();
    }

    private Section CreateCardSection(string yesterdayFileName, string container, string storageAccountName)
    {
        var facts = SetFacts(yesterdayFileName, container, storageAccountName);

        var section = new Section
        {
            StartGroup = true,
            Title = "Storage information",
            Facts = facts
        };

        return section;
    }

    private List<Fact> SetFacts(string yesterdayFileName, string container, string storageAccountName)
    {
        var facts = new List<Fact>
        {
            new Fact
            {
                Name = "Storage account",
                Value = storageAccountName
            },
            new Fact
            {
                Name = "Container",
                Value = container
            },
            new Fact
            {
                Name = "Expected file",
                Value = yesterdayFileName
            }
        };

        return facts;
    }

    private async Task CreateTeamsCard(List<Section> sections, DateTime localTime, ILogger log)
    {
        var card = new TeamsCard
        {
            Type = "MessageCard",
            Context = "http://schema.org/extensions",
            ThemeColor = "00FF00",
            Title = "Missing Azure backup",
            Text = $"*Missing backup for {localTime.AddDays(-1).ToString("dd.MM.yyyy")}*",
            Sections = sections
        };

        var jsonTemplate = JsonConvert.SerializeObject(card);

        var client = new HttpClient();
        var content = new StringContent(jsonTemplate, Encoding.UTF8, "application/json");

        var resp = await client.PostAsync(Settings.WebhookUrl, content);

        log.LogInformation("Teams status: {0}", resp.StatusCode);
        resp.EnsureSuccessStatusCode();
    }
}
