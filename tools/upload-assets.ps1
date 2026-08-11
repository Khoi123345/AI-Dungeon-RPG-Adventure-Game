<#
.SYNOPSIS
    Upload game assets len AWS S3.
    Bo qua tat ca .meta files cua Unity.

.PARAMETER BucketName
    Ten S3 bucket. Neu khong truyen, doc tu env var ASSETS_BUCKET_NAME.

.PARAMETER Profile
    AWS CLI profile name (mac dinh: default).

.PARAMETER Region
    AWS region (mac dinh: ap-southeast-1).

.PARAMETER DryRun
    Neu set, chi in ra lenh se chay ma khong upload.

.EXAMPLE
    .\upload-assets.ps1 -BucketName "game-assets-rpg-569278273174"
    .\upload-assets.ps1 -BucketName "game-assets-rpg-569278273174" -DryRun
#>

param(
    [string]$BucketName = $env:ASSETS_BUCKET_NAME,
    [string]$Profile = "default",
    [string]$Region = "ap-southeast-1",
    [switch]$DryRun
)

# --- Validation ---
if ([string]::IsNullOrEmpty($BucketName)) {
    Write-Error "Thieu ten S3 bucket. Truyen -BucketName hoac set env:ASSETS_BUCKET_NAME"
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  AI Dungeon RPG - Upload Game Assets to AWS S3" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Bucket : $BucketName" -ForegroundColor Yellow
Write-Host "  Region : $Region" -ForegroundColor Yellow
Write-Host "  Profile: $Profile" -ForegroundColor Yellow
if ($DryRun) {
    Write-Host "  Mode   : DRY RUN" -ForegroundColor Magenta
}
Write-Host "------------------------------------------------------------"
Write-Host ""

$TotalErrors = 0

# --- Helper: sync mot thu muc len S3 ---
function Sync-Folder {
    param(
        [string]$Label,
        [string]$LocalPath,
        [string]$S3Prefix,
        [string]$CacheControl
    )

    if (-not (Test-Path $LocalPath)) {
        Write-Warning "[$Label] Thu muc khong ton tai, bo qua: $LocalPath"
        return
    }

    Write-Host "[$Label] Dang sync..." -ForegroundColor Green
    Write-Host "  Tu  : $LocalPath"
    Write-Host "  Den : s3://$BucketName/$S3Prefix"
    Write-Host "  Cache: $CacheControl"

    $SyncArgs = @(
        "s3", "sync",
        $LocalPath,
        "s3://$BucketName/$S3Prefix",
        "--exclude", "*.meta",
        "--exclude", "*.meta.bak",
        "--exclude", ".DS_Store",
        "--exclude", "Thumbs.db",
        "--cache-control", $CacheControl,
        "--delete",
        "--profile", $Profile,
        "--region", $Region
    )

    if ($DryRun) {
        $SyncArgs += "--dryrun"
    }

    & aws @SyncArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$Label] FAIL - exit code: $LASTEXITCODE" -ForegroundColor Red
        $script:TotalErrors++
    } else {
        Write-Host "[$Label] OK" -ForegroundColor Green
    }
    Write-Host ""
}

# --- Upload 1: Sprites ---
Sync-Folder -Label "Graphics/Sprites" `
    -LocalPath (Join-Path $ProjectRoot "Assets\Graphics\Sprites") `
    -S3Prefix "graphics/sprites/" `
    -CacheControl "max-age=86400, public"

# --- Upload 2: Art backgrounds ---
Sync-Folder -Label "Graphics/Art" `
    -LocalPath (Join-Path $ProjectRoot "Assets\Graphics\Art") `
    -S3Prefix "graphics/art/" `
    -CacheControl "max-age=86400, public"

# --- Upload 3: BGM ---
Sync-Folder -Label "Sounds/BGM" `
    -LocalPath (Join-Path $ProjectRoot "Assets\Sounds\BGM") `
    -S3Prefix "sounds/bgm/" `
    -CacheControl "max-age=604800, public"

# --- Upload 4: SFX files ---
Write-Host "[Sounds/SFX] Dang upload SFX files..." -ForegroundColor Green

$SfxFiles = @(
    "ButtonClick.wav",
    "CoinPickup.wav",
    "Crash.wav",
    "FuelPickup.wav",
    "LandingSuccess.wav",
    "Music.mp3",
    "Thruster.wav"
)

foreach ($SfxFile in $SfxFiles) {
    $LocalFile = Join-Path $ProjectRoot "Assets\Sounds\$SfxFile"
    if (Test-Path $LocalFile) {
        $CpArgs = @(
            "s3", "cp",
            $LocalFile,
            "s3://$BucketName/sounds/sfx/$SfxFile",
            "--cache-control", "max-age=604800, public",
            "--profile", $Profile,
            "--region", $Region
        )
        if ($DryRun) {
            $CpArgs += "--dryrun"
        }
        Write-Host "  Upload: $SfxFile" -ForegroundColor Gray
        & aws @CpArgs | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "  Fail: $SfxFile"
            $TotalErrors++
        }
    } else {
        Write-Host "  Skip (khong tim thay): $SfxFile" -ForegroundColor DarkGray
    }
}
Write-Host "[Sounds/SFX] Xong" -ForegroundColor Green
Write-Host ""

# --- Upload 5: Prompt markdown ---
Sync-Folder -Label "Prompts/MD" `
    -LocalPath (Join-Path $ProjectRoot "backend\src\GameBackend.Core\AIStory\Content\Prompt") `
    -S3Prefix "prompts/" `
    -CacheControl "max-age=300, public, must-revalidate"

# --- Tong ket ---
Write-Host "============================================================" -ForegroundColor Cyan
if ($TotalErrors -eq 0) {
    Write-Host "  THANH CONG - Tat ca assets da upload!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  CloudFront CDN URL prefix:"
    Write-Host "    https://d3plikv8poewp8.cloudfront.net/graphics/sprites/..."
    Write-Host "    https://d3plikv8poewp8.cloudfront.net/sounds/bgm/..."
    Write-Host "    https://d3plikv8poewp8.cloudfront.net/prompts/system_prompt.md"
    Write-Host ""
    Write-Host "  Buoc tiep theo: cdk deploy GameLambdaStack"
} else {
    Write-Host "  CO LOI: $TotalErrors loi. Kiem tra o tren." -ForegroundColor Red
}
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

exit $TotalErrors
