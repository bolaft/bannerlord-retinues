using System;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Behaviors.Recruitement.Models
{
    /// <summary>
    /// Wraps the active VolunteerModel to prefer base custom troop roots per settlement.
    /// </summary>
    internal sealed class CustomVolunteerModel(VolunteerModel inner) : VolunteerModel
    {
        private readonly VolunteerModel _inner =
            inner ?? throw new ArgumentNullException(nameof(inner));

        /// <summary>
        /// Gets the maximum volunteer tier delegated to the inner model.
        /// </summary>
        public override int MaxVolunteerTier => _inner.MaxVolunteerTier;

        /// <summary>
        /// Delegates maximum recruit index resolution to the inner model.
        /// </summary>
        public override int MaximumIndexHeroCanRecruitFromHero(
            Hero buyerHero,
            Hero sellerHero,
            int useValueAsRelation = -101
        ) => _inner.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);

#if BL13
        /// <summary>
        /// Delegates maximum index the garrison can recruit from a hero to the inner model.
        /// </summary>
        public override int MaximumIndexGarrisonCanRecruitFromHero(
            Settlement settlement,
            Hero sellerHero
        ) => _inner.MaximumIndexGarrisonCanRecruitFromHero(settlement, sellerHero);
#endif

        /// <summary>
        /// Delegates daily volunteer production probability calculation to the inner model.
        /// </summary>
        public override float GetDailyVolunteerProductionProbability(
            Hero hero,
            int index,
            Settlement settlement
        ) => _inner.GetDailyVolunteerProductionProbability(hero, index, settlement);

        /// <summary>
        /// Delegates whether the hero can have recruits to the inner model.
        /// </summary>
        public override bool CanHaveRecruits(Hero hero) => _inner.CanHaveRecruits(hero);

        /// <summary>
        /// Returns a basic volunteer, preferring clan/kingdom custom roots when available.
        /// </summary>
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
                var rootBase = root?.Base;
                if (rootBase == null)
                    return inner;

                if (Settings.SameCultureOnly)
                {
                    var settlementCulture = settlement.Culture;
                    var rootCulture = rootBase.Culture;

                    if (
                        settlementCulture != null
                        && rootCulture != null
                        && rootCulture != settlementCulture
                    )
                        return inner;
                }

                return rootBase;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: RetinuesVolunteerModel.GetBasicVolunteer failed.");
                return inner;
            }
        }

        /// <summary>
        /// Try to add the recruitment VolunteerModel wrapper.
        /// </summary>
        public static void TryAdd(CampaignGameStarter cs)
        {
            try
            {
                // IMPORTANT:
                // At OnGameStart time Campaign.Current.Models is not created yet.
                // We must read from the gameStarter's model list instead.
                var inner = cs.Models.OfType<VolunteerModel>().LastOrDefault();
                if (inner == null)
                {
                    Log.Warning(
                        "Recruitement: no VolunteerModel found in CampaignGameStarter.Models; wrapper not installed."
                    );
                    return;
                }

                if (inner is CustomVolunteerModel)
                {
                    Log.Info("Recruitement: VolunteerModel wrapper already installed.");
                    return;
                }

                cs.AddModel(new CustomVolunteerModel(inner));

                Log.Info(
                    $"Recruitement: VolunteerModel wrapper installed (inner={inner.GetType().Name})."
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: failed to install VolunteerModel wrapper.");
            }
        }
    }
}
