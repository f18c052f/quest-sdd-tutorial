# structure — 置き場所と責務

## リポジトリ直下

| パス | 中身 | 更新するのは |
| --- | --- | --- |
| `Assets/` | Unity プロジェクト本体 | 人間と AI |
| `ProjectSettings/` `Packages/` | Unity の設定。テキストを手で編集しない | Unity Editor |
| `.kiro/steering/` | このファイルを含む共通前提 | 人間のみ |
| `.kiro/specs/<spec>/` | 進行中の spec（requirements / design / tasks） | AI と人間 |
| `.kiro/archive/` | 完了した spec | 人間 |
| `.claude/` | 権限設定と Stop フック | 人間のみ |
| `docs/` | ガイドライン、ロードマップ、外部設計、学習ログ | 人間 |

## `Assets/` の責務

| パス | 置くもの | 置かないもの |
| --- | --- | --- |
| `Assets/Scripts/Core/` | `UnityEngine` に依存しない計算と状態遷移 | MonoBehaviour、GameObject に触る型 |
| `Assets/Scripts/Adapter/` | MonoBehaviour、Core と GameObject の橋渡し | ドメインの計算、Meta XR SDK の型 |
| `Assets/Scripts/XR/` | Meta XR SDK に直接触る実装 | ドメインの計算 |
| `Assets/Tests/EditMode/` | Core と Adapter の EditMode テスト | PlayMode テスト |
| `Assets/Tests/PlayMode/` | Adapter の PlayMode テスト | EditMode テスト、XR 依存のテスト |
| `Assets/Scenes/` | シーン | ― |
| `Assets/Prefabs/` | プレハブ | そのシーンだけの一点もの |
| `Assets/Settings/` | ScriptableObject にした設定値 | コードの定数でよいもの |

調整しうる数値はコードに直書きせず `Assets/Settings/` の ScriptableObject に出す。
シーンを薄く保ち、差分を小さくするため。

## シーン

**シーンは `Assets/Scenes/Game.unity` の1つだけにする。**

ガイドラインの「1 spec が触るシーンは1つまで」を、シーンを1つしか作らないことで自動的に満たす。
待機・プレイ・結果表示は、シーンを分けずにこの中で切り替える。

シーンを増やしたくなったら、まず「プレハブの出し分けで足りないか」を考える。
それでも必要なら、増やすこと自体を `docs/learning-log.md` に記録する。

## spec の命名

`<domain>-<layer>` の形にする。名前を見ればどの層を触るか分かること。

| layer | 触ってよいもの |
| --- | --- |
| `-core` | `MosquitoSpray.Core` と EditMode テストのみ。シーンもプレハブも触らない |
| `-adapter` | `MosquitoSpray.Adapter` / `.XR`、テスト、プレハブ |
| `-scene` | `Game.unity` |

Core だけで閉じる spec を先に作る。シーンを触らないので、流れに慣れるのに向いている。

依存する spec は requirements.md の冒頭に1行で書く。依存が3つ以上あるなら分割を間違えている。

## `docs/` の使い分け

| パス | 中身 |
| --- | --- |
| `docs/quest-sdd-guideline.md` | 検証の対象にしているガイドライン本体 |
| `docs/learning-roadmap.md` | 学習の道筋 |
| `docs/external/` | 外部設計。**人間だけが書く**。spec より先に書く |
| `docs/references/` | L4 の参照画像・動画。design.md からはパスで示すだけ |
| `docs/verification/` | シミュレータでの確認記録 |
| `docs/learning-log.md` | spec ごとの記録と最後の振り返り。**ハンズオンの成果物**。`run/<名前>` に置く |
| `docs/retrospectives/` | 各自の振り返りを抜き出したもの。ここだけ `main` に戻す |

`docs/learning-log.md` はステアリングではない。AI が毎回読むものではなく、人間が書き足していく。

## 行数の上限

| ファイル | 上限の目安 |
| --- | --- |
| design.md | 800行 |
| tasks.md | 300行 |
| requirements.md | 250行 |
| ステアリング3ファイル合計 | 400行 |

このリポジトリには CI がないので、超過を止める仕組みはない。
spec を閉じるときに自分で数え、`docs/learning-log.md` に記録する。

## 1 spec の流れ

PR の代わりにローカルのブランチとコミットを使う。

| ガイドライン | ここでの代替 |
| --- | --- |
| Work Item | `.kiro/specs/<spec>/` そのもの。進行中は `specs`、完了は `archive` |
| 仕様PR | `spec/<spec>` ブランチの最初のコミット（仕様3ファイルだけ）。コミット前に自分で読み返す |
| 実装PR | 1〜3タスクごとのコミット |
| マージ | 全タスク完了後、`run/<名前>` へ squash マージ |

完了した spec は `.kiro/archive/` にすぐ移す。古い仕様を AI が読まないようにするため。

## ブランチ

この repo は他のメンバーにも配布する。**`main` は全員が同じ起点から始められる状態**に保つ。

| ブランチ | 中身 |
| --- | --- |
| `main` | 骨組み、Unity プロジェクト、SDK、cc-sdd、steering、外部設計の確定版、learning-log の雛形 |
| `run/<名前>` | その人の実走。spec、実装、記入済み learning-log |
| `spec/<spec>` | `run/<名前>` から切る。閉じたら `run/<名前>` へ戻す |

分ける基準は「誰がやっても同じ結果になるか」。同じになるものは `main`、
人によって違い、そこに学習があるものは `run/<名前>`。

`main` の節目にはタグを打ち、どの状態からでも clone できるようにする。
タグの一覧と意味は README にある。タグは人間が打つ。

**spec と実装を `main` に入れない。** 入れた時点で、次に受け取る人は演習ではなく答えを読むことになる。
`main` に戻すのは、骨組みそのものの改善と、最後の振り返り（`docs/retrospectives/<名前>.md`）だけ。
