# Script to replace remaining Castle logging method calls

$files = Get-ChildItem -Path ".\src",".\test" -Include *.cs -Recurse | Where-Object {
    $content = Get-Content $_.FullName -Raw
    $content -match '\.(Info|Warn|Debug|Error|Fatal|InfoFormat|WarnFormat|DebugFormat|ErrorFormat|FatalFormat|Create)\('
}

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Replace Castle logging methods with Microsoft equivalents
    $content = $content -replace '\.Info\(', '.LogInformation('
    $content = $content -replace '\.Warn\(', '.LogWarning('
    $content = $content -replace '\.Debug\(', '.LogDebug('
    $content = $content -replace '\.Error\(', '.LogError('
    $content = $content -replace '\.Fatal\(', '.LogCritical('

    # Format method calls
    $content = $content -replace '\.InfoFormat\(', '.LogInformation('
    $content = $content -replace '\.WarnFormat\(', '.LogWarning('
    $content = $content -replace '\.DebugFormat\(', '.LogDebug('
    $content = $content -replace '\.ErrorFormat\(', '.LogError('
    $content = $content -replace '\.FatalFormat\(', '.LogCritical('

    # Replace ILoggerFactory.Create() with CreateLogger() - be careful to not replace other Create() calls
    $content = $content -replace 'ILoggerFactory\)\.Create\(', 'ILoggerFactory).CreateLogger('
    $content = $content -replace 'loggerFactory\.Create\(', 'loggerFactory.CreateLogger('
    $content = $content -replace 'LoggerFactory\.Create\(', 'LoggerFactory.CreateLogger('
    $content = $content -replace '_loggerFactory\.Create\(', '_loggerFactory.CreateLogger('

    # Replace IsXxxEnabled properties
    $content = $content -replace '\.IsDebugEnabled', '.IsEnabled(LogLevel.Debug)'
    $content = $content -replace '\.IsInfoEnabled', '.IsEnabled(LogLevel.Information)'
    $content = $content -replace '\.IsWarnEnabled', '.IsEnabled(LogLevel.Warning)'
    $content = $content -replace '\.IsErrorEnabled', '.IsEnabled(LogLevel.Error)'
    $content = $content -replace '\.IsFatalEnabled', '.IsEnabled(LogLevel.Critical)'

    if ($content -ne $originalContent) {
        $content | Set-Content $file.FullName -NoNewline
        Write-Host "Updated: $($file.FullName)"
        $count++
    }
}

Write-Host "`nTotal files updated: $count"
