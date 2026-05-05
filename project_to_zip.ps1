# project_to_zip.ps1
#
# Extracts relevant files from the briko repository and packages them into a ZIP archive.
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\project_to_zip.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\project_to_zip.ps1 -SourceDir "C:\Users\hiroxpepe\Projects\briko" -OutZip "briko_export.zip"
#
# Parameters:
#   -SourceDir  : Repository root directory (default: same folder as this script)
#   -OutZip     : Output ZIP path (default: <parent>/briko_yyyyMMdd_HHmmss.zip)

Param(
    [string]$SourceDir = "",
    [string]$OutZip    = ""
)

# ── Resolve paths ─────────────────────────────────────────────────────────────
if (-not $SourceDir) {
    $SourceDir = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).ProviderPath }
}
$SourceDir = [System.IO.Path]::GetFullPath($SourceDir)

if (-not $OutZip) {
    $ts     = Get-Date -Format "yyyyMMdd_HHmmss"
    $OutZip = Join-Path (Split-Path $SourceDir -Parent) "briko_${ts}.zip"
}
$OutZip = [System.IO.Path]::GetFullPath($OutZip)

# ── Excluded directories by name (case-insensitive) ───────────────────────────
$excludeDirSet = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@(
        # .NET build artifacts
        'obj', 'bin',
        # VCS / IDE
        '.git', '.vs', '.vscode', '.idea',
        # Package managers
        'node_modules',
        # Unity auto-generated (if briko is ever opened as a Unity project)
        'Library', 'Temp', 'Logs', 'UserSettings'
    ),
    [System.StringComparer]::OrdinalIgnoreCase
)

# ── Excluded directories by relative path prefix (forward-slash, case-insensitive)
$excludePathPrefixes = @(
    # No project-specific path exclusions for briko
)

# ── Excluded file extensions (case-insensitive) ────────────────────────────────
$excludeExtSet = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@(
        '.meta', '.gitignore', '.gitmodules', '.gitkeep',
        '.code-workspace', '.vsconfig',
        '.png', '.jpg', '.jpeg',
        '.bin', '.xml'
    ),
    [System.StringComparer]::OrdinalIgnoreCase
)

# ── File collection (manual recursion with early directory pruning) ────────────
function Get-FilesToZip {
    Param(
        [string]$Root,
        [System.Collections.Generic.HashSet[string]]$ExcludeDirs,
        [string[]]$ExcludePaths,
        [System.Collections.Generic.HashSet[string]]$ExcludeExts,
        [string]$ZipPathAbs
    )

    $stack     = [System.Collections.Generic.Stack[string]]::new()
    $collected = [System.Collections.Generic.List[string]]::new()
    $stack.Push($Root)

    while ($stack.Count -gt 0) {
        $dir = $stack.Pop()
        try {
            $entries = [System.IO.Directory]::EnumerateFileSystemEntries($dir)
        } catch { continue }

        foreach ($entry in $entries) {
            $name = [System.IO.Path]::GetFileName($entry)

            if ([System.IO.Directory]::Exists($entry)) {
                # Skip by directory name
                if ($ExcludeDirs.Contains($name)) { continue }

                # Skip by relative path prefix
                $relDir    = $entry.Substring($Root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
                $relDirFwd = $relDir.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
                $skipPath  = $false
                foreach ($prefix in $ExcludePaths) {
                    if ($relDirFwd -ieq $prefix -or
                        $relDirFwd.StartsWith($prefix + '/', [System.StringComparison]::OrdinalIgnoreCase)) {
                        $skipPath = $true; break
                    }
                }
                if ($skipPath) { continue }

                # Skip empty directories
                $firstChild = [System.IO.Directory]::EnumerateFileSystemEntries($entry) |
                              Select-Object -First 1
                if (-not $firstChild) { continue }

                $stack.Push($entry)
            } else {
                # Never include the output ZIP itself
                if ([string]::Equals($entry, $ZipPathAbs, [System.StringComparison]::OrdinalIgnoreCase)) { continue }

                # Skip excluded extensions
                $ext = [System.IO.Path]::GetExtension($name)
                if ($ExcludeExts.Contains($ext)) { continue }

                $collected.Add($entry)
            }
        }
    }

    return $collected
}

# ── Main ──────────────────────────────────────────────────────────────────────

$OutLog    = [System.IO.Path]::ChangeExtension($OutZip, '.log')
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$logLines  = [System.Collections.Generic.List[string]]::new()

function Log {
    Param([string]$Line, [string]$Color = "")
    $logLines.Add($Line)
    if ($Color) { Write-Host $Line -ForegroundColor $Color } else { Write-Host $Line }
}

Log "================================================================" Cyan
Log "  briko_to_zip.ps1" Cyan
Log "================================================================" Cyan
Log "  Source : $SourceDir"
Log "  Output : $OutZip"
Log "  Log    : $OutLog"
Log "================================================================"
Log ""

# Remove existing ZIP if present
if (Test-Path $OutZip) {
    Remove-Item $OutZip -Force
    Log "  Removed existing ZIP: $OutZip" Yellow
}

# Collect file list
Log "  Scanning files..." Cyan
$files = Get-FilesToZip `
    -Root         $SourceDir `
    -ExcludeDirs  $excludeDirSet `
    -ExcludePaths $excludePathPrefixes `
    -ExcludeExts  $excludeExtSet `
    -ZipPathAbs   $OutZip
$files = $files | Sort-Object
Log "  Scan complete: $($files.Count) files found"
Log ""

# Add files to ZIP
Add-Type -Assembly System.IO.Compression
Add-Type -Assembly System.IO.Compression.FileSystem

$archive   = [System.IO.Compression.ZipFile]::Open($OutZip, [System.IO.Compression.ZipArchiveMode]::Create)
$fileCount = 0
$skipCount = 0

$categoryCounts = @{}

foreach ($filePath in $files) {
    $rel       = $filePath.Substring($SourceDir.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
    $entryName = $rel.Replace([System.IO.Path]::DirectorySeparatorChar, '/')

    $topSegment = $entryName.Split('/')[0]
    if (-not $categoryCounts.ContainsKey($topSegment)) {
        $categoryCounts[$topSegment] = 0
    }

    try {
        $entry       = $archive.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
        $entryStream = $entry.Open()
        $fileStream  = [System.IO.File]::OpenRead($filePath)
        $fileStream.CopyTo($entryStream)
        $fileStream.Dispose()
        $entryStream.Dispose()

        $logLines.Add("  + $entryName")
        Write-Host "  + $entryName"
        $categoryCounts[$topSegment]++
        $fileCount++
    } catch {
        $logLines.Add("  WARN: skipped $rel")
        $logLines.Add("        reason: $_")
        Write-Host "  WARN: skipped $rel" -ForegroundColor Yellow
        Write-Host "        reason: $_"   -ForegroundColor DarkYellow
        $skipCount++
    }
}

$archive.Dispose()

# ── Summary ───────────────────────────────────────────────────────────────────
Log ""
Log "================================================================" Green
Log "  Done!" Green
Log "================================================================" Green
Log "  Files included : $fileCount"
if ($skipCount -gt 0) { Log "  Files skipped  : $skipCount" Yellow }
Log ""
Log "  Breakdown by category:" Cyan
foreach ($key in ($categoryCounts.Keys | Sort-Object)) {
    Log ("    {0,-35} {1,4} files" -f $key, $categoryCounts[$key])
}
Log ""
if (Test-Path $OutZip) {
    $zipSizeMB = [math]::Round((Get-Item $OutZip).Length / 1MB, 2)
    Log "  ZIP  : $OutZip ($zipSizeMB MB)"
} else {
    Log "  ZIP  : (not created — all files were skipped)" Yellow
}
Log "  Log  : $OutLog"
Log "================================================================"

[System.IO.File]::WriteAllLines($OutLog, $logLines, $utf8NoBom)
