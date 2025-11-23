# Add missing Microsoft.Extensions.Logging using statements

$filesToCheck = @(
    ".\src\Abp\Threading\BackgroundWorkers\PeriodicBackgroundWorkerBase.cs",
    ".\src\Abp\Threading\BackgroundWorkers\AsyncPeriodicBackgroundWorkerBase.cs",
    ".\src\Abp\Webhooks\BackgroundWorker\WebhookSenderJob.cs",
    ".\src\Abp\Webhooks\DefaultWebhookSender.cs",
    ".\src\Abp\Notifications\DefaultNotificationDistributer.cs",
    ".\src\Abp\Configuration\DefaultConfigSettingStore.cs",
    ".\src\Abp\BackgroundJobs\BackgroundJobManager.cs",
    ".\src\Abp\Localization\Sources\Resource\ResourceFileLocalizationSource.cs",
    ".\src\Abp\Logging\LogHelper.cs",
    ".\src\Abp\Localization\Dictionaries\DictionaryBasedLocalizationSource.cs"
)

$count = 0
foreach ($filePath in $filesToCheck) {
    if (Test-Path $filePath) {
        $content = Get-Content $filePath -Raw

        if ($content -notmatch 'using Microsoft\.Extensions\.Logging;') {
            # Add the using statement after the first using statement
            $content = $content -replace '(using [^;]+;)', "`$1`nusing Microsoft.Extensions.Logging;"
            $content | Set-Content $filePath -NoNewline
            Write-Host "Updated: $filePath"
            $count++
        }
    }
}

Write-Host "`nTotal files updated: $count"
