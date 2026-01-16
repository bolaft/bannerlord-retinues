using System;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Game.Recruitement.Models
{
    /// <summary>
    /// Wraps the currently active VolunteerModel to inject base (settings-agnostic) custom roots upstream.
    /// Base rule (per settlement):
    /// - If owner clan has custom troops: use clan roots
    /// - Else if owner kingdom has custom troops: use kingdom roots
    /// - Else: do not interfere (delegate to inner model, for vanilla or other recruitement mods)
    /// </summary>
    internal sealed class CustomVolunteerModel(VolunteerModel inner) : VolunteerModel
    {
        private readonly VolunteerModel _inner =
            inner ?? throw new ArgumentNullException(nameof(inner));

        public override int MaxVolunteerTier => _inner.MaxVolunteerTier;

        public override int MaximumIndexHeroCanRecruitFromHero(
            Hero buyerHero,
            Hero sellerHero,
            int useValueAsRelation = -101
        ) => _inner.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);

#if BL13
        public override int MaximumIndexGarrisonCanRecruitFromHero(
            Settlement settlement,
            Hero sellerHero
        ) => _inner.MaximumIndexGarrisonCanRecruitFromHero(settlement, sellerHero);
#endif

        public override float GetDailyVolunteerProductionProbability(
            Hero hero,
            int index,
            Settlement settlement
        ) => _inner.GetDailyVolunteerProductionProbability(hero, index, settlement);

        public override bool CanHaveRecruits(Hero hero) => _inner.CanHaveRecruits(hero);

        public override CharacterObject GetBasicVolunteer(Hero sellerHero)
        {
            // Always let the active model/mod compute its default first.
            var inner = _inner.GetBasicVolunteer(sellerHero);

            try
            {
                if (sellerHero == null || !sellerHero.IsAlive)
                    return inner;

                var settlement = sellerHero.CurrentSettlement;
                if (settlement == null)
                    return inner;

                var ws = WSettlement.Get(settlement);
                if (ws == null)
                    return inner;

                // Only override if this settlement has base custom troops (clan > kingdom).
                // If not, we must not interfere with vanilla or other recruitement mods.
                if (ws.GetBaseTroopsFaction() == null)
                    return inner;

                // Decide whether we want elite-root pool or basic-root pool.
                // Use the inner result to infer elite vs basic when possible.
                bool wantElite = false;

                if (inner != null)
                {
                    var wc = WCharacter.Get(inner);
                    if (wc != null)
                        wantElite = wc.IsElite;
                }
                else
                {
                    // Vanilla special case: rural notable in a castle-bound village uses an elite-ish basic troop.
                    if (
                        sellerHero.IsRuralNotable
                        && settlement.IsVillage
                        && settlement.Village?.Bound?.IsCastle == true
                    )
                        wantElite = true;
                }

                var root = wantElite ? ws.GetBaseEliteRoot() : ws.GetBaseBasicRoot();
                return root?.Base ?? inner;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: RetinuesVolunteerModel.GetBasicVolunteer failed.");
                return inner;
            }
        }
    }
}
