using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Experience;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Utils;

namespace Retinues.Managers
{
    /// <summary>
    /// Retinue-related limits and costs.
    /// </summary>
    [SafeClass]
    public static class RetinueManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Caps                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Max elite retinue units allowed in party.
        /// </summary>
        public static int EliteRetinueCap => (int)(RetinueCapBase * Config.MaxEliteRetinueRatio);

        /// <summary>
        /// Max basic retinue units allowed in party.
        /// </summary>
        public static int BasicRetinueCap => (int)(RetinueCapBase * Config.MaxBasicRetinueRatio);

        /// <summary>
        /// Base retinue cap before applying configured ratios.
        /// </summary>
        private static int RetinueCapBase
        {
            get
            {
                var max = Player.Party?.PartySizeLimit ?? 0;
                if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                    max = (int)(max * 1.15f);
                return max;
            }
        }

        /// <summary>
        /// Retinue cap for a troop (elite or basic).
        /// </summary>
        public static int RetinueCapFor(WCharacter retinue)
        {
            return retinue.IsElite ? EliteRetinueCap : BasicRetinueCap;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Conversion                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const float InfluenceCostRatio = 0.05f;
        private const float RenownCostRatio = 0.2f;

        /// <summary>
        /// Gold cost per unit for converting to this retinue.
        /// </summary>
        public static int ConversionGoldCostPerUnit(WCharacter retinue)
        {
            if (retinue == null || !retinue.IsRetinue)
                return 0;
            int tier = retinue.Tier <= 0 ? 1 : retinue.Tier;
            return tier * Config.RetinueConversionCostPerTier;
        }

        /// <summary>
        /// Influence cost per unit for converting to this retinue.
        /// </summary>
        public static int ConversionInfluenceCostPerUnit(WCharacter retinue)
        {
            return (int)(ConversionGoldCostPerUnit(retinue) * InfluenceCostRatio);
        }

        /// <summary>
        /// Renown cost per unit for converting to this retinue.
        /// </summary>
        public static int ConversionRenownCostPerUnit(WCharacter retinue)
        {
            return (int)(ConversionGoldCostPerUnit(retinue) * RenownCostRatio);
        }

        /// <summary>
        /// Maximum troops convertible from one type to another, considering retinue caps.
        /// </summary>
        public static int GetMaxConvertible(WCharacter from, WCharacter to)
        {
            int maxConvertible = Player.Party.MemberRoster.CountOf(from);

            if (to.IsRetinue)
            {
                int currentTo = Player.Party.MemberRoster.CountOf(to);
                int cap = RetinueCapFor(to);
                maxConvertible = Math.Min(maxConvertible, Math.Max(0, cap - currentTo));
            }

            return maxConvertible;
        }

        /// <summary>
        /// Convert units and mutate the roster, spending currency as needed.
        /// </summary>
        public static void Convert(WCharacter from, WCharacter to, int amountRequested)
        {
            int max = GetMaxConvertible(from, to);
            int amount = Math.Min(amountRequested, max);
            if (amount <= 0)
                return;

            int goldCost = to.IsRetinue ? ConversionGoldCostPerUnit(to) * amount : 0;
            int influenceCost = to.IsRetinue ? ConversionInfluenceCostPerUnit(to) * amount : 0;

            if (Player.Gold < goldCost)
                return;
            if (Player.Influence < influenceCost)
                return;

            if (goldCost > 0)
                Player.ChangeGold(-goldCost);
            if (influenceCost > 0)
                Player.ChangeInfluence(-influenceCost);

            Player.Party.MemberRoster.RemoveTroop(from, amount);
            Player.Party.MemberRoster.AddTroop(to, amount);
        }

        /// <summary>
        /// Suggest best source troops for creating a retinue.
        /// </summary>
        public static List<WCharacter> GetRetinueSourceTroops(WCharacter retinue)
        {
            var sources = new List<WCharacter>(2);
            if (retinue == null || !retinue.IsRetinue)
                return sources;

            // Helper to add a source troop from a faction or culture.
            void AddSourceIfValid(BaseFaction f)
            {
                if (f == null)
                    return;

                WCharacter root = retinue.IsElite ? f.RootElite : f.RootBasic;

                Log.Info($"IsElite: {retinue.IsElite}, Picking from root: {root.Name}");

                var pick = TroopMatcher.PickBestFromTree(root, retinue);

                if (pick?.IsValid == true)
                    sources.Add(pick);
            }

            AddSourceIfValid(retinue.Faction);
            AddSourceIfValid(retinue.Culture);

            return sources;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rank Up                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gold cost for ranking up a retinue (per tier).
        /// </summary>
        public static int RankUpCost(WCharacter retinue)
        {
            int tier = retinue?.Tier ?? 1;
            return tier * Config.RetinueRankUpCostPerTier;
        }

        /// <summary>
        /// Rank up a retinue, charging gold and XP.
        /// </summary>
        public static void RankUp(WCharacter retinue)
        {
            if (retinue == null || retinue.IsMaxTier)
                return;

            int cost = RankUpCost(retinue);
            if (Player.Gold < cost)
                return;

            if (!BattleXpBehavior.TrySpend(retinue, cost))
                return;

            Player.ChangeGold(-cost);
            retinue.Level += 5;
        }
    }
}
