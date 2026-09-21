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

目的が2つ重なっている。**Unity を思い出すこと**と、**ガイドラインを1周して守りにくい箇所を
見つけること**。前者は AI がやるほど学習が消え、後者は AI がやらないと「AI はどこまで
作れるのか」という肝心の前提が確かめられない。

そこで、**作るものの性質で分ける**。

| 作るもの | AI | 自分 |
| --- | --- | --- |
| 計算だけの部分<br><sub>当たり判定、スコア、時間の管理など。4機能</sub> | 完成まで作る。テストが落ちたら AI が直す | 仕様を読み返して記録する |
| Unity と繋ぐ部分<br><sub>コントローラーの入力、蚊の表示。2機能</sub> | 7〜8割の下書きまで | 残りを仕上げる。**ここが Unity の復習になる** |
| 空間への配置<br><sub>蚊が飛ぶ範囲、表示の位置。1機能</sub> | 骨組みだけ組む | Unity の画面で見ながら調整する |
| 見た目と音 | 触らない | 自分で仕上げる |
| 最後の振り返り | 書かない | **自分が書く** |

計算だけの部分は、正しいかどうかをテストで機械的に判定できるので AI に任せきれる。
Unity と繋ぐ部分から先は、実際に動かして見ないと分からないので、人が仕上げる。

最後の行が要点。AI はこの作業の当事者なので、**「どこが守りにくかったか」を AI が書くと
自己採点になる**。AI が詰まった箇所は報告させるが、判断は自分が持つ。

## 配布とブランチ

この repo は他のメンバーにも配る。**`main` は「全員が同じ地点から始められる状態」に保つ。**

| ブランチ | 中身 | 触るとき |
| --- | --- | --- |
| `main` | 準備が済んだ状態。Unity の設定、AI 用の道具、決めごとの文書、作るものの説明、記録用の空欄シート | 準備そのものを直すときだけ |
| `run/<名前>` | その人の作業。仕様、コード、書き込んだ記録 | 各自 |
| `spec/<機能名>` | 機能1つ分の作業。`run/<名前>` から切り、終わったら戻す | 各自 |

分け方の基準は「**誰がやっても同じ結果になるか**」。Unity と SDK の設定は誰がやっても
同じなので `main` に置く（手間を共有する価値がある）。仕様とコードは人によって違い、
そこに学習がある。`main` に入れると、次に受け取る人は自分で考えず答えを読むことになる。

最後の振り返りだけは `docs/retrospectives/<名前>.md` として `main` に戻す。
これを集めることが、本番用ガイドラインの修正材料になる。

## タグ — どこから自分でやるかを選ぶ

`main` の節目にタグを打ってある。受け取る人は **どこから自分でやりたいか** でタグを選び、
そこから `run/<名前>` を切る。後ろのタグほど準備が済んでいて、早く本題に入れる。

### 全体の流れ

このチュートリアルは4段階ある。本題は **D**。A〜C は準備で、飛ばしても D はできる。

```
  A 環境を作る        B 道具を入れる      C 何を作るか決める    D 作る（本題）

  Unity を入れて      AI が書いたコードを  ゲームの中身を        機能1つ分の仕様を
  Quest 用に          自動でテストする     自分の言葉で          書いて AI に作らせる
  設定する            仕組みを入れる       文章にする            これを7機能ぶん
  ────────────────    ────────────────    ────────────────     ────────────────
  ▲                   ▲                   ▲                    ▲
  setup-0-skeleton    setup-1-unity       setup-2-toolchain    setup-3-ready
```

タグは「**その手前までが済んでいる**」状態を指す。
たとえば `setup-2-toolchain` を選ぶと A と B は済んでいて、C から自分でやることになる。

---

### `setup-0-skeleton` — A から全部やる ✅ 打ってある

**入っているもの**
フォルダの骨組みと、決めごとを書いた文書だけ。Unity のプロジェクトそのものがまだ無いので、
Unity Hub で開いても中身は空。

**自分でやること**
Unity のインストール、Quest 用の設定、Meta XR SDK の導入、テストの自動化、
仕様を書くコマンドの導入、ゲーム内容の決定、7機能の実装。つまり全部。

**ここでしか分からないこと**
「AI に勝手をさせない仕掛け」が本当に効くかを自分の手で試せる。たとえば、計算専用の
置き場所（`Assets/Scripts/Core/`）に Unity 専用のコードを書くとコンパイルが通らないこと。
AI にシーンのファイルを直接書き換えさせようとすると拒否されること。
先のタグから始めると、これらは最初から動いているので、何が守られているのか見えない。

---

### `setup-1-unity` — B から ⬜ これから

**入っているもの**
Unity で開けばそのまま動く状態。Quest のシミュレータ（実機なしで、ヘッドセットで
どう見えるかを試せるもの）も入っている。

**自分でやること**
AI が作業を終えるたびにテストを自動で走らせる設定、仕様を書くコマンドの導入、
ゲーム内容の決定、7機能の実装。

**ここでしか分からないこと**
「AI が『できました』と言っても信じない」を、どう仕組みにするか。
AI の報告ではなくテストの結果で判定する流れを、自分で組み立てることになる。

---

### `setup-2-toolchain` — C から ⬜ これから

**入っているもの**
AI が作業を終えるたびにテストが自動で走る。仕様を書くためのコマンド（`/kiro:spec-init` など）
も入っている。

**自分でやること**
何を作るかを自分の言葉で書くこと、そして7機能の実装。

**ここでしか分からないこと**
「作るものの説明を先に書く」ことの難しさ。ここで書くのは顧客に見せる説明なので、
数値（「噴射の範囲は15度」など）を書いてはいけない。それでいて、あとで仕様に
落とせるだけの中身が要る。この匙加減は、実際に書いてみないと分からない。

---

### `setup-3-ready` — D だけやる ⬜ これから

**入っているもの**
上の全部。作るものの説明も書いてある。

**自分でやること**
機能1つ分の仕様を書いて AI に作らせる、を7回。

**ここでしか分からないこと**
本題そのもの。全員が同じ説明から始めるので、あとで各自の記録を横に並べて
「同じ仕様から何が出てきたか」を比べられる。

**迷ったらこれを選ぶ。**

---

### 受け取った人の始め方

```
git clone https://github.com/f18c052f/quest-sdd-tutorial.git
cd quest-sdd-tutorial
git switch -c run/<自分の名前> setup-3-ready
```

最後のタグ名は、上から選んだものに変える。打ってあるタグは `git tag -l` で確認できる。

そのあとは、

1. Unity Hub でこのフォルダを開く（`setup-1-unity` 以降なら**設定済み。やり直さない**）
2. [docs/external/mosquito-spray.md](docs/external/mosquito-spray.md) を読む。これが作るものの説明
3. `git switch -c spec/spray-hit-core` して `/kiro:spec-init` から始める
4. 機能を1つ終えるたびに [docs/learning-log.md](docs/learning-log.md) に記録する
5. 全部終わったら、振り返りを `docs/retrospectives/<名前>.md` にして `main` へ

`setup-2-toolchain` 以降を選んだ人は、**`npx cc-sdd` を自分で実行しないこと。**
導入済みのものが入っている。各自で入れ直すとバージョンがずれる。

### タグを打つ側の手順

`main` が節目に達したら、その場で打つ。あとからまとめて打つと、どのコミットが
どの状態だったか分からなくなる。

```
git tag -a setup-1-unity -m "Unity 6 + Meta XR SDK + Pipeline + XR asmdef まで"
git push origin setup-1-unity
```

| 打つ時機 | タグ |
| --- | --- |
| 下の A-4 が終わったら | `setup-1-unity` |
| B-6 が終わったら | `setup-2-toolchain` |
| C-7 が終わったら | `setup-3-ready` |

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

上の A〜D に対応している。上から順に。

| 段階 | 手順 | 終わったら |
| --- | --- | --- |
| A 環境を作る | A-1 〜 A-4 | `setup-1-unity` を打つ |
| B 道具を入れる | B-5 〜 B-6 | `setup-2-toolchain` を打つ |
| C 何を作るか決める | C-7 | `setup-3-ready` を打つ。**ここまでが `main`** |
| D 作る | D-8 | `run/<名前>` に移る |

### A-1. Unity と AI 用のツールを入れる

<sub>ロードマップ Step 0</sub>

ロードマップ Step 0 のとおり。

```
unity auth login
unity doctor
```

`unity` が PATH に通るまで、Stop フックは「Unity CLI が見つからない」と出して素通りする。

### A-2. Quest 用に設定する

<sub>ロードマップ Step 2。OpenXR、Meta XR SDK、シミュレータ</sub>

**Unity Hub で新規作成せず、このフォルダをそのまま開く。**
`Assets/Scripts/` と `Assets/Tests/` の asmdef が既に置いてあるので、開いた時点で層の境界が効く。

1. Unity Hub → Add → このフォルダを選ぶ（Unity 6、3D テンプレート相当の設定）
2. OpenXR を有効化し、Meta XR Core SDK を導入する
3. Project Setup Tool の指摘をすべて解消する
4. Building Blocks でカメラリグとパススルーを追加する
5. Meta XR Simulator を導入し、ツールバーから有効化する

開いたあと Unity が `.meta` ファイルを大量に生成する。これは追跡するのが正しいのでコミットする。

### A-3. AI から Unity を操作できるようにする

<sub>Unity Pipeline パッケージの導入</sub>

```
unity pipeline install
```

### A-4. Meta XR SDK を使う置き場所を指定する

<sub>`MosquitoSpray.XR.asmdef` への参照追加</sub>

`Assets/Scripts/XR/MosquitoSpray.XR.asmdef` の `references` に、Meta XR SDK の
アセンブリ名を追記する。SDK が入っていないと参照を解決できないため、いまは空にしてある。

Adapter と Core には足さない。Meta XR SDK への依存は XR アセンブリだけに閉じ込める。

急がなくてよい。最初の4機能は XR に触らないので、**5番目（`spray-input-adapter`）に入る直前**
までに済んでいればよい。

ここまで終わったらタグを打つ。

```
git tag -a setup-1-unity -m "Unity 6 + Meta XR SDK + Pipeline + XR asmdef まで"
git push origin setup-1-unity
```

### B-5. テストの自動実行を動くようにする

<sub>Stop フックの EditMode フラグを埋める。**最初の機能に入る前に必須**</sub>

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

### B-6. 仕様を書くためのコマンドを入れる

<sub>cc-sdd の導入</sub>

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

### C-7. 何を作るかを自分の言葉で書く

<sub>外部設計。顧客に見せる説明なので、数値は書かない</sub>

[docs/external/mosquito-spray.md](docs/external/mosquito-spray.md) は AI の下書き。
ガイドラインの鉄則3に従い、**自分の言葉で書き直してから** spec に入る。

書き直した版が `main` の確定版になり、他のメンバーはこれを入力に spec を書く。
全員が同じ仕様から始めるので、あとで learning-log を横に並べて比較できる。

```
git tag -a setup-3-ready -m "外部設計の確定版あり。配布の標準形"
git push origin setup-3-ready
```

**ここまでが `main`。**次の D-8 から `run/<名前>` に移る。

### D-8. 最初の機能を作る

<sub>spec 1本目。`spray-hit-core`</sub>

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
