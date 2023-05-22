# Solution AzureTools

This solution contains the following projects:

- AzureNetTools: Azure function project created with NET 6.
Function MissingArchiveCheck essentialy is a cron job executed daily run at `07:00:00 UTC`, and checks if there are any missing archives in the storage account.
If archive is missing it sends an email to the configured email address. The filename pattern should be `yyyyMMdd.tgz`

## Installation
Environment variables are used to configure the function. The following are used:
- **AzureWebJobsStorage**: connection string to the storage account
- **SendGrid_ApiKey**: api key for sendgrid
- **SendGrid_Email**: email address to send the email from
- **SendGrid_Sender_Address**: email address to be notified
- **SendGrid_Sender_Name**: name of the sender
- **CheckedContainers**: comma separated list of containers to check

## Contributing

- Fork the repo
- Create a branch (git checkout -b new-feature)
- Make changes
- Commit your changes (git commit -am 'Add new feature')
- Push to the branch (git push origin new-feature)
- Create a new Pull Request

## License

[MIT License](https://opensource.org/licenses/MIT)
