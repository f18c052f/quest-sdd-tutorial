using System.Numerics;

namespace MosquitoSpray.Core.Mosquitoes
{
    /// <summary>蚊1匹の識別子・位置・状態・累積秒数。公開するのは読み取りだけで、変更は <see cref="MosquitoSwarm"/> からだけ行う。</summary>
    public sealed class Mosquito
    {
        internal Mosquito(int id, Vector3 position)
        {
            Id = id;
            Position = position;
            State = MosquitoState.Approaching;
            BiteTime = 0f;
        }

        /// <summary>群れの中で一意の識別子。変わらない。</summary>
        public int Id { get; }

        /// <summary>位置（メートル）。撃墜または刺したあとは変わらない。</summary>
        public Vector3 Position { get; private set; }

        /// <summary>状態。</summary>
        public MosquitoState State { get; private set; }

        /// <summary>顔の近くにいた累積秒数。減らない。</summary>
        public float BiteTime { get; private set; }

        // 距離と秒数の許容誤差。float の丸めで境界の判定が外れないようにする
        const float DistanceTolerance = 1e-5f;
        const float TimeTolerance = 1e-5f;

        /// <summary>
        /// 接近中なら、顔の位置へまっすぐ <see cref="MosquitoRules.ApproachSpeed"/>×<paramref name="deltaTime"/> だけ近づき（通り過ぎるなら顔の位置で止まる）、
        /// そのあとの顔との距離が <see cref="MosquitoRules.BiteDistance"/> 以下（許容誤差 1e-5m を含む）なら <paramref name="deltaTime"/> を累積秒数に足す。範囲外では足さず、0 にも戻さない。
        /// 累積秒数が <see cref="MosquitoRules.BiteDuration"/> 以上（許容誤差 1e-5 秒を含む）になったら刺した状態にして true を返す。
        /// 接近中でなければ何もせず false。家具・壁・ほかの蚊との当たり判定は持たない。
        /// </summary>
        internal bool Advance(float deltaTime, Vector3 face, MosquitoRules rules)
        {
            if (State != MosquitoState.Approaching) return false;

            Approach(deltaTime, face, rules.ApproachSpeed);

            if ((face - Position).Length() <= rules.BiteDistance + DistanceTolerance)
                BiteTime += deltaTime;

            if (BiteTime < rules.BiteDuration - TimeTolerance) return false;

            State = MosquitoState.Bitten;
            return true;
        }

        void Approach(float deltaTime, Vector3 face, float speed)
        {
            var toFace = face - Position;
            var distance = toFace.Length();
            if (distance == 0f) return;

            var step = speed * deltaTime;
            Position = distance <= step ? face : Position + toFace / distance * step;
        }

        /// <summary>接近中なら撃墜にして true を返す。位置はそのまま変わらない。接近中でなければ何もせず false。</summary>
        internal bool Down()
        {
            if (State != MosquitoState.Approaching) return false;

            State = MosquitoState.Downed;
            return true;
        }
    }
}
