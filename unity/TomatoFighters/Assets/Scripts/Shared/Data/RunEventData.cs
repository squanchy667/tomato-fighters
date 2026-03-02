using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Data
{
    /// <summary>Data for when an area (wave set) is cleared.</summary>
    public readonly struct AreaClearedData
    {
        public readonly int areaIndex;
        public readonly int enemiesDefeated;

        public AreaClearedData(int areaIndex, int enemiesDefeated)
        {
            this.areaIndex = areaIndex;
            this.enemiesDefeated = enemiesDefeated;
        }
    }

    /// <summary>Data for when a boss is defeated.</summary>
    public readonly struct BossDefeatedData
    {
        public readonly string bossId;
        public readonly int islandIndex;

        public BossDefeatedData(string bossId, int islandIndex)
        {
            this.bossId = bossId;
            this.islandIndex = islandIndex;
        }
    }

    /// <summary>Data for when an island (set of areas + boss) is completed.</summary>
    public readonly struct IslandCompletedData
    {
        public readonly int islandIndex;

        public IslandCompletedData(int islandIndex)
        {
            this.islandIndex = islandIndex;
        }
    }

    /// <summary>Data for when the player enters a shop between areas.</summary>
    public readonly struct ShopEnteredData
    {
        public readonly int islandIndex;
        public readonly bool isSpecialShop;

        public ShopEnteredData(int islandIndex, bool isSpecialShop)
        {
            this.islandIndex = islandIndex;
            this.isSpecialShop = isSpecialShop;
        }
    }

    /// <summary>Data for when a run ends (victory or defeat).</summary>
    public readonly struct RunEndData
    {
        public readonly bool wasVictory;
        public readonly int crystalsEarned;
        public readonly int finalIslandIndex;

        public RunEndData(bool wasVictory, int crystalsEarned, int finalIslandIndex)
        {
            this.wasVictory = wasVictory;
            this.crystalsEarned = crystalsEarned;
            this.finalIslandIndex = finalIslandIndex;
        }
    }

    /// <summary>Data for when a player selects a main or secondary path.</summary>
    public readonly struct PathSelectedData
    {
        public readonly CharacterType character;
        public readonly PathType pathType;
        public readonly bool isMainPath;

        public PathSelectedData(CharacterType character, PathType pathType, bool isMainPath)
        {
            this.character = character;
            this.pathType = pathType;
            this.isMainPath = isMainPath;
        }
    }

    /// <summary>Data for when a path tier is upgraded.</summary>
    public readonly struct PathTierUpData
    {
        public readonly CharacterType character;
        public readonly PathType pathType;
        public readonly int newTier;

        public PathTierUpData(CharacterType character, PathType pathType, int newTier)
        {
            this.character = character;
            this.pathType = pathType;
            this.newTier = newTier;
        }
    }
}
