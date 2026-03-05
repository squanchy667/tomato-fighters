using System.Collections.Generic;
using TomatoFighters.Characters.Abilities.Arcanist;
using TomatoFighters.Characters.Abilities.Bulwark;
using TomatoFighters.Characters.Abilities.Conjurer;
using TomatoFighters.Characters.Abilities.Enchanter;
using TomatoFighters.Characters.Abilities.Executioner;
using TomatoFighters.Characters.Abilities.Guardian;
using TomatoFighters.Characters.Abilities.Marksman;
using TomatoFighters.Characters.Abilities.Reaper;
using TomatoFighters.Characters.Abilities.Sage;
using TomatoFighters.Characters.Abilities.Shadow;
using TomatoFighters.Characters.Abilities.Trapper;
using TomatoFighters.Characters.Abilities.Warden;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities
{
    /// <summary>
    /// Creates path ability instances by ability ID string.
    /// Maps PathData.tierXAbilityId → concrete IPathAbility implementation.
    /// </summary>
    public static class AbilityFactory
    {
        private delegate IPathAbility AbilityCreator(PathAbilityContext ctx);

        private static readonly Dictionary<string, AbilityCreator> _creators = new()
        {
            // Brutor
            ["Warden_Provoke"] = ctx => new Provoke(ctx),
            ["Bulwark_IronGuard"] = ctx => new IronGuard(ctx),
            ["Guardian_ShieldLink"] = ctx => new ShieldLink(ctx),

            // Slasher
            ["Executioner_MarkForDeath"] = ctx => new MarkForDeath(ctx),
            ["Reaper_CleavingStrikes"] = ctx => new CleavingStrikes(ctx),
            ["Shadow_PhaseDash"] = ctx => new PhaseDash(ctx),

            // Mystica
            ["Sage_MendingAura"] = ctx => new MendingAura(ctx),
            ["Enchanter_Empower"] = ctx => new Empower(ctx),
            ["Conjurer_SummonSproutling"] = ctx => new SummonSproutling(ctx),

            // Viper
            ["Marksman_PiercingShots"] = ctx => new PiercingShots(ctx),
            ["Trapper_HarpoonShot"] = ctx => new HarpoonShot(ctx),
            ["Arcanist_ManaCharge"] = ctx => new ManaCharge(ctx),
        };

        /// <summary>
        /// Create a path ability by its ID. Returns null if the ID is not recognized.
        /// </summary>
        public static IPathAbility Create(string abilityId, PathAbilityContext context)
        {
            if (string.IsNullOrEmpty(abilityId))
            {
                Debug.LogWarning("[AbilityFactory] Null or empty ability ID.");
                return null;
            }

            if (_creators.TryGetValue(abilityId, out var creator))
            {
                return creator(context);
            }

            Debug.LogWarning($"[AbilityFactory] Unknown ability ID: '{abilityId}'");
            return null;
        }
    }
}
