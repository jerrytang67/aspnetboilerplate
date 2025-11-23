# Script to replace Castle.Core.Logging with Microsoft.Extensions.Logging

$files = Get-ChildItem -Path ".\src",".\test" -Include *.cs -Recurse | Where-Object { (Get-Content $_.FullName -Raw) -match 'using Castle\.Core\.Logging;' }

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $newContent = $content -replace 'using Castle\.Core\.Logging;', 'using Microsoft.Extensions.Logging;'

    if ($content -ne $newContent) {
        $newContent | Set-Content $file.FullName -NoNewline
        Write-Host "Updated: $($file.FullName)"
        $count++
    }
}

Write-Host "`nTotal files updated: $count"
