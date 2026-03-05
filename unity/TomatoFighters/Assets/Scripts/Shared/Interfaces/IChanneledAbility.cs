namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Extension of <see cref="IPathAbility"/> for channeled abilities that respond to input release.
    /// </summary>
    public interface IChanneledAbility : IPathAbility
    {
        /// <summary>Called when the ability input key is released.</summary>
        void Release();
    }
}
