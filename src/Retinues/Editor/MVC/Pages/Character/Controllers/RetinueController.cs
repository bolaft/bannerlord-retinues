using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using TaleWorlds.CampaignSystem;

namespace Retinues.Editor.MVC.Pages.Character.Controllers
{
    /// <summary>
    /// Controller for managing retinue-specific actions like ranking up.
    /// </summary>
    public class RetinueController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rank Up                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ranks up the selected unit after confirming costs and requirements.
        /// </summary>
        public static ControllerAction<WCharacter> RankUp { get; } =
            Action<WCharacter>("RankUp")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => State.Mode == EditorMode.Player,
                    L.T("rank_up_player_only", "Not available in the Universal Editor")
                )
                .AddCondition(
                    s => State.Character.IsCaptain != true,
                    L.T("rank_up_no_captains", "Captains cannot rank up")
                )
                .AddCondition(
                    c => c != null && c.IsRetinue,
                    L.T("rank_up_retinue_only", "Only available for retinues")
                )
                .AddCondition(
                    c => c != null && !c.IsMaxTier,
                    L.T("rank_up_max_tier", "Already at max tier")
                )
                .AddCondition(
                    c => c != null && c.SkillTotalUsed >= c.SkillTotal,
                    L.T(
                        "rank_up_requires_maxed_skills",
                        "Skills must be maxed out first"
                    )
                )
                .AddCondition(
                    c => c != null && HasEnoughRankUpSkillPoints(c),
                    () =>
                        L.T("rank_up_not_enough_sp", "Requires {POINTS} skill points")
                            .SetTextVariable("POINTS", GetRankUpSkillPointCost(State.Character))
                )
                .AddCondition(
                    c => c != null && HasEnoughGoldForRankUp(c),
                    () =>
                        L.T("rank_up_not_enough_money", "Requires {COST} denars")
                            .SetTextVariable("COST", GetRankUpGoldCost(State.Character))
                )
                .ExecuteWith(RankUpImpl)
                .Fire(UIEvent.Character);

        /// <summary>
        /// Returns the gold cost required to rank up the given character.
        /// </summary>
        private static int GetRankUpGoldCost(WCharacter c)
        {
            if (c == null)
                return 0;

            return (c.Tier + 1) * 1000;
        }

        /// <summary>
        /// Skill point cost: 5 per tier above 1 for the target tier.
        /// If we are tier N and we rank up to N+1, the cost is 5 * N.
        /// Examples:
        /// - tier 2 -> 3 costs 10
        /// - tier 3 -> 4 costs 15
        /// </summary>
        private static int GetRankUpSkillPointCost(WCharacter c)
        {
            if (c == null)
                return 0;

            // targetTier = c.Tier + 1 -> cost = 5 * (targetTier - 1) = 5 * c.Tier
            return 5 * c.Tier;
        }

        /// <summary>
        /// Returns true if the character has enough skill points to pay the rank-up cost.
        /// </summary>
        private static bool HasEnoughRankUpSkillPoints(WCharacter c)
        {
            if (c == null)
                return false;

            var cost = GetRankUpSkillPointCost(c);
            return c.SkillPoints >= cost;
        }

        /// <summary>
        /// Returns true if the main hero has enough gold to pay the rank-up cost for the character.
        /// </summary>
        private static bool HasEnoughGoldForRankUp(WCharacter c)
        {
            if (c == null)
                return false;

            var hero = Hero.MainHero;
            if (hero == null)
                return false;

            var cost = GetRankUpGoldCost(c);
            return hero.Gold >= cost;
        }

        /// <summary>
        /// Prompt for confirmation and perform the rank-up: deduct costs, increase level, and fire events.
        /// </summary>
        private static void RankUpImpl(WCharacter c)
        {
            if (c == null)
                return;

            var hero = Hero.MainHero;
            if (hero == null)
                return;

            var goldCost = GetRankUpGoldCost(c);
            var spCost = GetRankUpSkillPointCost(c);

            var body = L.T(
                    "rank_up_confirm_body",
                    "Rank up {NAME}?\n\nCosts:\n- {SP_COST} skill points\n- {GOLD_COST} gold\n\nEffect:\n- +5 levels (tier up)"
                )
                .SetTextVariable("NAME", c.Name)
                .SetTextVariable("SP_COST", spCost)
                .SetTextVariable("GOLD_COST", goldCost);

            Inquiries.Popup(
                title: L.T("rank_up_confirm_title", "Confirm Rank Up"),
                description: body,
                confirmText: L.T("rank_up_confirm_yes", "Rank Up"),
                cancelText: L.T("rank_up_confirm_no", "Cancel"),
                onConfirm: () =>
                {
                    if (!HasEnoughGoldForRankUp(c) || !HasEnoughRankUpSkillPoints(c))
                        return;

                    hero.ChangeHeroGold(-goldCost);
                    c.SkillPoints -= spCost;

                    c.Level += 5;

                    EventManager.Fire(UIEvent.Character);
                    EventManager.Fire(UIEvent.Skill);
                }
            );
        }
    }
}
