using System;

namespace MosquitoSpray.Core.Mosquitoes
{
    /// <summary>刺された判定・出現・接近の設定値。検証済みで、生成後は値が変わらない。既定値は持たない。</summary>
    public sealed class MosquitoRules
    {
        /// <param name="biteDistance">刺された判定の距離（メートル）。顔からこの距離以下で秒数を足す。0 より大きい</param>
        /// <param name="biteDuration">刺されるまでの累積秒数。0 より大きい</param>
        /// <param name="spawnMinRadius">顔からの水平距離の下限（メートル）。0 以上</param>
        /// <param name="spawnMaxRadius">顔からの水平距離の上限（メートル）。下限以上</param>
        /// <param name="spawnMinHeight">顔の高さとの差の下限（メートル）。負でもよい</param>
        /// <param name="spawnMaxHeight">顔の高さとの差の上限（メートル）。下限以上</param>
        /// <param name="maxCount">接近中の蚊の同時数の上限（匹）。1 以上</param>
        /// <param name="spawnInterval">出現どうしの最短間隔（秒）。0 より大きい</param>
        /// <param name="approachSpeed">顔へ近づく速さ（メートル毎秒）。0 より大きい</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 値が受け付ける範囲の外（NaN と無限大を含む）。引数の並び順に検査し、最初に見つかった不正な値の引数名を知らせる
        /// </exception>
        public MosquitoRules(
            float biteDistance,
            float biteDuration,
            float spawnMinRadius,
            float spawnMaxRadius,
            float spawnMinHeight,
            float spawnMaxHeight,
            int maxCount,
            float spawnInterval,
            float approachSpeed)
        {
            // 範囲に入っていなければ拒否する形で書き、NaN も拒否する。無限大は IsFinite で拒否する
            if (!(IsFinite(biteDistance) && biteDistance > 0f))
                throw new ArgumentOutOfRangeException(nameof(biteDistance), biteDistance, "刺された判定の距離は 0 より大きい有限値にしてください。");
            if (!(IsFinite(biteDuration) && biteDuration > 0f))
                throw new ArgumentOutOfRangeException(nameof(biteDuration), biteDuration, "刺されるまでの秒数は 0 より大きい有限値にしてください。");
            if (!(IsFinite(spawnMinRadius) && spawnMinRadius >= 0f))
                throw new ArgumentOutOfRangeException(nameof(spawnMinRadius), spawnMinRadius, "出現範囲の内側の水平距離は 0 以上の有限値にしてください。");
            if (!(IsFinite(spawnMaxRadius) && spawnMaxRadius >= spawnMinRadius))
                throw new ArgumentOutOfRangeException(nameof(spawnMaxRadius), spawnMaxRadius, "出現範囲の外側の水平距離は内側以上の有限値にしてください。");
            if (!IsFinite(spawnMinHeight))
                throw new ArgumentOutOfRangeException(nameof(spawnMinHeight), spawnMinHeight, "出現範囲の高さの下限は有限値にしてください。");
            if (!(IsFinite(spawnMaxHeight) && spawnMaxHeight >= spawnMinHeight))
                throw new ArgumentOutOfRangeException(nameof(spawnMaxHeight), spawnMaxHeight, "出現範囲の高さの上限は下限以上の有限値にしてください。");
            if (!(maxCount >= 1))
                throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "同時数の上限は 1 以上にしてください。");
            if (!(IsFinite(spawnInterval) && spawnInterval > 0f))
                throw new ArgumentOutOfRangeException(nameof(spawnInterval), spawnInterval, "出現間隔は 0 より大きい有限値にしてください。");
            if (!(IsFinite(approachSpeed) && approachSpeed > 0f))
                throw new ArgumentOutOfRangeException(nameof(approachSpeed), approachSpeed, "接近の速さは 0 より大きい有限値にしてください。");

            BiteDistance = biteDistance;
            BiteDuration = biteDuration;
            SpawnMinRadius = spawnMinRadius;
            SpawnMaxRadius = spawnMaxRadius;
            SpawnMinHeight = spawnMinHeight;
            SpawnMaxHeight = spawnMaxHeight;
            MaxCount = maxCount;
            SpawnInterval = spawnInterval;
            ApproachSpeed = approachSpeed;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>刺された判定の距離（メートル）。</summary>
        public float BiteDistance { get; }

        /// <summary>刺されるまでの累積秒数。</summary>
        public float BiteDuration { get; }

        /// <summary>顔からの水平距離の下限（メートル）。</summary>
        public float SpawnMinRadius { get; }

        /// <summary>顔からの水平距離の上限（メートル）。</summary>
        public float SpawnMaxRadius { get; }

        /// <summary>顔の高さとの差の下限（メートル）。</summary>
        public float SpawnMinHeight { get; }

        /// <summary>顔の高さとの差の上限（メートル）。</summary>
        public float SpawnMaxHeight { get; }

        /// <summary>接近中の蚊の同時数の上限（匹）。</summary>
        public int MaxCount { get; }

        /// <summary>出現どうしの最短間隔（秒）。</summary>
        public float SpawnInterval { get; }

        /// <summary>顔へ近づく速さ（メートル毎秒）。</summary>
        public float ApproachSpeed { get; }
    }
}
