using System;
using System.Numerics;

namespace MosquitoSpray.Core.Spray
{
    /// <summary>起点から向きの方向に伸びる円錐の噴射範囲。生成後は値が変わらない。</summary>
    public sealed class SprayVolume
    {
        /// <param name="origin">起点（メートル）</param>
        /// <param name="direction">向き。長さは無視して正規化する</param>
        /// <param name="halfAngle">中心軸から縁までの角度（度）。0 より大きく 90 より小さい</param>
        /// <param name="reach">起点からの到達距離（メートル）。0 より大きい</param>
        /// <exception cref="ArgumentException">向きを単位ベクトルにできない（長さ 0、NaN、無限大）</exception>
        /// <exception cref="ArgumentOutOfRangeException">半角または到達距離が受け付ける範囲の外（NaN を含む）</exception>
        public SprayVolume(Vector3 origin, Vector3 direction, float halfAngle, float reach)
        {
            var normalized = Vector3.Normalize(direction);
            // 範囲に入っていなければ拒否する形で書き、NaN も拒否する
            if (!(MathF.Abs(normalized.LengthSquared() - 1f) < 1e-3f))
                throw new ArgumentException("向きを単位ベクトルにできません。", nameof(direction));
            if (!(halfAngle > 0f && halfAngle < 90f))
                throw new ArgumentOutOfRangeException(nameof(halfAngle), halfAngle, "半角は 0 より大きく 90 より小さくしてください。");
            if (!(reach > 0f))
                throw new ArgumentOutOfRangeException(nameof(reach), reach, "到達距離は 0 より大きくしてください。");

            Origin = origin;
            Direction = normalized;
            HalfAngle = halfAngle;
            Reach = reach;
            _cosHalfAngle = (float)Math.Cos(halfAngle * Math.PI / 180.0);
        }

        // 境界ちょうどの位置が浮動小数の誤差で外れにならないための許容誤差
        private const float DistanceTolerance = 1e-5f; // メートル
        private const float CosTolerance = 1e-6f;

        private readonly float _cosHalfAngle;

        public Vector3 Origin { get; }

        /// <summary>正規化した単位ベクトル。</summary>
        public Vector3 Direction { get; }

        public float HalfAngle { get; }

        public float Reach { get; }

        /// <summary>位置が噴射範囲に入っていれば true（命中）。境界ちょうどは命中。値は変えない。</summary>
        /// <param name="position">判定する位置（メートル）</param>
        public bool Contains(Vector3 position)
        {
            var offset = position - Origin;
            var distance = offset.Length();
            // 起点と同じ位置は方向が決まらない（0 で割る）ので、角度を求める前に命中とする
            if (distance == 0f)
                return true;
            if (distance > Reach + DistanceTolerance)
                return false;
            // 真後ろは cos = -1 で、半角 < 90° なら必ず外れになる
            return Vector3.Dot(Direction, offset) / distance >= _cosHalfAngle - CosTolerance;
        }
    }
}
