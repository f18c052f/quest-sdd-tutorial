using System.Linq;
using System.Numerics;
using MosquitoSpray.Core.Mosquitoes;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Mosquitoes
{
    // 刺された判定（動いたあとの距離で累積秒数を足し、足りたら刺して一覧から外す）のテスト。刺す設定値を使う
    public sealed partial class MosquitoSwarmTests
    {
        // 乱数 (0,0,0) で顔 + (radius, 0, 0) に出る設定値。刺す距離・秒数・速さ・上限・間隔を選べる
        static MosquitoRules BiteRules(
            float radius,
            float biteDistance = 0.3f,
            float biteDuration = 2.0f,
            float approachSpeed = 0.5f,
            int maxCount = 1,
            float spawnInterval = 100f) =>
            new MosquitoRules(biteDistance, biteDuration, radius, radius, 0f, 0f, maxCount, spawnInterval, approachSpeed);

        [Test]
        public void 顔の位置で2_0秒経つと刺して結果に入り状態が刺したになり一覧から外れる()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f));

            var result = swarm.Advance(2.0f, Face);

            Assert.That(result.Bitten.Select(m => m.Id), Is.EqualTo(new[] { mosquito.Id }));
            Assert.That(result.Bitten[0], Is.SameAs(mosquito));
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Bitten));
            Assert.That(mosquito.Position, Is.EqualTo(Face));
            Assert.That(swarm.Mosquitoes.Any(m => m.Id == mosquito.Id), Is.False);
        }

        [Test]
        public void 刺した位置は動いたあとの位置で以後の時間経過でも変わらない()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0.3f, biteDistance: 0.3f));

            var result = swarm.Advance(2.0f, Face);
            var positionAtBite = mosquito.Position;
            swarm.Advance(5.0f, Face + new Vector3(3f, 0f, 0f));

            Assert.That(result.Bitten.Single(), Is.SameAs(mosquito));
            AssertNear(positionAtBite, Face);
            Assert.That(mosquito.Position, Is.EqualTo(positionAtBite));
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Bitten));
        }

        [Test]
        public void 刺した蚊は以後の時間経過の結果に入らない()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f));
            swarm.Advance(2.0f, Face);
            var biteTimeAtBite = mosquito.BiteTime;

            for (var i = 0; i < 5; i++)
            {
                var result = swarm.Advance(2.0f, Face);
                Assert.That(result.Bitten.Any(m => m.Id == mosquito.Id), Is.False);
            }

            Assert.That(mosquito.BiteTime, Is.EqualTo(biteTimeAtBite));
        }

        [Test]
        public void 顔の位置で1_99秒では刺さずBiteTimeが経過秒数ぶん増える()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f));

            var result = swarm.Advance(1.99f, Face);

            Assert.That(result.Bitten, Is.Empty);
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Approaching));
            Assert.That(mosquito.BiteTime, Is.EqualTo(1.99f).Within(1e-6f));
            Assert.That(swarm.Mosquitoes.Any(m => m.Id == mosquito.Id), Is.True);
        }

        [Test]
        public void 顔の位置で0_1秒を20回進めても刺す()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f));

            for (var i = 0; i < 19; i++)
                Assert.That(swarm.Advance(0.1f, Face).Bitten, Is.Empty, $"{i + 1}回目で刺した");
            var result = swarm.Advance(0.1f, Face);

            Assert.That(result.Bitten.Single(), Is.SameAs(mosquito));
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Bitten));
        }

        [Test]
        public void 動いたあとの距離がちょうど0_3mなら秒数を足す()
        {
            // 0.8m から 1 秒で 0.5m 近づくと 0.3m
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0.8f));

            swarm.Advance(1.0f, Face);

            Assert.That(mosquito.BiteTime, Is.EqualTo(1.0f));
        }

        [Test]
        public void 動いたあとの距離が0_3mをごくわずかに超えても許容誤差の内なら秒数を足す()
        {
            // 動いたあとの距離は 0.300005m。許容誤差 1e-5m の内
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0.800005f));

            swarm.Advance(1.0f, Face);

            Assert.That(mosquito.BiteTime, Is.EqualTo(1.0f));
        }

        [Test]
        public void 動いたあとの距離が0_301mなら秒数を足さない()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0.801f));

            swarm.Advance(1.0f, Face);

            Assert.That(mosquito.BiteTime, Is.EqualTo(0f));
        }

        [Test]
        public void 距離は動く前でなく動いたあとで測る()
        {
            // 動く前は 0.8m で範囲外、動いたあとは 0.3m で範囲内
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0.8f));
            Assert.That(Vector3.Distance(mosquito.Position, Face), Is.GreaterThan(0.3f));

            swarm.Advance(1.0f, Face);

            Assert.That(mosquito.BiteTime, Is.GreaterThan(0f));
        }

        [Test]
        public void 範囲を出て戻っても累積が続き離れている間は変わらない()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f, approachSpeed: 0.1f));
            swarm.Advance(1.0f, Face);
            Assert.That(mosquito.BiteTime, Is.EqualTo(1.0f).Within(1e-6f));

            // 顔が 1m 離れる。3 秒で 0.3m しか近づけず、0.7m 残って範囲外
            swarm.Advance(3.0f, Face + new Vector3(1f, 0f, 0f));
            Assert.That(mosquito.BiteTime, Is.EqualTo(1.0f).Within(1e-6f));
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Approaching));

            // 顔が蚊の位置に戻る
            var result = swarm.Advance(1.0f, mosquito.Position);

            Assert.That(result.Bitten.Single(), Is.SameAs(mosquito));
            Assert.That(mosquito.BiteTime, Is.EqualTo(2.0f).Within(1e-6f));
        }

        [Test]
        public void 片方の蚊の秒数がもう片方に足されない()
        {
            // 1 匹目は顔の位置、2 匹目は顔から 5m
            var rules = new MosquitoRules(0.3f, 2.0f, 0f, 5f, 0f, 0f, 2, 0.1f, 0.5f);
            var swarm = CreateSwarm(rules, new FixedRandomSource(0f, 0f, 0f, 0f, 1f, 0f));
            swarm.Start();
            var spawned = swarm.Advance(0.1f, Face).Spawned;
            var near = spawned[0];
            var far = spawned[1];
            Assert.That(Vector3.Distance(near.Position, Face), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(Vector3.Distance(far.Position, Face), Is.EqualTo(5f).Within(1e-4f));

            var result = swarm.Advance(2.0f, Face);

            Assert.That(result.Bitten.Any(m => m.Id == near.Id), Is.True);
            Assert.That(near.State, Is.EqualTo(MosquitoState.Bitten));
            Assert.That(far.BiteTime, Is.EqualTo(0f));
            Assert.That(far.State, Is.EqualTo(MosquitoState.Approaching));
            Assert.That(result.Bitten.Any(m => m.Id == far.Id), Is.False);
        }

        [Test]
        public void 仮の初期値以外の設定値では刺されるまでの秒数が変わる()
        {
            // 刺す距離 0.1m、秒数 0.5、速さ 3m/s。顔から 0.2m の蚊は最初は範囲外
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0.2f, biteDistance: 0.1f, biteDuration: 0.5f, approachSpeed: 3.0f));
            Assert.That(Vector3.Distance(mosquito.Position, Face), Is.GreaterThan(0.1f));

            // 0.03m しか近づかず 0.17m 残って範囲外
            swarm.Advance(0.01f, Face);
            Assert.That(mosquito.BiteTime, Is.EqualTo(0f));

            // 顔に着いて範囲内。ここから秒数が足される
            swarm.Advance(0.1f, Face);
            Assert.That(mosquito.BiteTime, Is.EqualTo(0.1f).Within(1e-6f));

            var notYet = swarm.Advance(0.39f, Face);
            Assert.That(notYet.Bitten, Is.Empty);
            Assert.That(mosquito.BiteTime, Is.EqualTo(0.49f).Within(1e-6f));

            var bitten = swarm.Advance(0.01f, Face);
            Assert.That(bitten.Bitten.Single(), Is.SameAs(mosquito));
        }

        [Test]
        public void 出現間隔0_1秒で顔の位置に2匹を同時に出し2_0秒進めると結果に2匹入る()
        {
            var rules = BiteRules(0f, maxCount: 3, spawnInterval: 0.1f);
            var swarm = CreateSwarm(rules);
            swarm.Start();

            var spawned = swarm.Advance(0.1f, Face).Spawned;
            Assert.That(spawned.Select(m => m.Id), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(spawned.All(m => m.Position == Face), Is.True);

            var result = swarm.Advance(2.0f, Face);

            Assert.That(result.Bitten.Count, Is.EqualTo(2));
            Assert.That(result.Bitten.Select(m => m.Id), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.Bitten.All(m => m.State == MosquitoState.Bitten), Is.True);
        }

        [Test]
        public void 刺した蚊への命中は失敗し以後の時間経過の結果に同じ蚊が入らない()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f));
            swarm.Advance(2.0f, Face);

            var hit = swarm.TryHit(mosquito.Id, out var downed);

            Assert.That(hit, Is.False);
            Assert.That(downed, Is.Null);
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Bitten));
            Assert.That(swarm.Advance(2.0f, Face).Bitten.Any(m => m.Id == mosquito.Id), Is.False);
        }

        // 回帰の確認: 実装前から通る（撃墜した蚊は一覧にいないので、もともと刺さない）
        [Test]
        public void 撃墜した蚊は顔の位置で何秒進めても刺さない()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f));
            swarm.TryHit(mosquito.Id, out var downed);

            var bitten = false;
            for (var i = 0; i < 5; i++)
                bitten |= swarm.Advance(2.0f, Face).Bitten.Any(m => m.Id == mosquito.Id);

            Assert.That(bitten, Is.False);
            Assert.That(downed.State, Is.EqualTo(MosquitoState.Downed));
            Assert.That(downed.BiteTime, Is.EqualTo(0f));
        }

        [Test]
        public void 刺して空いた枠は同じ時間経過の出現判定で埋まる()
        {
            var (swarm, mosquito) = SpawnAt(Face, BiteRules(0f, spawnInterval: 2.0f));

            var result = swarm.Advance(2.0f, Face);

            Assert.That(result.Bitten.Single(), Is.SameAs(mosquito));
            Assert.That(result.Spawned.Count, Is.EqualTo(1));
            Assert.That(result.Spawned[0].Id, Is.Not.EqualTo(mosquito.Id));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes[0], Is.SameAs(result.Spawned[0]));
        }

        [Test]
        public void 出現した時間経過では出現した蚊にBiteTimeが足されない()
        {
            var swarm = CreateSwarm(BiteRules(0f, spawnInterval: 2.0f));
            swarm.Start();

            var result = swarm.Advance(5.0f, Face);

            var spawned = result.Spawned.Single();
            Assert.That(spawned.BiteTime, Is.EqualTo(0f));
            Assert.That(spawned.State, Is.EqualTo(MosquitoState.Approaching));
            Assert.That(result.Bitten, Is.Empty);
        }
    }
}
