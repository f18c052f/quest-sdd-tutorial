using System.Linq;
using System.Numerics;
using MosquitoSpray.Core.Mosquitoes;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Mosquitoes
{
    // 撃墜（命中を受けて一覧から外す）と、空いた枠の出現のテスト。刺さない設定値（biteDuration 1000 秒）を使う
    public sealed partial class MosquitoSwarmTests
    {
        const float Frame = 1f / 64f;

        // 上限 3 匹・間隔 2.0 秒で開始し 10 秒進めて、ちょうど 3 匹いる状態にする
        static MosquitoSwarm FullSwarm()
        {
            var swarm = CreateSwarm();
            swarm.Start();
            swarm.Advance(10f, Face);
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));
            return swarm;
        }

        [Test]
        public void 撃墜すると成功し撃墜状態で撃墜時の位置のまま一覧から外れる()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(1.5f));
            swarm.Advance(1.0f, Face);
            var positionAtHit = mosquito.Position;
            var countBefore = swarm.Mosquitoes.Count;

            var hit = swarm.TryHit(mosquito.Id, out var downed);

            Assert.That(hit, Is.True);
            Assert.That(downed, Is.SameAs(mosquito));
            Assert.That(downed.State, Is.EqualTo(MosquitoState.Downed));
            Assert.That(downed.Id, Is.EqualTo(mosquito.Id));
            Assert.That(downed.Position, Is.EqualTo(positionAtHit));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(countBefore - 1));
            Assert.That(swarm.Mosquitoes.Any(m => m.Id == mosquito.Id), Is.False);
        }

        [Test]
        public void 撃墜した蚊は顔の近くで何度進めても位置も状態も変わらない()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(0.2f));
            swarm.TryHit(mosquito.Id, out var downed);
            var positionAtHit = downed.Position;

            for (var i = 0; i < 10; i++)
                swarm.Advance(1.0f, Face);

            Assert.That(downed.Position, Is.EqualTo(positionAtHit));
            Assert.That(downed.State, Is.EqualTo(MosquitoState.Downed));
        }

        [Test]
        public void 複数いるときに指定した識別子だけが撃墜されほかは残り動き続ける()
        {
            var swarm = FullSwarm();
            var ids = swarm.Mosquitoes.Select(m => m.Id).ToArray();
            var target = ids[1];
            var others = swarm.Mosquitoes.Where(m => m.Id != target).ToArray();
            var before = others.Select(m => m.Position).ToArray();

            Assert.That(swarm.TryHit(target, out var downed), Is.True);
            Assert.That(downed.Id, Is.EqualTo(target));
            swarm.Advance(1.0f, Face);

            // 同じ時間経過で空いた枠に新しい蚊が 1 匹出るので、残った 2 匹は先頭 2 匹で確かめる
            Assert.That(swarm.Mosquitoes.Take(2).Select(m => m.Id), Is.EqualTo(new[] { ids[0], ids[2] }));
            Assert.That(swarm.Mosquitoes.Any(m => m.Id == target), Is.False);
            for (var i = 0; i < others.Length; i++)
            {
                Assert.That(others[i].State, Is.EqualTo(MosquitoState.Approaching));
                Assert.That(others[i].Position, Is.Not.EqualTo(before[i]));
            }
        }

        [Test]
        public void 同じ蚊への2回目の命中は失敗して空を返し一覧は変わらない()
        {
            var swarm = FullSwarm();
            var id = swarm.Mosquitoes[0].Id;
            swarm.TryHit(id, out _);
            var idsBefore = swarm.Mosquitoes.Select(m => m.Id).ToArray();

            var hit = swarm.TryHit(id, out var again);

            Assert.That(hit, Is.False);
            Assert.That(again, Is.Null);
            Assert.That(swarm.Mosquitoes.Select(m => m.Id), Is.EqualTo(idsBefore));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(9999)]
        public void 存在しない識別子への命中は失敗して空を返し一覧は変わらない(int id)
        {
            var swarm = FullSwarm();
            var idsBefore = swarm.Mosquitoes.Select(m => m.Id).ToArray();

            var hit = swarm.TryHit(id, out var downed);

            Assert.That(hit, Is.False);
            Assert.That(downed, Is.Null);
            Assert.That(swarm.Mosquitoes.Select(m => m.Id), Is.EqualTo(idsBefore));
            Assert.That(swarm.Mosquitoes.All(m => m.State == MosquitoState.Approaching), Is.True);
        }

        [Test]
        public void 停止で取り除かれた蚊への命中は失敗して空を返す()
        {
            var swarm = FullSwarm();
            var id = swarm.Mosquitoes[0].Id;
            swarm.Stop();

            var hit = swarm.TryHit(id, out var downed);

            Assert.That(hit, Is.False);
            Assert.That(downed, Is.Null);
            Assert.That(swarm.Mosquitoes, Is.Empty);
        }

        [Test]
        public void 開始していない群れへの命中は失敗して空を返す()
        {
            var swarm = CreateSwarm();

            var hit = swarm.TryHit(1, out var downed);

            Assert.That(hit, Is.False);
            Assert.That(downed, Is.Null);
            Assert.That(swarm.Mosquitoes, Is.Empty);
        }

        [Test]
        public void 撃墜した蚊は同時数に数えず次の時間経過で空いた枠が埋まる()
        {
            var swarm = FullSwarm();
            swarm.TryHit(swarm.Mosquitoes[0].Id, out _);
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(2));

            var result = swarm.Advance(Frame, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));
        }

        [Test]
        public void 上限で待った時間は貯まらず空いた枠は1匹ずつ出現間隔を待って埋まる()
        {
            var swarm = FullSwarm();

            swarm.TryHit(swarm.Mosquitoes[0].Id, out _);
            Assert.That(swarm.Advance(Frame, Face).Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));

            swarm.TryHit(swarm.Mosquitoes[0].Id, out _);
            swarm.TryHit(swarm.Mosquitoes[0].Id, out _);
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));

            var next = swarm.Advance(Frame, Face);

            Assert.That(next.Spawned, Is.Empty);
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));

            // 直前の出現から 2.0 秒たつと 1 匹ずつ出る
            Assert.That(swarm.Advance(2f - 2 * Frame, Face).Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(2));
            Assert.That(swarm.Advance(2f, Face).Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));
        }

        [Test]
        public void 識別子の連番は撃墜のあとも続き撃墜された蚊の識別子と重ならない()
        {
            var swarm = FullSwarm();
            var used = swarm.Mosquitoes.Select(m => m.Id).ToList();
            swarm.TryHit(used[0], out _);
            swarm.TryHit(used[2], out _);

            var spawned = swarm.Advance(2f, Face).Spawned;

            Assert.That(spawned, Is.Not.Empty);
            foreach (var m in spawned)
                Assert.That(used.Contains(m.Id), Is.False);
            Assert.That(spawned.Select(m => m.Id).Distinct().Count(), Is.EqualTo(spawned.Count));
        }
    }
}
