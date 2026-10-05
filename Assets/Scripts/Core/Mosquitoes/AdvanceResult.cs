using System.Collections.Generic;

namespace MosquitoSpray.Core.Mosquitoes
{
    /// <summary>1回の時間経過で起きたこと。新たに出現した蚊と、新たに刺した蚊を渡す。</summary>
    public sealed class AdvanceResult
    {
        internal AdvanceResult(IReadOnlyList<Mosquito> spawned, IReadOnlyList<Mosquito> bitten)
        {
            Spawned = spawned;
            Bitten = bitten;
        }

        /// <summary>この時間経過で出現した蚊。出現した順。</summary>
        public IReadOnlyList<Mosquito> Spawned { get; }

        /// <summary>この時間経過で刺した蚊。State は Bitten で、Position は刺した位置。接近中の一覧にいた順で、刺した蚊は同じ時間経過のうちに一覧から外れる。</summary>
        public IReadOnlyList<Mosquito> Bitten { get; }
    }
}
