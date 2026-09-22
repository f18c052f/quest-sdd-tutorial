# 準備の手順

[README](../README.md) の A〜C を自分でやる場合の手順。`setup-3-ready` から始めた人は
ここを読む必要はない（すべて済んでいる）。

最後に、このリポジトリの構成と、AI に守らせている仕組みの説明を置いてある。

## いまの状態

`main` がどこまで進んでいるか。**この表は `main` が進むたびに更新する。**
受け取った人が「自分はどこから始めるのか」を判断する材料になる。

| 済 | 項目 | 手順 |
| --- | --- | --- |
| ✅ | Git の設定（無視するファイル、バイナリの扱い、改行コード） | — |
| ✅ | 置き場所ごとのコンパイル境界（計算専用 / Unity と繋ぐ / Meta XR SDK 用 / テスト2種） | — |
| ✅ | AI にシーン・プレハブ・`.meta` を直接編集させない設定 | — |
| ✅ | AI が毎回読む決めごと3ファイル（`.kiro/steering/`） | — |
| ✅ | 記録シートの雛形、作るものの説明の下書き | — |
| ✅ | Unity のプロジェクト本体（URP、Unity 6000.6.2f1） | A-2 |
| ✅ | OpenXR と Meta XR Core SDK、シーンとカメラリグとパススルー | A-2 |
| ✅ | AI から起動中の Editor を操作する仕組み（Unity Pipeline） | A-3 |
| ✅ | テスト自動実行のコマンド確定（`unity test --mode EditMode`） | B-5 |
| ✅ | Meta XR SDK を使う置き場所の指定（`Oculus.VR`） | A-4 |
| ⬜ | テスト自動実行が本当に止めるかの確認 | B-5 |
| ⬜ | 仕様を書くための道具（cc-sdd） | B-6 |
| ⬜ | シミュレータ（実機なしで動かす唯一の手段） | B-7 |
| ⬜ | 作るものの説明の書き直し（人が書く） | C-7 |

## 手順

README の A〜D に対応している。上から順に。

| 段階 | 手順 | 終わったら |
| --- | --- | --- |
| A 環境を作る | A-1 〜 A-4 | `setup-1-unity` を打つ |
| B 道具を入れる | B-5 〜 B-7 | `setup-2-toolchain` を打つ |
| C 何を作るか決める | C-7 | `setup-3-ready` を打つ。**ここまでが `main`** |
| D 作る | D-8 | `run/<名前>` に移る |

### 誰がやるか

各手順の冒頭にも書いてあるが、先に一覧で示す。

| 手順 | 担当 | AI に任せられない理由 |
| --- | --- | --- |
| A-1 ツールを入れる | **自分** | GUI のインストーラとブラウザでのサインインがあり、AI は操作できない |
| A-2 Unity プロジェクト | **両方** | 雛形の作成とファイルの移動は AI。Project Validation の Fix と Building Blocks の配置は Editor の画面操作なので自分 |
| A-3 Editor 操作の仕組み | **AI** | コマンド1つ。ただし**そのあとの Editor 再起動は自分** |
| A-4 XR の置き場所指定 | **AI** | 設定ファイルの編集だけ |
| B-5 テスト自動実行の確認 | **両方** | AI がわざと落ちるテストを書く。止まったかを見るのは自分 |
| B-6 仕様を書く道具 | **自分** | 全員が同じ版を使うため、入れる人を1人に絞る（ガイドライン第4章） |
| B-7 シミュレータ | **両方** | 導入は AI。ツールバーからの有効化は Editor の画面操作なので自分 |
| C-7 作るものの説明 | **自分** | 外部設計は人間が書く（ガイドライン第1章 鉄則3） |
| D-8 以降の実装 | 層で分ける | [README の「AI と自分の分担」](../README.md#ai-と自分の分担)を見る |

**「AI」と書いてある手順は、Claude Code に「A-3 をやって」と頼めばよい。**
「自分」と書いてある手順は、下の手順どおりに自分で操作する。

---

### A-1. Unity と AI 用のツールを入れる

**担当: 自分。** GUI のインストーラとサインインがあるため、AI は代われない。

必要なものの一覧と、**何を入れると何ができるようになるか**は
[README の「必要なもの」](../README.md#必要なもの)にある。順に入れる。

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

**担当: 両方。** 1〜3 は AI に頼める。4（Project Validation）と 5（Building Blocks）は
Editor の画面操作なので自分でやる。

[学習ロードマップ](learning-roadmap.md) の Step 2。

やることは5つ。それぞれ下に手順がある。

1. Unity のプロジェクトとして成立させる
2. OpenXR を有効にする
3. Meta XR Core SDK を入れる
4. Project Validation の指摘を消す
5. シーンを作り、Building Blocks でカメラとパススルーを置く

（Meta XR Simulator は B-7 で入れる）

#### 1. Unity のプロジェクトとして成立させる

**`setup-1-unity` 以降のタグから始めた人は、この節を飛ばす。**
`unity open <このフォルダ>` で開けば済む。

`setup-0-skeleton` から始めた人だけ、ここを行う。このフォルダには `Assets/` しか無く
`ProjectSettings/` が無いので、**Unity Hub の Add では認識されない。**
別の場所に雛形を作り、設定だけを持ってくる。

```
unity projects create UrpScaffold --path <一時フォルダ>   --editor-version 6000.6.2f1 --template com.unity.template.urp-blank --no-cloud
```

**テンプレートは Universal 3D（URP）を使う。** Quest は URP が標準で、
Built-In Render Pipeline は使わない。

雛形から、このフォルダへ次をコピーする。

| コピーするもの | 理由 |
| --- | --- |
| `ProjectSettings/` | Unity のバージョンと各種設定。`.meta` を持たない |
| `Packages/` | パッケージの一覧 |
| `Assets/Settings/` の `.asset` と `.meta` 7個 | URP の描画設定。**無いとマテリアルが全部ピンクになる** |

雛形の `TutorialInfo/`・`Readme.asset`・`SampleScene.unity`・`InputSystem_Actions.inputactions`
は**持ってこない**。シーンは後で自分で作る。

最後に Hub に登録しておく。しないと `unity open` のたびに警告が出る。

```
unity projects add <このフォルダ>
```

開いたあと、Unity が `.meta` ファイルを大量に作る。これは**消さずにコミットする**のが正しい。
Unity がファイルの対応関係を記録しているもので、消すと参照が壊れる。

同時に `ProjectSettings/` と `Packages/manifest.json` も作られる。ここに Unity 本体と
Meta XR SDK のバージョンが記録される。**受け取った人はこれを見て同じバージョンを入れる**ので、
どちらも必ずコミットする。以降、バージョンの正はこのファイルになる。

#### 2. OpenXR を有効にする

Project Settings → XR Plug-in Management で、**Windows と Android の両タブ**の
OpenXR にチェックを入れる。両方に要る理由は次の節で書く。

#### 3. Meta XR Core SDK を入れる

`com.meta.xr.sdk.core` は **Unity 公式のレジストリにある**。スコープ付きレジストリの
追加は要らない。

**`Packages/manifest.json` を手で編集しない。** 依存解決が壊れる。Unity の
PackageManager API 経由で入れる（`Assets/Editor/ProjectBootstrap/PackageInstaller.cs`）。

```
"<Unity.exe のパス>" -batchmode -projectPath "<このフォルダ>"   -executeMethod ProjectBootstrap.PackageInstaller.Install -logFile install.log
```

> **バッチモードは Editor を閉じてから実行する。** 開いたままだと
> `It looks like another Unity instance is running with this project open.`
> で失敗する。テストの自動実行（`unity test`）が開いている Editor を使うのと逆なので
> 混同しやすい。**パッケージ操作だけは Editor を閉じる。**
>
> 失敗したらログを絞らずに全文を読む。関係のないライセンス警告が先に出るので、
> それを原因と読み違えやすい。

`-quit` を付けないこと。パッケージの解決は非同期で、`-quit` があると解決を待たずに
Editor が終了する。スクリプト側が自分で終了コードを返す。

#### 4. Project Validation の指摘を消す

Meta SDK が入ると、`Meta` → `Tools` → `Project Setup Tool` に設定の不備が並ぶ。
タブがプラットフォームごとに分かれている。**両方直す。**

| タブ | 何を決めるか | 必要度 |
| --- | --- | --- |
| Windows（PC, Mac & Linux Standalone） | **Editor の Play モード**。シミュレータで動かすのはこれ | 必須 |
| Android | Quest 実機向けの apk | 直接は効かないが直す |

Android を飛ばしてはいけない理由は2つ。**Android タブの指摘にはプロジェクト全体の設定が
混ざっている**（Color Space を Linear にする項目など）ので、Windows での見え方にも影響する。
もう1つは `ProjectSettings.asset` がコミットされて配布されるので、直しておけば
受け取った人が直った状態から始められる。

> **消えない警告が2件残る。これは正常。**
>
> ```
> [Meta XR Feature] This OpenXR Feature targets an API version with a patch
> version lower than 1.1.54 ... The requested API version will be ignored.
> ```
>
> OpenXR パッケージ側は条件を満たしている（loader 1.1.54 は `com.unity.xr.openxr`
> 1.17.0-pre.2 から入り、こちらは 1.18.0）。**Meta SDK の Feature が古い API を
> 要求していて、それが無視される**という意味なので、安全側に倒れている。
> Meta SDK が追従するまで消えない。エラーではなく警告なので、このまま進めてよい。

#### 5. シーンを作り、Building Blocks でカメラとパススルーを置く

**先にシーンを作る。**

1. `File` → `New Scene` → **Basic (URP)** を選ぶ
2. `Assets/Scenes/Game.unity` として保存する

このプロジェクトは**シーンを1つだけ**にする決まりなので、名前は `Game` にする
（`.kiro/steering/structure.md`）。

**次に Building Blocks。**

1. `Meta` → `Tools` → `Building Blocks` を開く
2. 一覧から **`Camera Rig`** を探す
3. **サムネイルを Hierarchy ウィンドウにドラッグ&ドロップ**する
4. 同じ要領で **`Passthrough`** を追加する

**`Camera Rig` を先に入れる。** `Passthrough` は単独では動かない。

パススルー系のブロックは6つあって紛らわしい。**`Passthrough` を選ぶ。**

| ブロック名 | 用途 |
| --- | --- |
| **`Passthrough`** | **部屋が見える基本のパススルー。これ** |
| `Passthrough Overlay` | パススルーを重ねて表示する |
| `Passthrough Window` | 壁に穴を開けたような表示 |
| `Passthrough Camera Access` | カメラ映像をコードから取る |
| `Passthrough Camera Visualizer` | 上のデバッグ表示 |
| `Surface Projected Passthrough` | 面に投影する |

> **シーンに `Main Camera` が残る。** Basic (URP) のシーンに最初からあるもので、
> Camera Rig を置いても消えない。放っておくと **`AudioListener` が2つ**になり
> Unity が警告を出す（Camera Rig 側の `CenterEyeAnchor` にも付いているため）。
> Hierarchy から `Main Camera` を削除する。`Directional Light` は残してよい。

終わると、Meta SDK が次を作る。**`APILayers~` 以外はコミットする。**

| 生成物 | 扱い |
| --- | --- |
| `Assets/Oculus/OculusProjectConfig.asset` | コミット。Quest 向けの設定 |
| `Assets/Plugins/Android/AndroidManifest.xml` | コミット。実機の権限設定 |
| `Assets/Resources/*.asset` | コミット。実行時の設定 |
| `Assets/XR/APILayers~/` | **`.gitignore` 済み。**7MB の DLL で、中身はパッケージ内の
ファイルと同一（ハッシュ一致を確認済み）。SDK が再配置するので追跡しない |

### A-3. AI から Unity を操作できるようにする

**担当: AI。** ただし**そのあとの Editor 再起動は自分**。

```
unity pipeline install
```

**これで何ができるようになるか**

入れる前の AI は、プロジェクトの**ファイルを読み書きできるだけ**だった。
入れると、**起動中の Editor そのものに指示を送れる**ようになる。

| | 入れる前 | 入れた後 |
| --- | --- | --- |
| C# コードの読み書き | できる | できる |
| Editor を閉じた状態でのパッケージ導入・ビルド | できる | できる |
| **シーンにオブジェクトを置く** | できない | **できる** |
| **C# をその場で実行して結果を見る** | できない | **できる** |
| **開いている Editor でテストを走らせる** | できない | **できる** |

3つ目が特に効く。このプロジェクトは AI にシーンのファイルを直接書き換えさせない決まりなので、
**シーンを触る唯一の経路がこれ**になる（`.kiro/steering/tech.md` の禁止事項2）。
これが無いと、シーンの構成はすべて人がやることになる。

**公式プラグインとは別物。**公式プラグインは Claude Code 側に入り、`unity` コマンドと
スキルを与える。Pipeline パッケージは Unity プロジェクト側に入り、起動中の Editor への
接続口を開ける。**両方要る。**

**入れたあと Editor を再起動する。** 接続サーバーは Editor の起動時に立ち上がるので、
入れただけでは繋がらない。確認はこの2つ。

```
unity pipeline list   # 「サーバー到達可能」が true か
unity status          # 接続中の Editor が行として出るか
```

`サーバー到達可能` が `false` のままなら、まだ繋がっていない。
`unity list` を叩くと「Pipeline サーバーに接続できません」と出る。

### A-4. Meta XR SDK を使う置き場所を指定する

**担当: AI。** 設定ファイルの編集だけなので任せてよい。

**この設定は済んでいる。**`Assets/Scripts/XR/MosquitoSpray.XR.asmdef` の
`references` に **`Oculus.VR`** が入っている。

Meta XR SDK には50以上のアセンブリがあるが、必要なのはこれ1つ。
シーンに置かれた `OVRCameraRig` `OVRManager` `OVRPassthroughLayer`、および
コントローラー入力の `OVRInput` がすべて `Oculus.VR` にある。

| 足したもの | 何のため |
| --- | --- |
| `Oculus.VR` | `OVRInput`（トリガー入力）、`OVRPassthroughLayer`、`OVRCameraRig` |

`Meta.XR.BuildingBlocks` は足していない。シーンに置かれた `BuildingBlock` コンポーネントの
アセンブリだが、こちらのコードから参照しないため。**要るものだけ足す。**

**`Adapter` と `Core` には書き足さない。** Meta XR SDK に依存するコードを
`Assets/Scripts/XR/` だけに閉じ込めるための指定で、他に広げると意味がなくなる。

自分でやる場合は、`.asmdef` の `references` に文字列を1つ足すだけ。

```json
"references": [
    "MosquitoSpray.Core",
    "MosquitoSpray.Adapter",
    "Oculus.VR"
]
```

書いただけでは合っているか分からない。**実際に使うコードを1本書いて、コンパイルが通るか
確かめる。**確認したら消す。

```csharp
// Assets/Scripts/XR/Probe.cs（確認後に削除する）
namespace MosquitoSpray.XR
{
    internal static class Probe
    {
        internal static bool RightTriggerPressed()
            => OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);
    }
}
```

```
unity command recompile
unity command recompile_status   # status が completed、errors が空なら通っている
```

ここまで終わったらタグを打つ。

```
git tag -a setup-1-unity -m "Unity 6 + Meta XR SDK + Pipeline + XR の置き場所指定まで"
git push origin setup-1-unity
```

### B-5. テストの自動実行を動くようにする

**担当: 両方。** AI がわざと落ちるテストを書く。止まったかを確認するのは自分。

**最初の機能に入る前に必須。**

テストには2種類ある。Unity を再生せずに走るもの（EditMode）と、実際に再生して走るもの
（PlayMode）。**自動実行では前者だけを走らせる。**後者まで走ると Unity が再生状態に入り、
人の作業が止まってしまう。

**この設定は済んでいる。**
[`.claude/hooks/run-editmode-tests.ps1`](../.claude/hooks/run-editmode-tests.ps1) に
`--mode EditMode` が入っている（Unity CLI 1.0.0-beta.10 で確認）。
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

**担当: 自分。** 全員が同じ版を使うよう、入れる人を1人に絞る（ガイドライン第4章）。

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

### B-7. シミュレータを入れる

**担当: AI が導入、自分が有効化。**

実機を使わないので、**これが唯一の「動かして確かめる」手段**になる。
実機のヘッドセットの見え方（視野角、解像度、コントローラー入力）を PC 上で再現する。

```
com.meta.xr.simulator
```

導入は A-2 と同じ仕組み（`Assets/Editor/ProjectBootstrap/PackageInstaller.cs` の
`PackagesToAdd` に足して、Editor を閉じてバッチモードで実行）。

> **版の採番が Core SDK と揃っていない。** Core SDK が 205 系なのに対し、
> シミュレータは 81 系（確認時点で 81.0.1）。同じ SDK の一部に見えて別系統なので、
> **入れたあとに Core SDK が壊れていないか確かめる。**
>
> ```
> unity command recompile
> unity command recompile_status   # errors が空か
> ```

入れたら Unity のツールバーから有効にする。ここは GUI 操作なので自分でやる。
有効になっているかは `Meta` → `Tools` → `Meta XR Simulator` の表示で分かる。

**シミュレータで分からないこと**（ガイドライン第9章）:

| 分からないこと | 理由 |
| --- | --- |
| フレームレート | PC の性能で動くので、実機の性能は分からない |
| パススルー下での実際の見え方 | 明るさや遮蔽は実機でしか分からない |
| 酔い・疲労・距離感 | 身体感覚が要る |

これらは実機を入手したときに確かめる。**シミュレータで問題なくても実機で問題ない保証はない。**

```
git tag -a setup-2-toolchain -m "テスト自動実行 + 仕様を書く道具 + シミュレータまで"
git push origin setup-2-toolchain
```

### C-7. 何を作るかを自分の言葉で書く

**担当: 自分。** 外部設計は人間が書く（ガイドライン第1章 鉄則3）。AI に書かせない。

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

**担当: 層で分ける。** [README の「AI と自分の分担」](../README.md#ai-と自分の分担)を見る。

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
Assets/Scenes/    Game.unity ひとつだけ
Assets/Settings/  URP の描画設定と、あとで足すゲームの設定値
Assets/Editor/    準備用の使い捨てスクリプト。ゲームのコードは置かない
Assets/Oculus/ Assets/Plugins/ Assets/Resources/   Meta SDK が作った設定。触らない
ProjectSettings/ Packages/                         Unity の設定。テキストを手で編集しない
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

3つ目が、このやり方の要。**AI に「終わりました」と言わせない**ための仕組みで、
AI が作業を終えようとするたびにテストが走り、落ちていれば作業が完了扱いにならない。
AI の自己申告ではなくテストの結果で判定する、というのがこのやり方の核心なので、
**ここが動いていないと全体が形だけになる。**だから B-5 で、わざと落ちるテストを書いて
本当に止まるかを確かめる手順を入れてある。

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
