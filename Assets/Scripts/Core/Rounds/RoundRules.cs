using System;

namespace MosquitoSpray.Core.Rounds
{
    /// <summary>ラウンドの制限時間・受付待ち・離脱判定の2つの距離の設定値。検証済みで、生成後は値が変わらない。既定値は持たない。</summary>
    public sealed class RoundRules
    {
        /// <param name="timeLimit">1ラウンドの制限時間（秒）。0 より大きい</param>
        /// <param name="restartDelay">結果に入ってから開始の指示を受け付けない時間（秒）。0 以上</param>
        /// <param name="strayWarnDistance">定位置からの水平距離がこれ以上で警告を出す（メートル）。0 より大きい</param>
        /// <param name="strayClearDistance">定位置からの水平距離がこれ未満で警告を消す（メートル）。0 より大きく、出す距離以下</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 値が受け付ける範囲の外（NaN と無限大を含む）。引数の並び順に検査し、最初に見つかった不正な値の引数名を知らせる
        /// </exception>
        public RoundRules(
            float timeLimit,
            float restartDelay,
            float strayWarnDistance,
            float strayClearDistance)
        {
            // 範囲に入っていなければ拒否する形で書き、NaN も拒否する。無限大は IsFinite で拒否する
            if (!(IsFinite(timeLimit) && timeLimit > 0f))
                throw new ArgumentOutOfRangeException(nameof(timeLimit), timeLimit, "制限時間は 0 より大きい有限値にしてください。");
            if (!(IsFinite(restartDelay) && restartDelay >= 0f))
                throw new ArgumentOutOfRangeException(nameof(restartDelay), restartDelay, "受付待ちの秒数は 0 以上の有限値にしてください。");
            if (!(IsFinite(strayWarnDistance) && strayWarnDistance > 0f))
                throw new ArgumentOutOfRangeException(nameof(strayWarnDistance), strayWarnDistance, "警告を出す距離は 0 より大きい有限値にしてください。");
            if (!(IsFinite(strayClearDistance) && strayClearDistance > 0f && strayClearDistance <= strayWarnDistance))
                throw new ArgumentOutOfRangeException(nameof(strayClearDistance), strayClearDistance, "警告を消す距離は 0 より大きく、警告を出す距離以下の有限値にしてください。");

            TimeLimit = timeLimit;
            RestartDelay = restartDelay;
            StrayWarnDistance = strayWarnDistance;
            StrayClearDistance = strayClearDistance;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>1ラウンドの制限時間（秒）。</summary>
        public float TimeLimit { get; }

        /// <summary>結果に入ってから開始の指示を受け付けない時間（秒）。</summary>
        public float RestartDelay { get; }

        /// <summary>定位置からの水平距離がこれ以上で警告を出す距離（メートル）。</summary>
        public float StrayWarnDistance { get; }

        /// <summary>定位置からの水平距離がこれ未満で警告を消す距離（メートル）。</summary>
        public float StrayClearDistance { get; }
    }
}
