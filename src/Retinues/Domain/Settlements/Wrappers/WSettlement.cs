using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Models;
using Retinues.Framework.Model;
using Retinues.Game;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace Retinues.Domain.Settlements.Wrappers
{
    public class WSettlement(Settlement @base) : WBase<WSettlement, Settlement>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Identity                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name?.ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCulture Culture => WCulture.Get(Base.Culture);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Town                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MTown Town => new(Base.Town);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Heroes                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WHero Owner => WHero.Get(Base.Owner);
        public List<WHero> Notables => [.. Base.Notables.Select(WHero.Get)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WClan Clan => WClan.Get(Base.OwnerClan);

        public WKingdom Kingdom
        {
            get
            {
                var clan = Base.OwnerClan;
                if (clan == null)
                    return null;

                return WKingdom.Get(clan.Kingdom);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Base troops ━━━━━ */

        public bool HasClanTroops => Clan?.RootBasic != null || Clan?.RootElite != null;
        public bool HasKingdomTroops => Kingdom?.RootBasic != null || Kingdom?.RootElite != null;

        /// <summary>
        /// Canonical troop source for settlement volunteer generation.
        /// This is not affected by any settings.
        /// Clan takes priority over kingdom.
        /// </summary>
        public IBaseFaction GetBaseTroopsFaction()
        {
            if (HasClanTroops)
                return Clan;

            if (HasKingdomTroops)
                return Kingdom;

            return null;
        }

        public WCharacter GetBaseRoot(bool elite)
        {
            var faction = GetBaseTroopsFaction();
            if (faction != null)
                return GetFactionRoot(faction, elite) ?? GetVanillaRoot(elite);

            return GetVanillaRoot(elite);
        }

        public WCharacter GetBaseEliteRoot() => GetBaseRoot(elite: true);

        public WCharacter GetBaseBasicRoot() => GetBaseRoot(elite: false);

        /* ━━━━━━━━ Player ━━━━━━━━ */

        public bool CanPlayerRecruitClanTroops()
        {
            if (Player.Clan?.RootBasic == null && Player.Clan?.RootElite == null)
                return false;

            return Settings.ClanTroopsAvailability.Value switch
            {
                Settings.RecruitmentMode.Anywhere => true,
                Settings.RecruitmentMode.FactionFiefs => Clan == Player.Clan,
                Settings.RecruitmentMode.ClanOrKingdomFiefs => Clan == Player.Clan
                    || Kingdom == Player.Kingdom,
                Settings.RecruitmentMode.Nowhere => false,
                _ => false,
            };
        }

        public bool CanPlayerRecruitKingdomTroops()
        {
            if (Player.Kingdom?.RootBasic == null && Player.Kingdom?.RootElite == null)
                return false;

            return Settings.KingdomTroopsAvailability.Value switch
            {
                Settings.RecruitmentMode.Anywhere => true,
                Settings.RecruitmentMode.FactionFiefs => Kingdom == Player.Kingdom,
                Settings.RecruitmentMode.ClanOrKingdomFiefs => Clan == Player.Clan
                    || Kingdom == Player.Kingdom,
                Settings.RecruitmentMode.Nowhere => false,
                _ => false,
            };
        }

        public bool CanPlayerRecruitAnyCustomTroops() =>
            CanPlayerRecruitClanTroops() || CanPlayerRecruitKingdomTroops();

        /// <summary>
        /// Returns all eligible troop roots for the player recruitment view.
        /// The list can contain:
        /// - Player clan roots (if eligible)
        /// - Player kingdom roots (if eligible)
        /// - Culture roots (if MixWithVanillaTroops is enabled)
        ///
        /// Rule: the player only sees vanilla if neither clan nor kingdom troops are available.
        /// </summary>
        public List<WCharacter> GetPlayerRoots(bool elite)
        {
            var roots = new List<WCharacter>();

            Log.Info("Recruitement: determining player eligible roots.");

            if (CanPlayerRecruitClanTroops())
                TryAddRootUnique(roots, GetFactionRoot(Player.Clan, elite));

            if (CanPlayerRecruitKingdomTroops())
                TryAddRootUnique(roots, GetFactionRoot(Player.Kingdom, elite));

            // Player only sees vanilla when:
            // - no custom roots are eligible, OR
            // - MixWithVanillaTroops is enabled (additive).
            if (roots.Count == 0 || Settings.MixWithVanillaTroops.Value)
                TryAddRootUnique(roots, GetVanillaRoot(elite));

            return roots;
        }

        public List<WCharacter> GetEliteRoots() => GetPlayerRoots(elite: true);

        public List<WCharacter> GetBasicRoots() => GetPlayerRoots(elite: false);

        /* ━━━━ AI restriction ━━━━ */

        /// <summary>
        /// Returns true if the recruiter is allowed to benefit from custom troops in this settlement.
        /// If the settlement has no base custom troops, this returns true.
        /// </summary>
        public bool IsRecruiterAllowedForCustomTroops(WParty recruiter)
        {
            var baseFaction = GetBaseTroopsFaction();
            if (baseFaction == null)
                return true;

            return Settings.AllowedRecruiters.Value switch
            {
                Settings.AllowedRecruitersMode.Everyone => true,

                Settings.AllowedRecruitersMode.PlayerOnly => recruiter != null
                    && recruiter.IsMainParty,

                Settings.AllowedRecruitersMode.FactionOnly => IsRecruiterMatchingBaseFaction(
                    recruiter,
                    baseFaction
                ),

                _ => true,
            };
        }

        public bool ShouldForceVanillaForRecruiter(WParty recruiter)
        {
            var baseFaction = GetBaseTroopsFaction();
            if (baseFaction == null)
                return false;

            return !IsRecruiterAllowedForCustomTroops(recruiter);
        }

        private bool IsRecruiterMatchingBaseFaction(WParty recruiter, IBaseFaction baseFaction)
        {
            if (recruiter == null || baseFaction == null)
                return false;

            if (baseFaction is WClan clan)
                return recruiter.Clan == clan;

            if (baseFaction is WKingdom kingdom)
                return recruiter.Kingdom == kingdom;

            return false;
        }

        private WCharacter GetFactionRoot(IBaseFaction faction, bool elite)
        {
            if (faction == null)
                return null;

            var root = elite ? faction.RootElite : faction.RootBasic;
            root ??= elite ? faction.RootBasic : faction.RootElite;

            return root;
        }

        private WCharacter GetVanillaRoot(bool elite)
        {
            var culture = Culture;
            if (culture == null)
                return null;

            var root = elite ? culture.RootElite : culture.RootBasic;
            root ??= elite ? culture.RootBasic : culture.RootElite;

            return root;
        }

        private void TryAddRootUnique(List<WCharacter> roots, WCharacter root)
        {
            if (root == null)
                return;

            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == root)
                    return;
            }

            roots.Add(root);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Kind / Type                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsTown => Base.IsTown;
        public bool IsCastle => Base.IsCastle;
        public bool IsVillage => Base.IsVillage;
        public bool IsHideout => Base.IsHideout;
        public bool IsFortification => Base.IsFortification;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Village Village => Base.Village;
        public Hideout Hideout => Base.Hideout;
    }
}
