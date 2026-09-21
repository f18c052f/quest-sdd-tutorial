# 準備の手順

[README](../README.md) の A〜C を自分でやる場合の手順。`setup-3-ready` から始めた人は
ここを読む必要はない（すべて済んでいる）。

最後に、このリポジトリの構成と、AI に守らせている仕組みの説明を置いてある。

## いまの状態

骨組みだけがある。**Unity のプロジェクトはまだ存在しない**
（`Assets/` のフォルダと、置き場所の境界を決めるファイルだけが先に置いてある）。

| 済 | 項目 |
| --- | --- |
| ✅ | Git の設定（無視するファイル、バイナリの扱い、改行コード） |
| ✅ | 置き場所ごとのコンパイル境界（計算専用 / Unity と繋ぐ / Meta XR SDK 用 / テスト2種） |
| ✅ | AI にシーン・プレハブ・`.meta` を直接編集させない設定 |
| ✅ | AI の作業終了時にテストを走らせるスクリプト（Unity CLI が入るまでは何もせず通る） |
| ✅ | AI が毎回読む決めごと3ファイル（`.kiro/steering/`） |
| ✅ | 記録シートの雛形、作るものの説明の下書き |
| ⬜ | Unity のプロジェクト本体（手順 A-2） |
| ⬜ | AI から Unity を操作するための追加パッケージ |
| ⬜ | Meta XR SDK と、それを使う置き場所の指定 |
| ✅ | テスト自動実行のコマンド確定（`--mode EditMode`） |
| ⬜ | 仕様を書くための道具（cc-sdd） |
| ⬜ | 作るものの説明の書き直し（人が書く） |

## 手順

README の A〜D に対応している。上から順に。

| 段階 | 手順 | 終わったら |
| --- | --- | --- |
| A 環境を作る | A-1 〜 A-4 | `setup-1-unity` を打つ |
| B 道具を入れる | B-5 〜 B-6 | `setup-2-toolchain` を打つ |
| C 何を作るか決める | C-7 | `setup-3-ready` を打つ。**ここまでが `main`** |
| D 作る | D-8 | `run/<名前>` に移る |

---

### A-1. Unity と AI 用のツールを入れる

必要なものの一覧は [README の「必要なもの」](../README.md#必要なもの) にある。
順に入れる。

**1. Unity Hub を入れる** — [unity.com/download](https://unity.com/download)

`unity` コマンドはこれと一緒に入る。別途インストールは要らない。

**2. Unity Hub から Unity 6 を入れる**

**Android Build Support を一緒に選ぶ。** Quest は Android なので、これが無いとビルドできない。

**`android` だけでは足りない。** SDK/NDK と OpenJDK が子モジュールになっていて、
3つ揃わないと実機向けのビルドができない。Unity Hub の画面では `android` に
チェックを入れると子項目が出てくる。

| モジュール ID | 名前 | ダウンロード |
| --- | --- | --- |
| `android` | Android Build Support | 1.24 GB |
| `android-sdk-ndk-tools` | Android SDK & NDK Tools | 1.12 GB |
| `android-open-jdk-17.0.18+8` | OpenJDK | 113 MB |

あとから入れる場合、Unity Hub の Installs 画面（歯車 → Add modules）か、CLI で入れる。
**プロジェクトは要らない。**モジュールは Editor のバージョンに紐づく。

```
unity install-modules -e 6000.6.2f1 -m android --cm --accept-eula
```

`--cm` が子モジュールを一緒に入れる指定。入ったかどうかは
`unity install-modules -e 6000.6.2f1 --list` で確認する。

**3. Claude Code を入れ、Unity 公式プラグインを追加する**

**ターミナル**で実行する。

```
claude plugin install unity@claude-plugins-official --scope project
```

`claude plugin list` の `Status` が `✔` になれば入っている。

`/plugin ...` という案内を見かけるが、**VS Code 拡張では使えない**（`/plugin isn't
available in this environment` と出る）。上のターミナルコマンドを使う。

**`.claude/settings.json` に書いてあるだけでは動かない。**このリポジトリには
`enabledPlugins` がコミットしてあるが、それは*有効にする*指定であって、実体は
各自の PC に入れる必要がある。入れずに使うと `failed to load` になる。

**4. サインインして確認する**

```
unity auth login
unity doctor
```

`unity doctor` は環境の不備を一覧で出す。ここで指摘が残っていると、あとの手順で
原因の分かりにくい失敗をする。先に消しておく。

**5. 入れたバージョンを記録する**

Unity のバージョンは手順 A-2 のあと `ProjectSettings/ProjectVersion.txt` にも
記録されるが、**それ以外はどこにも残らない。**入れたら下の表を更新する。

| | バージョン | 確認日 |
| --- | --- | --- |
| Unity | 6000.6.2f1 | 2026-09-21 |
| Unity CLI（`unity --version`） | 1.0.0-beta.10 | 2026-09-21 |
| Unity 公式プラグイン（`claude plugin list`） | 0.1.6-beta | 2026-09-21 |

**Unity CLI は勝手に上がる。**この手順を進めている最中に beta.8 → beta.10 に
自動更新された。`unity test` のオプションは変わっていなかったが、変わる前提でいる。

`unity doctor` で `check.windows-long-paths` が `warn` になることがある。
Unity のプロジェクトはパスが深くなりやすく、Windows の 260 文字制限に当たると
ビルドが不可解な失敗をする。作業を始める前に長いパスを有効にしておくとよい。

> `unity` コマンドが使えるようになるまで、テスト自動実行のスクリプトは
> 「Unity CLI が見つからない」と表示して素通りする。エラーにはならないので、
> ここが済んでいなくても作業は止まらない。

### A-2. Quest 用に設定する

[学習ロードマップ](learning-roadmap.md) の Step 2。

**Unity Hub で新規作成せず、このフォルダをそのまま開く。**
置き場所の境界を決めるファイルが既に置いてあるので、開いた時点でそれが効く。
新規作成すると、それらが別の場所にできてしまう。

1. Unity Hub → Add → このフォルダを選ぶ（Unity 6、3D テンプレート相当）
2. OpenXR を有効にし、Meta XR Core SDK を入れる
3. Project Setup Tool（設定の不備を指摘してくれる画面）の指摘をすべて消す
4. Building Blocks から、カメラとパススルーをドラッグして追加する
5. Meta XR Simulator を入れ、ツールバーから有効にする

開いたあと、Unity が `.meta` ファイルを大量に作る。これは**消さずにコミットする**のが正しい。
Unity がファイルの対応関係を記録しているもので、消すと参照が壊れる。

同時に `ProjectSettings/` と `Packages/manifest.json` も作られる。ここに Unity 本体と
Meta XR SDK のバージョンが記録される。**受け取った人はこれを見て同じバージョンを入れる**ので、
どちらも必ずコミットする。以降、バージョンの正はこのファイルになる。

### A-3. AI から Unity を操作できるようにする

```
unity pipeline install
```

これを入れると、AI が起動中の Unity に対して「このオブジェクトを置いて」「テストを流して」
といった操作を送れるようになる。

### A-4. Meta XR SDK を使う置き場所を指定する

`Assets/Scripts/XR/MosquitoSpray.XR.asmdef` の `references` に、Meta XR SDK の
アセンブリ名を書き足す。SDK が入っていない状態では名前を解決できないので、いまは空にしてある。

**`Adapter` と `Core` には書き足さない。** Meta XR SDK に依存するコードを
`Assets/Scripts/XR/` だけに閉じ込めるための指定で、他に広げると意味がなくなる。

急がなくてよい。最初の4機能は Meta XR SDK に触らないので、**5番目の機能
（コントローラー入力）に入る直前**までに済んでいればよい。

ここまで終わったらタグを打つ。

```
git tag -a setup-1-unity -m "Unity 6 + Meta XR SDK + Pipeline + XR の置き場所指定まで"
git push origin setup-1-unity
```

### B-5. テストの自動実行を動くようにする

**最初の機能に入る前に必須。**

テストには2種類ある。Unity を再生せずに走るもの（EditMode）と、実際に再生して走るもの
（PlayMode）。**自動実行では前者だけを走らせる。**後者まで走ると Unity が再生状態に入り、
人の作業が止まってしまう。

**この設定は済んでいる。**
[`.claude/hooks/run-editmode-tests.ps1`](../.claude/hooks/run-editmode-tests.ps1) に
`--mode EditMode` が入っている（Unity CLI 1.0.0-beta.8 で確認）。
Unity CLI は beta なので、動かなくなったら `unity test --help` で確認し直す。

残っているのは動作確認だけ。**わざと落ちるテストを1本書いて、AI の作業が
止まることを確認する。**止まらなければ、この仕組みは名前だけで機能していない。

> Claude Code を起動したあとに Unity を入れると、プロセスの PATH が古いままで
> `unity` が見つからない。スクリプトはレジストリから PATH を読み直し、それでも
> 駄目なら `%LOCALAPPDATA%/Unity/bin/unity.exe` を直接見るようにしてある。
> これが無いと、テストが走っていないのに素通りし続ける。

> スクリプトは **UTF-8 BOM 付き**で保存すること。Windows の PowerShell 5.1 は BOM が
> ないと `.ps1` を別の文字コードとして読み、日本語コメントで構文エラーになる。

### B-6. 仕様を書くための道具を入れる

```
npx cc-sdd@latest --claude-code --lang ja
```

`/kiro:spec-init`（機能の枠を作る）、`/kiro:spec-requirements`（要件を書く）といった
コマンドが入る。

入れたら **`.kiro/steering/` の3ファイルが上書きされていないか確認する。**
この道具は同じ場所に自前のテンプレートを置くので、手で書いた内容が消えることがある。

```
git status
git diff -- .kiro/steering/
```

消えていたら `git checkout -- .kiro/steering/` で戻す。

入ったコマンド（`.claude/commands/` など）は**コミットする**。
配布された人が各自で `npx` を実行するとバージョンがずれるため。

```
git tag -a setup-2-toolchain -m "テスト自動実行 + 仕様を書く道具まで"
git push origin setup-2-toolchain
```

### C-7. 何を作るかを自分の言葉で書く

[作るものの説明](external/mosquito-spray.md) は AI が書いた下書き。
**自分の言葉で書き直してから**次に進む。

これは顧客に見せる説明なので、数値（「噴射の範囲は15度」など）を書かない。
「手を伸ばした先に届く範囲」のように書く。それでいて、あとで機能ごとの仕様に
落とせるだけの中身は要る。

書き直した版が `main` の確定版になり、他のメンバーはこれを読んで仕様を書く。
全員が同じ説明から始めるので、あとで記録を横に並べて比べられる。

```
git tag -a setup-3-ready -m "作るものの説明の確定版あり。配布の標準形"
git push origin setup-3-ready
```

**ここまでが `main`。** 次から `run/<名前>` に移る。

### D-8. 最初の機能を作る

```
git switch -c run/<自分の名前>
git switch -c spec/spray-hit-core
```

`/kiro:spec-init` から始める。1機能あたりの流れは
[学習ロードマップ](learning-roadmap.md) の 3-4 にある。終えるたびに
[記録シート](learning-log.md) に書き込む。

終えた機能は `run/<名前>` に squash マージする。**`main` には戻さない。**

## リポジトリの構成

```
.claude/          AI の権限設定と、作業終了時に走るスクリプト
.kiro/steering/   AI が毎回読む決めごと（3ファイル・合計400行以内）
.kiro/specs/      作業中の機能の仕様
.kiro/archive/    終えた機能の仕様
Assets/Scripts/   Core（計算だけ）/ Adapter（Unity と繋ぐ）/ XR（Meta XR SDK 用）
Assets/Tests/     EditMode（再生せず走る）/ PlayMode（再生して走る）
docs/             ガイドライン、ロードマップ、作るものの説明、記録シート
```

それぞれの詳しい責務は [.kiro/steering/structure.md](../.kiro/steering/structure.md) にある。

## AI に守らせている仕組み

3つある。**どれも人の注意力ではなく、機械が止める。**

| 仕組み | 止めるもの | どうなるか |
| --- | --- | --- |
| 置き場所ごとのコンパイル境界 | `Assets/Scripts/Core/` に Unity 専用のコードを書くこと | コンパイルエラーになる |
| `.claude/settings.json` の拒否設定 | AI がシーン・プレハブ・`.meta` を直接書き換えること | AI の編集が拒否される |
| 作業終了時のテスト実行 | テストが落ちたまま AI が「完了」と言うこと | AI の作業が完了扱いにならない |

1つ目は、計算部分を Unity から切り離しておくための仕掛け。切り離してあると、Unity を
起動せずにテストできる。AI が「ここは Unity の機能を使えば早い」と判断してコードを
書き足すと、その時点でコンパイルが通らなくなる。

2つ目は、シーンやプレハブのファイルが壊れるのを防ぐため。これらは中身が複雑で、
テキストとして書き換えると簡単に壊れる。AI には Unity 経由で操作させる。

**ただし、AI が `sed` などのコマンド経由で書き換える抜け道は塞いでいない。**
ガイドライン第1章が認めている既知の穴で、コミット前の読み返しで拾う。
`.kiro/steering/tech.md` にも「迂回しない」と明記してある。

## Git の最初の設定

クローンした直後に一度だけ。

```
git lfs install
```

画像や音声などの大きいファイルを Git 本体ではなく別の場所に保存する仕組み。
これを入れておかないと、リポジトリが肥大化する。

`.gitattributes` で、シーンやプレハブに Unity 付属のマージ用ツールを指定してある。
使うには、そのツールの場所を Git に登録する必要がある。
一人で作業しているうちは衝突しないので、後回しでよい。
