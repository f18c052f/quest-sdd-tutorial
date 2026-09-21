# quest-sdd-tutorial

[Quest MRアプリ 仕様駆動開発ガイドライン](docs/quest-sdd-guideline.md) を一人で1周して、
守りにくい箇所を見つけるための学習用リポジトリ。題材は「スプレーで蚊を撃退するゲーム」。

進め方は [学習ロードマップ](docs/learning-roadmap.md) の Step 3。
記録は [学習ログ](docs/learning-log.md) に書く。

GitHub は保存先としてのみ使う。Issue・Pull Request・GitHub Actions は使わない。
レビューは自分で読み返し、品質ゲートは Stop フックまで（CI は作らない）。

## この repo の範囲

**Step 3 専用。**ロードマップ Step 0・1 の練習（空のプロジェクト、GMTK のミニゲーム）は
別フォルダでやる。捨てる前提のコードを混ぜると、この repo が「ガイドラインを1周した記録」
として読めなくなるため。

## 分担

目的が2つ重なっている。Unity を思い出すこと（Step 1・2）と、ガイドラインを1周して
守りにくい箇所を見つけること（Step 3）。前者は AI がやるほど学習が消え、後者は AI が
やらないと「L1 は完成品」「L2・L3 は7〜8割の叩き台」という前提が検証できない。
なので**層で分ける**。

| | AI | 人間 |
| --- | --- | --- |
| L1（Core 4本） | コードとテストを完成させる。落ちたら AI が直す | 仕様の読み返し、記録 |
| L2（Adapter 2本） | 叩き台まで。7〜8割で止める | 残りを仕上げる。ここが Unity の復習 |
| L3（game-scene） | CLI でシーンの初期構成のみ | Editor で目視、HUD と出現範囲の調整 |
| L4（見た目・音） | 触らない | 仕上げる |
| 振り返り（3-6） | 書かない | 書く |

最後の行が要点。AI はこのループの中の当事者なので、**「どこが守りにくかったか」を AI が
書くと自己採点になる**。AI が詰まった箇所は報告させるが、判断は人間が持つ。

## 配布とブランチ

この repo は他のメンバーにも配る。**`main` は「全員が同じ起点から始められる状態」に保つ。**

| ブランチ | 中身 | 触るとき |
| --- | --- | --- |
| `main` | 骨組み、Unity プロジェクト、Meta XR SDK、cc-sdd、steering、外部設計の確定版、learning-log の雛形 | 骨組みを直すときだけ |
| `run/<名前>` | その人の実走。spec、実装、記入済み learning-log | 各自 |
| `spec/<spec>` | `run/<名前>` から切る。閉じたら戻す | 各自 |

分け方の基準は「**誰がやっても同じ結果になるか**」。Unity と SDK のセットアップは誰がやっても
同じなので `main`（手間を共有する価値がある）。spec と実装は人によって違い、そこに学習がある。
`main` に入れると、次の人は演習ではなく答えを読むことになる。

最後の振り返りだけは `docs/retrospectives/<名前>.md` として `main` に戻す。
これを集めることが、本番用ガイドラインの修正材料になる。

## タグ — どの状態からでも始められる

`main` の節目にタグを打つ。受け取る人は **自分が何を体験したいか** でタグを選び、
そこから `run/<名前>` を切る。後ろのタグほど準備が進んでいて、早く spec に入れる。

| タグ | 打済 | その時点の状態 | ここから体験できること |
| --- | --- | --- | --- |
| `setup-0-skeleton` | ✅ | 骨組みのみ。Unity プロジェクトなし | ロードマップ **Step 0 から**。Unity と Meta XR SDK を自分で入れる。asmdef が Core への `UnityEngine` 持ち込みを本当に止めるか、権限設定がシーンの編集を本当に拒否するかを、自分の手で確かめられる |
| `setup-1-unity` | ⬜ | Unity + Meta XR SDK + Pipeline + XR asmdef | **Step 3 の準備から**。シミュレータは動く。品質ゲートと cc-sdd をこれから入れるので、ガイドライン第8章の組み立てを自分で作れる |
| `setup-2-toolchain` | ⬜ | Stop フック稼働 + cc-sdd 導入済み | **SDD のフローから**。ゲートが効いているので「AI の『テストが通りました』を信用しない」を実際に体験できる。外部設計は自分で書く |
| `setup-3-ready` | ⬜ | 外部設計の確定版あり | **spec を書くところから**。配布の標準形。全員が同じ仕様から始めるので、あとで learning-log を横に並べて比較できる |

### 受け取った人の始め方

```
git clone https://github.com/f18c052f/quest-sdd-tutorial.git
cd quest-sdd-tutorial
git switch -c run/<自分の名前> setup-3-ready
```

**迷ったら `setup-3-ready`。**Unity と Quest の環境構築そのものを学びたいなら `setup-0-skeleton`。
打ってあるタグは `git tag -l` で確認する。

そのあとは、

1. Unity Hub でこのフォルダを開く（`setup-1-unity` 以降なら**セットアップ済み。Step 2 をやり直さない**）
2. [docs/external/mosquito-spray.md](docs/external/mosquito-spray.md) を読む。これが仕様
3. `git switch -c spec/spray-hit-core` して `/kiro:spec-init` から
4. spec を閉じるたびに [docs/learning-log.md](docs/learning-log.md) に記録する
5. 全部終わったら、振り返りを `docs/retrospectives/<名前>.md` にして `main` へ

`setup-2-toolchain` 以降を選んだ人は、**`npx cc-sdd` を各自で実行しないこと。**
導入済みのものが入っている（ガイドライン第4章「テックリードが1回だけ導入してコミットする。
各自で実行しない」）。

### タグを打つ側の手順

`main` が節目に達したら、その場で打つ。あとからまとめて打つと、どのコミットが
どの状態だったか分からなくなる。

```
git tag -a setup-1-unity -m "Unity 6 + Meta XR SDK + Pipeline + XR asmdef まで"
git push origin setup-1-unity
```

| 打つ時機 | タグ |
| --- | --- |
| 下の手順4が終わったら | `setup-1-unity` |
| 手順6が終わったら | `setup-2-toolchain` |
| 手順7が終わったら | `setup-3-ready` |

## いまの状態

骨組みだけがある。**Unity プロジェクトはまだ存在しない**（`Assets/` のフォルダと asmdef だけ先に置いてある）。

| 済 | 項目 |
| --- | --- |
| ✅ | Git リポジトリ、`.gitignore`、`.gitattributes`（LFS・YAML マージ設定） |
| ✅ | asmdef による層の分離（Core / Adapter / XR / Tests.EditMode / Tests.PlayMode） |
| ✅ | Claude Code の権限設定（シーン・プレハブ・`.meta` の直接編集を拒否） |
| ✅ | Stop フックのスクリプト（Unity CLI が入るまでは何もせず通る） |
| ✅ | ステアリング3ファイル（`.kiro/steering/`、合計 282 行 / 上限 400 行） |
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

急がなくてよい。L1 の4本は XR に触らないので、**spray-input-adapter に入る直前**までに
済んでいればよい。

ここまで終わったらタグを打つ。

```
git tag -a setup-1-unity -m "Unity 6 + Meta XR SDK + Pipeline + XR asmdef まで"
git push origin setup-1-unity
```

### 5. Stop フックの EditMode フラグを確定する（最初の spec の前に必須）

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

cc-sdd が入れたコマンド（`.claude/commands/` など）は**コミットする**。
配布された人が各自で `npx` を叩くとバージョンがずれるため。

```
git tag -a setup-2-toolchain -m "Stop フック稼働 + cc-sdd 導入済み"
git push origin setup-2-toolchain
```

### 7. 外部設計を書き直す

[docs/external/mosquito-spray.md](docs/external/mosquito-spray.md) は AI の下書き。
ガイドラインの鉄則3に従い、**自分の言葉で書き直してから** spec に入る。

書き直した版が `main` の確定版になり、他のメンバーはこれを入力に spec を書く。
全員が同じ仕様から始めるので、あとで learning-log を横に並べて比較できる。

```
git tag -a setup-3-ready -m "外部設計の確定版あり。配布の標準形"
git push origin setup-3-ready
```

**ここまでが `main`。**次の手順から `run/<名前>` に移る。

### 8. 最初の spec を始める

```
git switch -c run/<自分の名前>
git switch -c spec/spray-hit-core
```

`/kiro:spec-init` から。ロードマップ 3-4 の流れで回し、閉じるたびに
[docs/learning-log.md](docs/learning-log.md) に記録する。

閉じた spec は `run/<名前>` へ squash マージする。**`main` には戻さない。**

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
