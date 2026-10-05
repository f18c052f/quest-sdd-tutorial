using System;
using System.Numerics;

namespace MosquitoSpray.Core.Rounds
{
    /// <summary>1ラウンドの状態（待機・プレイ・結果）と、残り時間・撃墜数・スコア・定位置を持つ。</summary>
    public sealed class Round
    {
        // 残り時間がこの値以下なら時間切れとみなす（float の足し引きで残る誤差を吸収する）
        const float TimeTolerance = 1e-5f;

        // 結果に入ってから時間を進めた秒数。結果に入るときは 0 から始め、残り時間を超えた分は数えない
        float restartWaitElapsed;

        /// <exception cref="ArgumentNullException">rules が null</exception>
        public Round(RoundRules rules)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Phase = RoundPhase.Waiting;
            RemainingTime = rules.TimeLimit;
        }

        /// <summary>生成時に渡された設定値。</summary>
        public RoundRules Rules { get; }

        /// <summary>現在の状態。</summary>
        public RoundPhase Phase { get; private set; }

        /// <summary>残り時間（秒）。</summary>
        public float RemainingTime { get; private set; }

        /// <summary>撃墜数。</summary>
        public int DownedCount { get; private set; }

        /// <summary>スコア。</summary>
        public int Score { get; private set; }

        /// <summary>プレイ開始時の位置。プレイ中だけ値を持つ。</summary>
        public Vector3? HomePosition { get; private set; }

        /// <summary>定位置から離れすぎている間 true。プレイ中だけ立つ（待機・結果では常に false）。</summary>
        public bool IsStrayWarning { get; private set; }

        /// <summary>
        /// 待機ならプレイを始め、定位置を記録する。結果で受付待ちの秒数が経っていれば待機に戻す（この呼び出しではプレイを始めない）。
        /// </summary>
        /// <returns>状態が変わったとき true</returns>
        public bool RequestStart(Vector3 playerPosition)
        {
            if (Phase == RoundPhase.Result)
            {
                if (restartWaitElapsed < Rules.RestartDelay - TimeTolerance)
                    return false;

                Phase = RoundPhase.Waiting;
                RemainingTime = Rules.TimeLimit;
                DownedCount = 0;
                Score = 0;
                return true;
            }

            if (Phase != RoundPhase.Waiting)
                return false;

            Phase = RoundPhase.Playing;
            RemainingTime = Rules.TimeLimit;
            DownedCount = 0;
            Score = 0;
            HomePosition = playerPosition;
            IsStrayWarning = false; // 警告なしから始める
            return true;
        }

        /// <summary>撃墜を知らせる。プレイ中だけ、撃墜数とスコアを 1 ずつ増やす。待機・結果では無視する。</summary>
        public void NotifyDowned()
        {
            if (Phase != RoundPhase.Playing)
                return;

            DownedCount += 1;
            Score += 1;
        }

        /// <summary>刺されたことを知らせる。プレイ中でスコアが 1 以上のときだけスコアを 1 減らす（撃墜数は変えない）。待機・結果では無視する。</summary>
        public void NotifyBitten()
        {
            if (Phase != RoundPhase.Playing || Score < 1)
                return;

            Score -= 1;
        }

        /// <summary>
        /// プレイヤー位置から離脱警告を更新する。プレイ中だけ、定位置からの水平距離で警告を立てたり解いたりする（出入りで距離を変える）。
        /// 待機・結果では警告なしにする。状態・残り時間・撃墜数・スコア・定位置は変えない。
        /// </summary>
        /// <returns>更新後の警告の有無</returns>
        public bool UpdateStray(Vector3 playerPosition)
        {
            IsStrayWarning = HomePosition.HasValue
                && StrayJudge.ShouldWarn(HomePosition.Value, playerPosition, IsStrayWarning, Rules);
            return IsStrayWarning;
        }

        /// <summary>時間を進める。</summary>
        /// <returns>この呼び出しでラウンドが終わったとき true</returns>
        /// <exception cref="ArgumentOutOfRangeException">deltaTime が負・NaN・無限大。何も変えない</exception>
        public bool Advance(float deltaTime)
        {
            if (!(deltaTime >= 0f) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "経過秒数は 0 以上の有限値にしてください。");

            if (Phase == RoundPhase.Result)
            {
                restartWaitElapsed += deltaTime;
                return false;
            }

            if (Phase != RoundPhase.Playing)
                return false;

            RemainingTime -= deltaTime;
            if (RemainingTime > TimeTolerance)
                return false;

            RemainingTime = 0f;
            restartWaitElapsed = 0f;
            Phase = RoundPhase.Result;
            HomePosition = null;
            IsStrayWarning = false;
            return true;
        }
    }
}
