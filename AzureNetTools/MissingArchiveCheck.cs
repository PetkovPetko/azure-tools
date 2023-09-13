using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AzureNetTools
{
    public class MissingArchiveCheck
    {
        [FunctionName("MissingArchiveCheck")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var yesterdayFileName = $"{DateTime.Now.AddDays(-1).ToString("yyyyMMdd")}.tgz";
            var container = Settings.Container;
            List<Section> sections = new();

            foreach (var connection in Settings.Connections)
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connection.Trim());

                CheckIfContainersExists(blobServiceClient, log, container);

                var exists = DoesArchiveExist(blobServiceClient, container, yesterdayFileName, log);
                if (!exists)
                {
                    sections.Add(this.CreateCardSection(yesterdayFileName, container, blobServiceClient.AccountName));
                }
            }

            if (sections.Any())
            {
                await CreateTeamsCard(sections);
            }
        }

        /*
        [FunctionName("MissingArchiveCheck")]
        public async Task Run([TimerTrigger("0 0 7 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            CheckIfContainersExists(log);

            var yesterdayFileName = $"{DateTime.Now.AddDays(-1).ToString("yyyyMMdd")}.tgz";

            foreach (var container in Settings.Containers)
            {
                var exists = DoesArchiveExist(container, yesterdayFileName, log);
                if (!exists)
                {
                    await ProcessMissingArchive(yesterdayFileName, container);
                }
            }
        }
        */

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
            return false;
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(fileName);
            var exists = blob.Exists();
            if (!exists)
            {
                log.LogInformation($"{containerName} does not exist");
            }
            return exists;
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
                    Name = "Missing file",
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
                Text = $"*Missing backup for {DateTime.Now.AddDays(-1).ToString("dd:MM:yyyy")}*",
                Sections = sections
            };

            var jsonTemplate = JsonConvert.SerializeObject(card);

            var client = new HttpClient();
            var content = new StringContent(jsonTemplate, Encoding.UTF8, "application/json");

            await client.PostAsync(Settings.WebhookUrl, content);
        }
    }
}
