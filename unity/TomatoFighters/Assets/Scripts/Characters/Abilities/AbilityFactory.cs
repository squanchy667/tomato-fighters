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
    /// Covers T1 (12), T2 (12), and T3 (12) abilities — 36 total.
    /// </summary>
    public static class AbilityFactory
    {
        private delegate IPathAbility AbilityCreator(PathAbilityContext ctx);

        private static readonly Dictionary<string, AbilityCreator> _creators = new()
        {
            // ── Brutor ──────────────────────────────────────────────
            // Warden
            ["Warden_Provoke"]            = ctx => new Provoke(ctx),
            ["Warden_AggroAura"]          = ctx => new AggroAura(ctx),
            ["Warden_WrathOfTheWarden"]   = ctx => new WrathOfTheWarden(ctx),
            // Bulwark
            ["Bulwark_IronGuard"]         = ctx => new IronGuard(ctx),
            ["Bulwark_Retaliation"]       = ctx => new Retaliation(ctx),
            ["Bulwark_Fortress"]          = ctx => new Fortress(ctx),
            // Guardian
            ["Guardian_ShieldLink"]       = ctx => new ShieldLink(ctx),
            ["Guardian_RallyingPresence"] = ctx => new RallyingPresence(ctx),
            ["Guardian_AegisDome"]        = ctx => new AegisDome(ctx),

            // ── Slasher ─────────────────────────────────────────────
            // Executioner
            ["Executioner_MarkForDeath"]       = ctx => new MarkForDeath(ctx),
            ["Executioner_ExecutionThreshold"] = ctx => new ExecutionThreshold(ctx),
            ["Executioner_Deathblow"]          = ctx => new Deathblow(ctx),
            // Reaper
            ["Reaper_CleavingStrikes"] = ctx => new CleavingStrikes(ctx),
            ["Reaper_ChainSlash"]      = ctx => new ChainSlash(ctx),
            ["Reaper_Whirlwind"]       = ctx => new Whirlwind(ctx),
            // Shadow
            ["Shadow_PhaseDash"]      = ctx => new PhaseDash(ctx),
            ["Shadow_Afterimage"]     = ctx => new Afterimage(ctx),
            ["Shadow_ThousandCuts"]   = ctx => new ThousandCuts(ctx),

            // ── Mystica ─────────────────────────────────────────────
            // Sage
            ["Sage_MendingAura"]    = ctx => new MendingAura(ctx),
            ["Sage_PurifyingPresence"] = ctx => new PurifyingBurst(ctx),
            ["Sage_Resurrection"]   = ctx => new Resurrection(ctx),
            // Enchanter
            ["Enchanter_Empower"]           = ctx => new Empower(ctx),
            ["Enchanter_ElementalInfusion"] = ctx => new ElementalInfusion(ctx),
            ["Enchanter_ArcaneOverdrive"]   = ctx => new ArcaneOverdrive(ctx),
            // Conjurer
            ["Conjurer_SummonSproutling"] = ctx => new SummonSproutling(ctx),
            ["Conjurer_TotemPulse"]       = ctx => new DeployTotem(ctx),
            ["Conjurer_SummonGolem"]      = ctx => new SummonGolem(ctx),

            // ── Viper ───────────────────────────────────────────────
            // Marksman
            ["Marksman_PiercingShots"] = ctx => new PiercingShots(ctx),
            ["Marksman_RapidVolleys"]  = ctx => new RapidFire(ctx),
            ["Marksman_Killshot"]      = ctx => new Killshot(ctx),
            // Trapper
            ["Trapper_HarpoonShot"] = ctx => new HarpoonShot(ctx),
            ["Trapper_TrapDeployment"] = ctx => new TrapNet(ctx),
            ["Trapper_AnchorChain"] = ctx => new AnchorChain(ctx),
            // Arcanist
            ["Arcanist_ManaCharge"]    = ctx => new ManaCharge(ctx),
            ["Arcanist_ManaBlast"]     = ctx => new ManaBlast(ctx),
            ["Arcanist_ManaOverload"]  = ctx => new ManaOverload(ctx),
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
