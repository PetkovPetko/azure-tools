using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AzureNetTools
{
    public class MissingArchiveCheck
    {
        [FunctionName("MissingArchiveCheck")]
        public async Task Run([TimerTrigger("0 0 7 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            CheckIfConteinersExists(log);

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

        private void CheckIfConteinersExists(ILogger log)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Settings.Connection);
            var missingContainer = false;

            foreach (var container in Settings.Containers)
            {
                var containerExists = blobServiceClient.GetBlobContainers().Any(s => s.Name.Equals(container, StringComparison.OrdinalIgnoreCase));
                if (!containerExists)
                {
                    missingContainer = true;
                    log.LogError($"Container {container} does not exist for storage: {blobServiceClient.AccountName}");
                }
            }

            if (missingContainer)
            {
                throw new Exception("Missing containers, check logs for more information");
            }
        }

        private bool DoesArchiveExist(string containerName, string fileName, ILogger log)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Settings.Connection);
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(fileName);
            var exists = blob.Exists();
            if (!exists)
            {
                log.LogInformation($"{containerName} does not exist");
            }
            return exists;
        }
        private async Task ProcessMissingArchive(string yesterdayFileName, string container)
        {
            await SendWithSendGrid(yesterdayFileName, container);
        }

        private async Task SendWithSendGrid(string yesterdayFileName, string container)
        {
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(Settings.SendGrid.Sender.Address, Settings.SendGrid.Sender.Name),
                Subject = $"Warning: Missing azure archive",
                PlainTextContent = $"The archive file with name '{yesterdayFileName}' is missing from azure container: {container}.",
            };

            msg.AddTo(new EmailAddress(Settings.SendGrid.Sender.Address, string.Empty));

            SendGridClient client = new SendGridClient(Settings.SendGrid.ApiKey);

            await client.SendEmailAsync(msg);
        }
    }
}
