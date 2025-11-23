# Clean up duplicate using Microsoft.Extensions.Logging statements

$files = Get-ChildItem -Path ".\src",".\test" -Include *.cs -Recurse | Where-Object {
    $content = Get-Content $_.FullName -Raw
    $content -match 'using Microsoft\.Extensions\.Logging;'
}

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Remove all instances of "using Microsoft.Extensions.Logging;" first
    $content = $content -replace 'using Microsoft\.Extensions\.Logging;\r?\n', ''

    # Remove incorrectly placed using statements (in the middle of code)
    # This regex removes lines that have just "using Microsoft.Extensions.Logging;" standalone
    $content = $content -replace '^\s*using Microsoft\.Extensions\.Logging;\s*$', '' -replace '\n\n\n+', "`n`n"

    # Add back one single using statement at the top, after System usings
    if ($content -match '(using System[^;]*;)') {
        # Find the last System.* using statement and add after it
        $content = $content -replace '(using System[^;]*;\r?\n)((?!using System))', "`$1using Microsoft.Extensions.Logging;`n`$2"
    }
    elseif ($content -match '(namespace [^{]+\{)') {
        # If no System usings, add before namespace
        $content = $content -replace '(namespace [^{]+\{)', "using Microsoft.Extensions.Logging;`n`n`$1"
    }

    if ($content -ne $originalContent) {
        $content | Set-Content $file.FullName -NoNewline
        Write-Host "Updated: $($file.FullName)"
        $count++
    }
}

Write-Host "`nTotal files updated: $count"
