using System;
using System.Collections.Generic;
using System.Numerics;

namespace MosquitoSpray.Core.Mosquitoes
{
    /// <summary>蚊の群れ。出現の開始と停止、時間経過を受け付ける公開の入口。時計を持たず、時間は <see cref="Advance"/> の引数でだけ進む。</summary>
    public sealed class MosquitoSwarm
    {
        static readonly IReadOnlyList<Mosquito> NoMosquitoes = Array.Empty<Mosquito>();

        readonly IRandomSource _random;
        readonly List<Mosquito> _mosquitoes = new List<Mosquito>();
        int _nextId = 1;
        float _sinceLastSpawn;

        /// <exception cref="ArgumentNullException"><paramref name="rules"/> または <paramref name="random"/> が null</exception>
        public MosquitoSwarm(MosquitoRules rules, IRandomSource random)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>この群れの設定値。</summary>
        public MosquitoRules Rules { get; }

        /// <summary>出現中か。作った直後は false。</summary>
        public bool IsSpawning { get; private set; }

        /// <summary>接近中の蚊。出現した順。</summary>
        public IReadOnlyList<Mosquito> Mosquitoes => _mosquitoes;

        /// <summary>出現を始める。開始中に呼んでも同じ（蚊を空に戻してやり直す）。識別子の連番は戻さない。直前の出現からの経過秒数は出現間隔にそろえ、開始後の最初の時間経過で 1 匹出るようにする。</summary>
        public void Start()
        {
            _mosquitoes.Clear();
            _sinceLastSpawn = Rules.SpawnInterval;
            IsSpawning = true;
        }

        /// <summary>蚊をすべて取り除き、出現を止める。止まっているときに呼んでも何もしない。</summary>
        public void Stop()
        {
            _mosquitoes.Clear();
            IsSpawning = false;
        }

        /// <summary>時間を進める。まず接近中の蚊を動かし、動いたあとの顔との距離で刺された判定をして、刺した蚊を一覧から外し結果の Bitten に入れる（一覧の順。同じ蚊は二度入らない）。そのあと直前の出現からの経過秒数を足し、出現間隔以上で接近中の数が上限未満の間、1 匹ずつ出す。1 回で複数匹出ることもある。上限に達している間は、経過秒数を出現間隔で頭打ちにする。</summary>
        /// <param name="deltaTime">経過秒数。0 以上の有限値</param>
        /// <param name="face">顔の位置（メートル）。検証しない</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="deltaTime"/> が負、NaN、無限大。このとき何も変えない</exception>
        public AdvanceResult Advance(float deltaTime, Vector3 face)
        {
            if (!(!float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime) && deltaTime >= 0f))
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "経過秒数は 0 以上の有限値にしてください。");

            if (!IsSpawning)
                return new AdvanceResult(NoMosquitoes, NoMosquitoes);

            // 既存の蚊を先に動かし、動いたあとの距離で刺された判定をする。このあと出る蚊はこの時間経過では動かず、判定もされない。障害物との当たり判定は持たない
            List<Mosquito> bitten = null;
            foreach (var existing in _mosquitoes)
            {
                if (existing.Advance(deltaTime, face, Rules))
                    (bitten ??= new List<Mosquito>()).Add(existing);
            }

            // 刺した蚊は一覧から外す（走査が終わってから）。空いた枠は同じ時間経過の出現判定で埋まることがある
            if (bitten != null)
                _mosquitoes.RemoveAll(m => m.State == MosquitoState.Bitten);

            _sinceLastSpawn += deltaTime;

            List<Mosquito> spawned = null;
            while (_sinceLastSpawn >= Rules.SpawnInterval && _mosquitoes.Count < Rules.MaxCount)
            {
                var mosquito = Spawn(face);
                _mosquitoes.Add(mosquito);
                _sinceLastSpawn -= Rules.SpawnInterval;
                (spawned ??= new List<Mosquito>()).Add(mosquito);
            }

            // 上限に達している間は待ち時間を貯めない。貯めると、撃墜で空いた枠が 2 匹以上いっぺんに埋まる
            if (_mosquitoes.Count >= Rules.MaxCount && _sinceLastSpawn > Rules.SpawnInterval)
                _sinceLastSpawn = Rules.SpawnInterval;

            return new AdvanceResult(spawned ?? NoMosquitoes, bitten ?? NoMosquitoes);
        }

        /// <summary>命中を受けて、接近中の蚊を撃墜にして一覧から外す。位置は撃墜した時点のまま変わらない。外れた枠は次の <see cref="Advance"/> で、出現間隔が経過していれば 1 匹だけ埋まる。</summary>
        /// <param name="id">命中した蚊の識別子</param>
        /// <param name="downed">撃墜した蚊。失敗のときは null</param>
        /// <returns>撃墜できたら true。撃墜済み・刺した・存在しない・停止で取り除かれた蚊なら false（何も変えない）</returns>
        public bool TryHit(int id, out Mosquito downed)
        {
            for (var i = 0; i < _mosquitoes.Count; i++)
            {
                var mosquito = _mosquitoes[i];
                if (mosquito.Id != id || !mosquito.Down()) continue;

                _mosquitoes.RemoveAt(i);
                downed = mosquito;
                return true;
            }

            downed = null;
            return false;
        }

        // 乱数は角度・水平距離・高さの順に 3 回だけ引く。Y が上
        Mosquito Spawn(Vector3 face)
        {
            var angle = 2f * MathF.PI * NextUnit();
            var radius = Rules.SpawnMinRadius + (Rules.SpawnMaxRadius - Rules.SpawnMinRadius) * NextUnit();
            var height = Rules.SpawnMinHeight + (Rules.SpawnMaxHeight - Rules.SpawnMinHeight) * NextUnit();
            var position = face + new Vector3(radius * MathF.Cos(angle), height, radius * MathF.Sin(angle));
            return new Mosquito(_nextId++, position);
        }

        // 0〜1 に収める。NaN は 0
        float NextUnit()
        {
            var value = _random.NextFloat();
            if (float.IsNaN(value) || value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
