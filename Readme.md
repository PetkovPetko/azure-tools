# Solution AzureTools

This solution contains the following projects:

- AzureNetTools: Azure function project created with NET 6.
Function MissingArchiveCheck essentially is a cron job executed daily run at `07:00:00 UTC`, and checks if there are any missing archives in the storage account.
If archive is missing it sends an email to the configured email address. The filename pattern should be `yyyyMMdd.tgz`

## Installation
Environment variables are used to configure the function. The following are used:
- **AzureStorageConnections**: connection strings to the storage accounts separated by '||'
- **AzureWebJobsStorage**: required to execute for timerTrigger function (storage connection string or "UseDevelopmentStorage=true")
- **CheckedContainers**: name of the container to check for uploaded backup
- **TEAMS_WEBHOOK_URL**: teams channel, where a post will be made if missing a backup file

## Contributing

- Fork the repo
- Create a branch (git checkout -b new-feature)
- Make changes
- Commit your changes (git commit -am 'Add new feature')
- Push to the branch (git push origin new-feature)
- Create a new Pull Request

## License

[MIT License](https://opensource.org/licenses/MIT)
