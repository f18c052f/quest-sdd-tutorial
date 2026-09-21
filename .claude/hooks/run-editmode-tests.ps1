<#
  品質ゲート 1段目（ガイドライン第8章）— Stop フック

  AI が作業を終えようとしたときに EditMode テストを走らせ、落ちていれば完了させない。
  「テストが通りました」という AI の自己申告を信用しないための仕組みなので、
  このスクリプトの判定は必ず unity の終了コードだけで行い、出力の文面では判断しない。

  PlayMode テストはここでは回さない。開いている Editor が Play モードに入り、
  人間の作業を止めてしまうため（ガイドライン第8章）。PlayMode は Test Runner から手動で流す。
#>

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# EditMode だけを走らせる。PlayMode まで走ると、開いている Editor が Play モードに
# 入って人の作業が止まるため（ガイドライン第8章）。
#
#   確認済み: unity CLI 1.0.0-beta.8 の `unity test --mode EditMode`
#   Unity CLI は beta なので、動かなくなったら `unity test --help` で確認し直す。
# ---------------------------------------------------------------------------
$EditModeFlag = @('--mode', 'EditMode')

# --- Stop フックの無限ループ防止 -------------------------------------------
# 既にこのフックが原因で作業が継続されている場合は、もう一度止めない。
try {
    $stdin = [Console]::In.ReadToEnd()
    if ($stdin) {
        $payload = $stdin | ConvertFrom-Json
        if ($payload.stop_hook_active -eq $true) {
            exit 0
        }
    }
} catch {
    # stdin が無い／壊れている場合は素通りさせる。判定の本体はこの下。
}

# --- プロジェクトルートへ移動 ----------------------------------------------
if ($env:CLAUDE_PROJECT_DIR) {
    $projectDir = $env:CLAUDE_PROJECT_DIR
} else {
    $projectDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
Set-Location $projectDir

# --- Unity CLI の在り処を決める ---------------------------------------------
# Claude Code を起動したあとに Unity を入れた場合、プロセスの PATH が古いままで
# unity が見つからない。これを「未導入」と誤判定するとゲートが黙って素通りし続けるので、
# レジストリから PATH を読み直し、それでも駄目なら既定の導入先を直接見る。
try {
    $machinePath = [System.Environment]::GetEnvironmentVariable('Path', 'Machine')
    $userPath    = [System.Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = (@($machinePath, $userPath, $env:Path) | Where-Object { $_ }) -join ';'
} catch {
    # レジストリが読めない環境ではそのまま進む
}

$unityExe = $null
$found = Get-Command unity -ErrorAction SilentlyContinue
if ($found) {
    $unityExe = $found.Source
} elseif (Test-Path "$env:LOCALAPPDATA/Unity/bin/unity.exe") {
    $unityExe = "$env:LOCALAPPDATA/Unity/bin/unity.exe"
}

if (-not $unityExe) {
    Write-Host "[Stop hook] Unity CLI が見つからないので EditMode テストを省略しました。"
    Write-Host "[Stop hook] ロードマップ Step 0 が終わるまでは、この段は機能しません。"
    exit 0
}

# --- Unity プロジェクトとして成立しているかを確認 ---------------------------
if (-not (Test-Path (Join-Path $projectDir 'ProjectSettings/ProjectVersion.txt'))) {
    Write-Host "[Stop hook] Unity プロジェクトがまだありません（ProjectSettings/ が無い）。"
    Write-Host "[Stop hook] ロードマップ Step 2 でこのフォルダを Unity Hub から開いてください。"
    exit 0
}

# --- EditMode テストを実行 --------------------------------------------------
# 結果ファイルは Logs/ に書く（.gitignore 済み）。既定のままだとリポジトリ直下に
# test-results.xml が残り、コミット対象に紛れ込む。
$resultsPath = Join-Path $projectDir 'Logs\editmode-tests.xml'
$logsDir = Split-Path $resultsPath
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir | Out-Null }

$unityArgs = @('test') + $EditModeFlag + @('--non-interactive', '--no-banner', '--output', $resultsPath)
Write-Host "[Stop hook] EditMode テストを実行します: unity $($unityArgs -join ' ')"

$testOutput = & $unityExe @unityArgs 2>&1 | Out-String
$code = $LASTEXITCODE

Write-Host $testOutput

switch ($code) {
    0 {
        Write-Host "[Stop hook] EditMode テストは全て成功しました。"
        exit 0
    }

    # 8  = テストは走ったが1件以上落ちた
    # 6  = 実行が完走しなかった（コンパイルエラーなど。テスト結果が出ていない）
    # 1  = 一般エラー
    # 2  = 使い方の誤り（＝上の $EditModeFlag が間違っている可能性が高い）
    { $_ -in 8, 6, 1, 2 } {
        $reason = switch ($code) {
            8 { "EditMode テストが失敗しています。" }
            6 { "テストが完走しませんでした。コンパイルエラーの可能性があります。" }
            2 { "unity test の引数が不正です（.claude/hooks/run-editmode-tests.ps1 の `$EditModeFlag を確認）。" }
            default { "unity test が一般エラーで終了しました。" }
        }
        # exit 2 で標準エラーに出した内容が Claude に戻り、作業が完了扱いにならない。
        Write-Error "[Stop hook] $reason (unity test 終了コード: $code)`n`n$testOutput"
        exit 2
    }

    # 3 = 認証/認可、4 = 要設定、7 = Unity サービスに到達できない
    # 130 / 143 = 中断。いずれも AI が直せる種類の失敗ではないので、止めずに警告だけ出す。
    default {
        Write-Host "[Stop hook] 環境側の問題でテストを実行できませんでした（終了コード: $code）。"
        Write-Host "[Stop hook] 作業は止めません。unity doctor で環境を確認してください。"
        exit 0
    }
}
