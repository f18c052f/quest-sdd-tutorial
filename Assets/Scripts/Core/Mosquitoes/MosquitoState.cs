namespace MosquitoSpray.Core.Mosquitoes
{
    /// <summary>蚊1匹の状態。</summary>
    public enum MosquitoState
    {
        /// <summary>顔へ接近中。</summary>
        Approaching,

        /// <summary>撃墜された。以後は動かない。</summary>
        Downed,

        /// <summary>顔を刺した。以後は動かない。</summary>
        Bitten
    }
}
