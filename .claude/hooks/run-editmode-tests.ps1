<#
  品質ゲート 1段目（ガイドライン第8章）— Stop フック

  AI が作業を終えようとしたときに EditMode テストを走らせ、落ちていれば完了させない。
  「テストが通りました」という AI の自己申告を信用しないための仕組み。

  【このスクリプトの鉄則】
  結果が取れなかったときは、黙って通さない。
  「実行できなかった」を「問題なし」として扱った瞬間、この仕組みは名前だけになる。
  例外はロードマップ Step 0 / Step 2 が未完のときだけ（下の 2 箇所）。

  PlayMode テストはここでは回さない。開いている Editor が Play モードに入り、
  人間の作業を止めてしまうため（ガイドライン第8章）。PlayMode は Test Runner から手動で流す。
#>

$ErrorActionPreference = 'Stop'

# --- 文字コード -------------------------------------------------------------
# Unity CLI は UTF-8 で出力するが、Windows PowerShell の既定は CP932。
# そのままだと日本語のテスト名が壊れ、返ってきた JSON が解析できなくなる
# （閉じ引用符が消えて別物になる）。判定の入り口なので必ず先に直す。
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
} catch {
    # 変更できない環境でも先へ進む
}

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

# ここで素通りさせてよいのは、ロードマップ Step 0 がまだ終わっていないときだけ。
if (-not $unityExe) {
    Write-Host "[Stop hook] Unity CLI が見つからないので EditMode テストを省略しました。"
    Write-Host "[Stop hook] ロードマップ Step 0 が終わるまでは、この段は機能しません。"
    exit 0
}

# ここで素通りさせてよいのは、ロードマップ Step 2 がまだ終わっていないときだけ。
if (-not (Test-Path (Join-Path $projectDir 'ProjectSettings/ProjectVersion.txt'))) {
    Write-Host "[Stop hook] Unity プロジェクトがまだありません（ProjectSettings/ が無い）。"
    Write-Host "[Stop hook] ロードマップ Step 2 でこのフォルダを Unity Hub から開いてください。"
    exit 0
}

$logsDir = Join-Path $projectDir 'Logs'
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir | Out-Null }
$errPath = Join-Path $logsDir 'stop-hook-stderr.txt'

# 外部コマンドを呼ぶ。
# PowerShell 5.1 では、外部コマンドが標準エラーに何か書くと $ErrorActionPreference='Stop' の
# もとでスクリプトが途中で死ぬ。そうなるとこのスクリプトは終了コード 1 を返し、
# Claude Code は「2 でないから続けてよい」と判断して作業を止めない ＝ ゲートが素通りする。
# これを避けるため、外部コマンドの呼び出し中だけ 'Continue' に落とす。
function Invoke-Unity {
    param([string[]] $Arguments)

    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & $unityExe @Arguments 2> $errPath | Out-String
        $exit = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $prev
    }

    $err = ''
    if (Test-Path $errPath) { $err = (Get-Content $errPath -Raw -ErrorAction SilentlyContinue) }

    [pscustomobject]@{ StdOut = $out; StdErr = $err; ExitCode = $exit }
}

# 作業を止める（＝完了させない）。exit 2 で標準エラーに出した内容だけが Claude に戻る。
#
# ここで Write-Error を使ってはいけない。$ErrorActionPreference='Stop' のもとでは
# Write-Error 自体が致命的エラーになってスクリプトがその場で死に、終了コードが 2 ではなく
# 1 になる。Claude Code が作業を止めるのは 2 のときだけなので、素通りしてしまう。
function Block-Completion {
    param([string] $Reason, [string] $Detail)
    [Console]::Error.WriteLine("[Stop hook] $Reason")
    [Console]::Error.WriteLine("")
    [Console]::Error.WriteLine($Detail)
    exit 2
}

# ---------------------------------------------------------------------------
# 経路1: 起動中の Editor でテストを走らせる（Unity Pipeline パッケージ）
#
# `unity test` は Editor を開いたままでは実行できない（終了コード 6）。
# 普段は Editor を開いて作業するので、こちらが本命の経路になる。
#
# 【注意】run_tests はテストが落ちても終了コード 0 を返す。
# 終了コードでは判定できないので、返ってきた JSON の Summary.Failed で判定する。
# ---------------------------------------------------------------------------
Write-Host "[Stop hook] 起動中の Editor で EditMode テストを実行します。"
$viaEditor = Invoke-Unity @('command', 'run_tests', '--mode', 'EditMode', '--result-only')

$summary = $null
$json = $null
try {
    # 先頭に BOM が付いてくることがあり、そのままでは ConvertFrom-Json が失敗する。
    $body = $viaEditor.StdOut.TrimStart([char]0xFEFF, ' ', [char]13, [char]10, [char]9)
    $json = $body | ConvertFrom-Json
    if ($null -ne $json.Summary) { $summary = $json.Summary }
} catch {
    # JSON が取れない ＝ Editor が起動していないか、接続できない。経路2 へ落ちる。
}

if ($null -ne $summary) {
    $line = "実行 $($summary.Total) 件 / 成功 $($summary.Passed) / 失敗 $($summary.Failed) / 省略 $($summary.Skipped)"

    if ($summary.Failed -gt 0) {
        $failed = $json.Results | Where-Object { $_.Status -eq 'Failed' } |
                  ForEach-Object { "- $($_.FullName)`n  $($_.Message)" }
        Block-Completion "EditMode テストが失敗しています。" "$line`n`n$($failed -join "`n")"
    }

    # コンパイルが通っているかを別に確かめる。
    # コンパイルが落ちていると、Editor は前回通ったときのアセンブリでテストを走らせる。
    # つまり「壊れたコードを書いたのに全部成功した」という結果が出うる。
    # テストの成否だけを見ていると、この状態を見逃す。
    $compilationFailed = $null
    $consoleStatus = Invoke-Unity @('command', 'console_status', '--result-only')
    try {
        $sBody = $consoleStatus.StdOut.TrimStart([char]0xFEFF, ' ', [char]13, [char]10, [char]9)
        $sJson = $sBody | ConvertFrom-Json
        if ($null -ne $sJson.groundTruth) {
            $compilationFailed = [bool] $sJson.groundTruth.compilationFailed
        }
    } catch {
        # 取れなければ $null のまま。下で「分からない」として扱う。
    }

    if ($compilationFailed -eq $true) {
        Block-Completion "コンパイルが通っていません。" `
            "テストの結果（$line）は、前回コンパイルが通ったときのコードによるものです。信用できません。"
    }

    if ($summary.Total -eq 0) {
        if ($compilationFailed -eq $false) {
            # コンパイルは通っている ＝ まだテストを1本も書いていないだけ。
            # D 段階に入る前はこの状態が正常なので止めない。ただし黙らない。
            Write-Host "[Stop hook] EditMode テストが1件もありません。この段は今なにも守っていません。"
            exit 0
        }
        Block-Completion "EditMode テストが1件も実行されませんでした。" `
            "コンパイルが通っているかどうかも確認できませんでした。`n`n$($viaEditor.StdOut)"
    }

    Write-Host "[Stop hook] EditMode テストは全て成功しました（$line）。"
    exit 0
}

# ---------------------------------------------------------------------------
# 経路2: Editor が起動していないので、バッチで走らせる
# こちらは終了コードで判定する。
# ---------------------------------------------------------------------------
Write-Host "[Stop hook] Editor に接続できないので、バッチで EditMode テストを実行します。"

# 結果ファイルは Logs/ に書く（.gitignore 済み）。既定のままだとリポジトリ直下に
# test-results.xml が残り、コミット対象に紛れ込む。
$resultsPath = Join-Path $logsDir 'editmode-tests.xml'

# 確認済み: unity CLI 1.0.0-beta.8 の `unity test --mode EditMode`
# Unity CLI は beta なので、動かなくなったら `unity test --help` で確認し直す。
$viaBatch = Invoke-Unity @('test', '--mode', 'EditMode', '--non-interactive', '--no-banner', '--output', $resultsPath)
$code = $viaBatch.ExitCode
$detail = "$($viaBatch.StdOut)`n$($viaBatch.StdErr)"

if ($code -eq 0) {
    Write-Host "[Stop hook] EditMode テストは全て成功しました。"
    exit 0
}

$reason = switch ($code) {
    8 { "EditMode テストが失敗しています。" }
    6 { "テストが完走しませんでした。コンパイルエラーか、Editor が開いたままの可能性があります。" }
    2 { "unity test の引数が不正です（.claude/hooks/run-editmode-tests.ps1 を確認）。" }
    3 { "Unity にサインインできていません。unity auth login を実行してください。" }
    4 { "Unity CLI の設定が足りません。unity doctor を実行してください。" }
    7 { "Unity のサービスに接続できませんでした。" }
    default { "unity test が終了コード $code で終了しました。" }
}

# 環境側の問題（3/4/7 など）でも止める。
# 「テストの結果が分からない」ときに通してしまうと、このゲートは名前だけになるため。
# 無限ループにはならない（先頭の stop_hook_active で2回目は素通りする）。
Block-Completion "$reason (unity test 終了コード: $code)" $detail
