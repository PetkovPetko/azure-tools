using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Newtonsoft.Json;

namespace AzureNetTools
{
    public class MissingArchiveCheck
    {
        [FunctionName("MissingArchiveCheck")]
        public async Task Run(
            [TimerTrigger("0 0 10 * * *")] TimerInfo myTimer,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var yesterdayFileName = $"{DateTime.Now.AddDays(-1).ToString("yyyyMMdd")}.tgz";
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
                await CreateTeamsCard(sections);
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

        private async Task CreateTeamsCard(List<Section> sections)
        {
            var card = new TeamsCard
            {
                Type = "MessageCard",
                Context = "http://schema.org/extensions",
                ThemeColor = "00FF00",
                Title = "Missing Azure backup",
                Text = $"*Missing backup for {DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")}*",
                Sections = sections
            };

            var jsonTemplate = JsonConvert.SerializeObject(card);

            var client = new HttpClient();
            var content = new StringContent(jsonTemplate, Encoding.UTF8, "application/json");

            await client.PostAsync(Settings.WebhookUrl, content);
        }
    }
}
