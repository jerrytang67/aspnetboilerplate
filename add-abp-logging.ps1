# Script to add "using Abp.Logging;" for files that use NullLogger

$files = Get-ChildItem -Path ".\src",".\test" -Include *.cs -Recurse | Where-Object {
    $content = Get-Content $_.FullName -Raw
    ($content -match 'NullLogger\.Instance') -and ($content -notmatch 'using Abp\.Logging;')
}

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # Insert "using Abp.Logging;" after the last "using Microsoft.Extensions" line or at the top
    if ($content -match 'using Microsoft\.Extensions\.Logging;') {
        $newContent = $content -replace '(using Microsoft\.Extensions\.Logging;)', "`$1`nusing Abp.Logging;"
    } elseif ($content -match '(using [^;]+;)') {
        # Insert after the first using statement
        $newContent = $content -replace '(using [^;]+;)', "`$1`nusing Abp.Logging;"
    } else {
        continue
    }

    if ($content -ne $newContent) {
        $newContent | Set-Content $file.FullName -NoNewline
        Write-Host "Updated: $($file.FullName)"
        $count++
    }
}

Write-Host "`nTotal files updated: $count"
