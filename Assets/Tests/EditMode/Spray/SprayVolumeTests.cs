using System;
using System.Numerics;
using MosquitoSpray.Core.Spray;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Spray
{
    public class SprayVolumeTests
    {
        static readonly Vector3 Forward = new Vector3(0f, 0f, 1f);

        [Test]
        public void 正しい値で作ると渡した起点と半角と到達距離を読み出せる()
        {
            var origin = new Vector3(1f, 1.5f, -2f);

            var volume = new SprayVolume(origin, Forward, 15f, 1.0f);

            Assert.That(volume.Origin, Is.EqualTo(origin));
            Assert.That(volume.HalfAngle, Is.EqualTo(15f));
            Assert.That(volume.Reach, Is.EqualTo(1.0f));
        }

        [Test]
        public void 向きの長さは無視され単位ベクトルとして読み出せる()
        {
            var volume = new SprayVolume(Vector3.Zero, new Vector3(0f, 0f, 5f), 15f, 1.0f);

            Assert.That(volume.Direction, Is.EqualTo(Forward));
        }

        [Test]
        public void 斜めの向きも正規化した単位ベクトルとして読み出せる()
        {
            var volume = new SprayVolume(Vector3.Zero, new Vector3(3f, 0f, 4f), 15f, 1.0f);

            Assert.That(volume.Direction.X, Is.EqualTo(0.6f).Within(1e-6f));
            Assert.That(volume.Direction.Y, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(volume.Direction.Z, Is.EqualTo(0.8f).Within(1e-6f));
        }

        [TestCase(0.1f, 0.01f)]
        [TestCase(45f, 3f)]
        [TestCase(89.9f, 1.0f)]
        [TestCase(15f, 0.01f)]
        [TestCase(15f, 3f)]
        public void 範囲内の任意の半角と到達距離で作れる(float halfAngle, float reach)
        {
            var volume = new SprayVolume(new Vector3(1f, 1.5f, -2f), Forward, halfAngle, reach);

            Assert.That(volume.HalfAngle, Is.EqualTo(halfAngle));
            Assert.That(volume.Reach, Is.EqualTo(reach));
        }

        [Test]
        public void 向きの長さが0なら向きが不正だと知らせる()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new SprayVolume(Vector3.Zero, new Vector3(0f, 0f, 0f), 15f, 1.0f));

            Assert.That(ex.ParamName, Is.EqualTo("direction"));
        }

        [Test]
        public void 向きにNaNを含むなら向きが不正だと知らせる()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new SprayVolume(Vector3.Zero, new Vector3(float.NaN, 0f, 1f), 15f, 1.0f));

            Assert.That(ex.ParamName, Is.EqualTo("direction"));
        }

        [TestCase(0f)]
        [TestCase(-10f)]
        [TestCase(90f)]
        [TestCase(120f)]
        [TestCase(float.NaN)]
        public void 半角が0より大きく90より小さくなければ半角が不正だと知らせる(float halfAngle)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new SprayVolume(Vector3.Zero, Forward, halfAngle, 1.0f));

            Assert.That(ex.ParamName, Is.EqualTo("halfAngle"));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void 到達距離が0より大きくなければ到達距離が不正だと知らせる(float reach)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new SprayVolume(Vector3.Zero, Forward, 15f, reach));

            Assert.That(ex.ParamName, Is.EqualTo("reach"));
        }

        // 命中の判定は仮の初期値（半角 15°・到達距離 1.0m）で行う。
        // 起点は原点と (1, 1.5, -2) の両方で試し、同じ結果になることを確かめる。

        [TestCase(0f, 0f, 0f, 0.0, 0.5, TestName = "正面0.5mは命中_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 0.0, 0.5, TestName = "正面0.5mは命中_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 10.0, 0.5, TestName = "10度0.5mは命中_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 10.0, 0.5, TestName = "10度0.5mは命中_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 15.0, 0.5, TestName = "ちょうど15度0.5mは命中_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 15.0, 0.5, TestName = "ちょうど15度0.5mは命中_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 0.0, 1.0, TestName = "正面ちょうど1.0mは命中_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 0.0, 1.0, TestName = "正面ちょうど1.0mは命中_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 15.0, 1.0, TestName = "ちょうど15度ちょうど1.0mは命中_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 15.0, 1.0, TestName = "ちょうど15度ちょうど1.0mは命中_起点が原点以外")]
        public void 到達距離以内かつ半角以内の位置は命中する(
            float originX, float originY, float originZ, double angle, double distance)
        {
            var origin = new Vector3(originX, originY, originZ);
            var volume = new SprayVolume(origin, Forward, 15f, 1.0f);

            Assert.That(volume.Contains(origin + Offset(angle, distance)), Is.True);
        }

        [TestCase(0f, 0f, 0f, 0.0, 1.001, TestName = "正面1.001mは外れ_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 0.0, 1.001, TestName = "正面1.001mは外れ_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 15.01, 0.5, TestName = "15.01度0.5mは外れ_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 15.01, 0.5, TestName = "15.01度0.5mは外れ_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 90.0, 0.5, TestName = "真横0.5mは外れ_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 90.0, 0.5, TestName = "真横0.5mは外れ_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 180.0, 0.5, TestName = "真後ろ0.5mは外れ_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 180.0, 0.5, TestName = "真後ろ0.5mは外れ_起点が原点以外")]
        [TestCase(0f, 0f, 0f, 180.0, 0.01, TestName = "真後ろ0.01mは外れ_起点が原点")]
        [TestCase(1f, 1.5f, -2f, 180.0, 0.01, TestName = "真後ろ0.01mは外れ_起点が原点以外")]
        public void 到達距離より遠いか半角より外の位置は外れる(
            float originX, float originY, float originZ, double angle, double distance)
        {
            var origin = new Vector3(originX, originY, originZ);
            var volume = new SprayVolume(origin, Forward, 15f, 1.0f);

            Assert.That(volume.Contains(origin + Offset(angle, distance)), Is.False);
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(1f, 1.5f, -2f)]
        public void 起点そのものは命中する(float originX, float originY, float originZ)
        {
            var origin = new Vector3(originX, originY, originZ);
            var volume = new SprayVolume(origin, Forward, 15f, 1.0f);

            Assert.That(volume.Contains(origin), Is.True);
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(1f, 1.5f, -2f)]
        public void 向きの長さが違っても同じ位置で同じ判定になる(float originX, float originY, float originZ)
        {
            var origin = new Vector3(originX, originY, originZ);
            var longDirection = new SprayVolume(origin, new Vector3(0f, 0f, 5f), 15f, 1.0f);
            var unitDirection = new SprayVolume(origin, Forward, 15f, 1.0f);
            var offsets = new[]
            {
                Offset(0.0, 0.5), Offset(10.0, 0.5), Offset(15.0, 0.5), Offset(15.0, 1.0),
                Offset(0.0, 1.001), Offset(15.01, 0.5), Offset(90.0, 0.5), Offset(180.0, 0.5),
                Vector3.Zero,
            };

            foreach (var offset in offsets)
            {
                var position = origin + offset;
                Assert.That(longDirection.Contains(position), Is.EqualTo(unitDirection.Contains(position)),
                    $"位置 {position}");
            }
        }

        [TestCase(0f, 0f, 0f, 10.0, 0.5)]
        [TestCase(1f, 1.5f, -2f, 10.0, 0.5)]
        [TestCase(0f, 0f, 0f, 180.0, 0.5)]
        [TestCase(1f, 1.5f, -2f, 180.0, 0.5)]
        public void 同じ位置で2回判定しても結果が同じで値が変わらない(
            float originX, float originY, float originZ, double angle, double distance)
        {
            var origin = new Vector3(originX, originY, originZ);
            var volume = new SprayVolume(origin, Forward, 15f, 1.0f);
            var position = origin + Offset(angle, distance);

            var first = volume.Contains(position);
            var second = volume.Contains(position);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(volume.Origin, Is.EqualTo(origin));
            Assert.That(volume.Direction, Is.EqualTo(Forward));
            Assert.That(volume.HalfAngle, Is.EqualTo(15f));
            Assert.That(volume.Reach, Is.EqualTo(1.0f));
        }

        // 仮の初期値（15°・1.0m）以外の組でも、同じ規則で命中と外れが決まることを確かめる。

        [TestCase(30f, 2.0f, 25.0, 1.9, TestName = "半角30度到達2.0mで25度1.9mは命中")]
        [TestCase(5f, 0.3f, 0.0, 0.29, TestName = "半角5度到達0.3mで正面0.29mは命中")]
        public void 仮の初期値以外の組でも到達距離以内かつ半角以内の位置は命中する(
            float halfAngle, float reach, double angle, double distance)
        {
            var origin = new Vector3(1f, 1.5f, -2f);
            var volume = new SprayVolume(origin, Forward, halfAngle, reach);

            Assert.That(volume.Contains(origin + Offset(angle, distance)), Is.True);
        }

        [TestCase(30f, 2.0f, 35.0, 1.0, TestName = "半角30度到達2.0mで35度1.0mは外れ")]
        [TestCase(30f, 2.0f, 0.0, 2.1, TestName = "半角30度到達2.0mで正面2.1mは外れ")]
        [TestCase(5f, 0.3f, 6.0, 0.2, TestName = "半角5度到達0.3mで6度0.2mは外れ")]
        public void 仮の初期値以外の組でも到達距離より遠いか半角より外の位置は外れる(
            float halfAngle, float reach, double angle, double distance)
        {
            var origin = new Vector3(1f, 1.5f, -2f);
            var volume = new SprayVolume(origin, Forward, halfAngle, reach);

            Assert.That(volume.Contains(origin + Offset(angle, distance)), Is.False);
        }

        /// <summary>中心軸 (0,0,1) を含む XZ 平面上で、軸から angle 度・起点から distance m 離れた位置への差分。</summary>
        static Vector3 Offset(double angle, double distance)
        {
            var radians = angle * Math.PI / 180.0;
            return new Vector3((float)(Math.Sin(radians) * distance), 0f, (float)(Math.Cos(radians) * distance));
        }
    }
}
