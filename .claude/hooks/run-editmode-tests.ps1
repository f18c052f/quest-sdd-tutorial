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
# TODO(Step 0 完了後): EditMode だけに絞るフラグを確認して埋める。
#
#   Unity CLI は beta で、リファレンスページには `unity test --help` を見よ、としか
#   書かれていない。推測で埋めると「PlayMode まで走って Editor が固まる」か
#   「フラグ不正で毎回 usage error」になるので、確認するまで空にしてある。
#
#   確認方法:  unity test --help
#   埋める例:  $EditModeFlag = @('--platform', 'EditMode')
#
#   空のままだと PlayMode テストも走る可能性がある。Step 0 が済んだら必ず埋めること。
# ---------------------------------------------------------------------------
$EditModeFlag = @()

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

# --- Unity CLI の有無を確認 -------------------------------------------------
$unity = Get-Command unity -ErrorAction SilentlyContinue
if (-not $unity) {
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
Write-Host "[Stop hook] EditMode テストを実行します: unity test $($EditModeFlag -join ' ')"

$testOutput = & unity test @EditModeFlag 2>&1 | Out-String
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
