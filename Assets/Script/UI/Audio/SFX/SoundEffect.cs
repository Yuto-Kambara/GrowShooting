// Assets/Scripts/Audio/SoundEffect.cs
namespace GrowShooting.Audio
{
    /// <summary>効果音の種類。追加するときはここに列挙子を足すだけ。</summary>
    public enum SoundEffect
    {
        PlayerShot,
        EnemyDown,
        PlayerHit,
        // --- ここより下は将来拡張用 -----------------
        // Example: LevelUp,
        // Example: ItemPickup,
    }
}
