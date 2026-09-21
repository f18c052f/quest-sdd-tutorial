# quest-sdd-tutorial

[Quest MRアプリ 仕様駆動開発ガイドライン](docs/quest-sdd-guideline.md) を一人で1周して、
守りにくい箇所を見つけるための学習用リポジトリ。題材は「スプレーで蚊を撃退するゲーム」。

進め方は [学習ロードマップ](docs/learning-roadmap.md) の Step 3。
記録は [学習ログ](docs/learning-log.md) に書く。

GitHub は保存先としてのみ使う。Issue・Pull Request・GitHub Actions は使わない。
レビューは自分で読み返し、品質ゲートは Stop フックまで（CI は作らない）。

## いまの状態

骨組みだけがある。**Unity プロジェクトはまだ存在しない**（`Assets/` のフォルダと asmdef だけ先に置いてある）。

| 済 | 項目 |
| --- | --- |
| ✅ | Git リポジトリ、`.gitignore`、`.gitattributes`（LFS・YAML マージ設定） |
| ✅ | asmdef による層の分離（Core / Adapter / XR / Tests.EditMode / Tests.PlayMode） |
| ✅ | Claude Code の権限設定（シーン・プレハブ・`.meta` の直接編集を拒否） |
| ✅ | Stop フックのスクリプト（Unity CLI が入るまでは何もせず通る） |
| ✅ | ステアリング3ファイル（`.kiro/steering/`、合計 258 行 / 上限 400 行） |
| ✅ | 学習ログの雛形、外部設計の下書き |
| ⬜ | Unity プロジェクト本体（ロードマップ Step 0・2） |
| ⬜ | `unity pipeline install` |
| ⬜ | Meta XR SDK と `MosquitoSpray.XR.asmdef` への参照追加 |
| ⬜ | Stop フックの EditMode フラグ確定 |
| ⬜ | cc-sdd の導入 |
| ⬜ | 外部設計の書き直し（人間が書く） |

## 残っている作業

上から順に。

### 1. Step 0 — 開発環境

ロードマップ Step 0 のとおり。

```
unity auth login
unity doctor
```

`unity` が PATH に通るまで、Stop フックは「Unity CLI が見つからない」と出して素通りする。

### 2. Step 2 — Unity プロジェクト

**Unity Hub で新規作成せず、このフォルダをそのまま開く。**
`Assets/Scripts/` と `Assets/Tests/` の asmdef が既に置いてあるので、開いた時点で層の境界が効く。

1. Unity Hub → Add → このフォルダを選ぶ（Unity 6、3D テンプレート相当の設定）
2. OpenXR を有効化し、Meta XR Core SDK を導入する
3. Project Setup Tool の指摘をすべて解消する
4. Building Blocks でカメラリグとパススルーを追加する
5. Meta XR Simulator を導入し、ツールバーから有効化する

開いたあと Unity が `.meta` ファイルを大量に生成する。これは追跡するのが正しいのでコミットする。

### 3. Unity Pipeline パッケージ

```
unity pipeline install
```

### 4. XR アセンブリに Meta XR SDK の参照を足す

`Assets/Scripts/XR/MosquitoSpray.XR.asmdef` の `references` に、Meta XR SDK の
アセンブリ名を追記する。SDK が入っていないと参照を解決できないため、いまは空にしてある。

Adapter と Core には足さない。Meta XR SDK への依存は XR アセンブリだけに閉じ込める。

### 5. Stop フックの EditMode フラグを確定する

```
unity test --help
```

EditMode だけに絞るフラグを確認し、
`.claude/hooks/run-editmode-tests.ps1` の `$EditModeFlag` に埋める。

Unity CLI は beta で、公式リファレンスにはフラグの一覧が載っていないため、推測では埋めていない。
**空のままだと PlayMode テストまで走り、Editor が Play モードに入って作業が止まる可能性がある。**

埋めたら、わざと落ちるテストを1本書いて、Stop フックが作業を止めることを確認する。

> スクリプトは **UTF-8 BOM 付き**で保存すること。Windows PowerShell 5.1 は BOM がないと
> `.ps1` を ANSI として読み、日本語コメントでパースエラーになる。

### 6. cc-sdd を導入する

```
npx cc-sdd@latest --claude-code --lang ja
```

導入したら **`.kiro/steering/` の3ファイルが上書きされていないか確認する。**
cc-sdd は `/kiro:steering` で steering を生成・更新するので、手書きの内容が消えることがある。

```
git status
git diff -- .kiro/steering/
```

消えていたら `git checkout -- .kiro/steering/` で戻す。

### 7. 外部設計を書き直す

[docs/external/mosquito-spray.md](docs/external/mosquito-spray.md) は AI の下書き。
ガイドラインの鉄則3に従い、**自分の言葉で書き直してから** spec に入る。

### 8. 最初の spec を始める

```
git switch -c spec/spray-hit-core
```

`/kiro:spec-init` から。ロードマップ 3-4 の流れで回し、閉じるたびに
[docs/learning-log.md](docs/learning-log.md) に記録する。

## リポジトリの構成

```
.claude/          権限設定と Stop フック
.kiro/steering/   全 spec が毎回読む共通前提（合計 400 行以内）
.kiro/specs/      進行中の spec
.kiro/archive/    完了した spec
Assets/Scripts/   Core / Adapter / XR（asmdef で分離）
Assets/Tests/     EditMode / PlayMode
docs/             ガイドライン、ロードマップ、外部設計、学習ログ
```

詳しい責務は [.kiro/steering/structure.md](.kiro/steering/structure.md) にある。

## Git の初期設定

クローンした直後に一度だけ。

```
git lfs install
```

`.gitattributes` で `*.unity` `*.prefab` `*.asset` に `merge=unityyamlmerge` を指定している。
使うにはマージドライバの登録が要る（Unity 同梱の `UnityYAMLMerge` へのパスを指定する）。
一人で開発しているうちは競合しないので、後回しでよい。

## AI に守らせていること

| 手段 | 何を止めるか |
| --- | --- |
| asmdef `noEngineReferences` | `MosquitoSpray.Core` への `UnityEngine` の持ち込み（コンパイルエラー） |
| `.claude/settings.json` の `deny` | `*.unity` `*.prefab` `*.meta` への Edit / Write |
| Stop フック | EditMode テストが落ちた状態での作業完了 |

**Bash（`sed`・リダイレクト）経由でシーンを書き換える抜け道は塞いでいない。**
ガイドライン第1章が認めている既知の穴で、コミット前の読み返しで拾う。
`.kiro/steering/tech.md` の禁止事項に明記してある。
