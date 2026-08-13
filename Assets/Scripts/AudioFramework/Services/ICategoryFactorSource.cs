using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// One independent gain factor, resolved per category. Every factor that feeds the single volume write is its
    /// own unit behind this contract — base volume today, ducking today, whatever a later feature adds — so the
    /// writer stays a pure combinator and never resolves anything itself.
    ///
    /// A source that is absent is not a special case: the writer substitutes 1.0, the neutral element of the
    /// multiplication. That is what makes a factor genuinely optional — leaving ducking out cannot break the
    /// write, it just drops a 1.0 into the product.
    /// </summary>
    public interface ICategoryFactorSource
    {
        float For(AudioCategory category);
    }
}
