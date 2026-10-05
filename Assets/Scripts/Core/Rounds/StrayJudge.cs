using System;
using System.Numerics;

namespace MosquitoSpray.Core.Rounds
{
    /// <summary>離脱判定の式。状態を持たず、ラウンドの状態も知らない。</summary>
    public static class StrayJudge
    {
        /// <summary>境界ちょうどの比較で float の誤差を吸収する許容誤差（メートル）。公開しない。</summary>
        private const float DistanceTolerance = 1e-5f;

        /// <summary>
        /// 定位置からの水平距離（高さは無視）と直前の警告の有無から、警告すべきかを返す。
        /// 警告なしなら「警告を出す距離 − 許容誤差」以上で警告する。警告中なら「警告を消す距離 − 許容誤差」未満で解除し、それ以外は続ける。
        /// </summary>
        /// <param name="home">定位置</param>
        /// <param name="position">プレイヤーの位置</param>
        /// <param name="warning">直前に警告していたか</param>
        /// <param name="rules">設定値</param>
        /// <exception cref="ArgumentNullException"><paramref name="rules"/> が null</exception>
        public static bool ShouldWarn(Vector3 home, Vector3 position, bool warning, RoundRules rules)
        {
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));

            // Y 軸が上。XZ 平面の距離だけを見る
            float dx = position.X - home.X;
            float dz = position.Z - home.Z;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);

            if (!warning)
                return distance >= rules.StrayWarnDistance - DistanceTolerance;

            return !(distance < rules.StrayClearDistance - DistanceTolerance);
        }
    }
}
