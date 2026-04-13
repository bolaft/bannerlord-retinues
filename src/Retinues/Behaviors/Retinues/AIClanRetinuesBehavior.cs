using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Retinues.Behaviors.Troops;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Behaviors.Retinues
{
    /// <summary>
    /// Manages retinue creation, daily recruitment, tier-change eligibility checks,
    /// and encyclopedia visibility for all non-player (AI) clans.
    /// </summary>
    public sealed class AIClanRetinuesBehavior : BaseCampaignBehavior<AIClanRetinuesBehavior>
    {
        private static readonly Random _rng = new();

        // Accumulated per-clan promotion counts for the daily summary log.
        // Key = clan StringId, Value = (display name, count) for the current day.
        private readonly Dictionary<string, (string Name, int Count)> _pendingRecruitLog = new();

        public override bool IsActive => Configuration.EnableRetinues;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Events                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void RegisterCustomEvents()
        {
            ConfigurationManager.OptionChanged -= OnOptionChanged;
            ConfigurationManager.OptionChanged += OnOptionChanged;

            CampaignEvents.ClanTierIncrease.AddNonSerializedListener(
                this,
                (clan, _) => OnClanTierIncreased(clan)
            );

            // Sync encyclopedia visibility on every load regardless of IsActive, so
            // AI clan retinues are correctly hidden when the feature is disabled after a restart.
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(
                this,
                _ => SyncAIClanRetinueEncyclopediaVisibility()
            );
        }

        private void OnOptionChanged(string key, object value)
        {
            if (
                key == nameof(Configuration.EnableRetinues)
                || key == nameof(Configuration.EnableAIClanRetinues)
                || key == nameof(Configuration.AIClanRetinueMinTier)
            )
            {
                EnsureRetinuesForAllAIClans();
            }
        }

        private void OnClanTierIncreased(Clan clan)
        {
            if (!Configuration.EnableRetinues || !Configuration.EnableAIClanRetinues)
                return;

            var wClan = WClan.Get(clan?.StringId);
            if (wClan?.Base == null)
                return;

            if (wClan.Base == Clan.PlayerClan)
                return;

            Log.Debug(
                $"[AIClanRetinue] '{wClan.Name}' tier increased to {clan.Tier}; checking retinue eligibility."
            );

            EnsureRetinuesForAIClan(wClan);
            SyncEncyclopediaVisibilityForClan(wClan);
        }

        protected override void OnGameLoadFinished()
        {
            EnsureRetinuesForAllAIClans();
        }

        protected override void OnCharacterCreationIsOver()
        {
            EnsureRetinuesForAllAIClans();
        }

        /// <summary>
        /// Flushes the previous day's recruitment activity as a single debug summary line.
        /// </summary>
        protected override void OnDailyTick()
        {
            if (_pendingRecruitLog.Count == 0)
                return;

            var sb = new StringBuilder("[AIClanRetinue] Daily promotions:");
            foreach (var kv in _pendingRecruitLog.Values)
                sb.Append($" {kv.Name}×{kv.Count}");

            Log.Debug(sb.ToString());
            _pendingRecruitLog.Clear();
        }

        /// <summary>
        /// Promotes one matching-tier regular troop into a clan retinue when the daily chance succeeds.
        /// Accumulates per-clan promotion counts for the daily summary log.
        /// </summary>
        protected override void OnDailyTickParty(WParty party)
        {
            if (!Configuration.EnableRetinues || !Configuration.EnableAIClanRetinues)
                return;

            if (party?.Base == null)
                return;

            if (!party.IsLordParty || party.IsMainParty)
                return;

            var rawClan = party.Base.ActualClan ?? party.Base.LeaderHero?.Clan;
            if (rawClan == null)
                return;

            if (rawClan == Clan.PlayerClan)
                return;

            var clan = WClan.Get(rawClan.StringId);
            if (clan?.Base == null)
                return;

            if (clan.IsEliminated || clan.IsBanditFaction)
                return;

            if (rawClan.Tier < Configuration.AIClanRetinueMinTier)
                return;

            var retinues = clan.RosterRetinues;
            if (retinues == null || retinues.Count == 0)
                return;

            var roster = party.MemberRoster;
            int currentRetinueCount = retinues.Sum(r => roster.CountOf(r));

            int cap = Math.Max(
                1,
                (int)(party.PartySizeLimit * (Configuration.AIClanRetinueCapPercent / 100.0))
            );

            if (currentRetinueCount >= cap)
                return;

            if (_rng.NextDouble() >= Configuration.AIClanRetinueConvertChance / 100.0)
                return;

            var retinue = retinues[_rng.Next(retinues.Count)];
            int retinueTier = retinue.Tier <= 0 ? 1 : retinue.Tier;

            var candidates = roster
                .Elements.Where(e =>
                    e.Number > 0
                    && !e.Troop.IsHero
                    && !e.Troop.IsRetinue
                    && e.Troop.Tier == retinueTier
                )
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = roster
                    .Elements.Where(e =>
                        e.Number > 0
                        && !e.Troop.IsHero
                        && !e.Troop.IsRetinue
                        && Math.Abs(e.Troop.Tier - retinueTier) == 1
                    )
                    .ToList();
            }

            if (candidates.Count == 0)
                return;

            var source = candidates[_rng.Next(candidates.Count)];
            roster.RemoveTroop(source.Troop, 1);
            roster.AddTroop(retinue, 1);

            var sid = clan.Base.StringId;
            _pendingRecruitLog.TryGetValue(sid, out var entry);
            _pendingRecruitLog[sid] = (clan.Name, entry.Count + 1);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Retinue Creation                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates retinues for all eligible AI clans if the feature is enabled,
        /// then syncs encyclopedia visibility for all AI clan retinues.
        /// </summary>
        internal void EnsureRetinuesForAllAIClans()
        {
            if (Configuration.EnableRetinues && Configuration.EnableAIClanRetinues)
            {
                foreach (var clan in WClan.All)
                {
                    if (clan?.Base == null)
                        continue;

                    EnsureRetinuesForAIClan(clan);
                }
            }

            SyncAIClanRetinueEncyclopediaVisibility();
        }

        /// <summary>
        /// Creates default retinues for a single AI clan if it is eligible and has none yet.
        /// </summary>
        internal void EnsureRetinuesForAIClan(WClan clan)
        {
            if (clan?.Base == null)
                return;

            if (clan.Base == Clan.PlayerClan)
                return;

            if (clan.IsEliminated || clan.IsBanditFaction)
                return;

            if (clan.Base.Tier < Configuration.AIClanRetinueMinTier)
                return;

            if (clan.RosterRetinues.Count > 0)
                return;

            int clanTier = clan.Base.Tier;
            int targetLevel = clanTier * 5;

            var houseGuardName = L.T("retinue_ai_clan_house_guard", "{CLAN} House Guard")
                .SetTextVariable("CLAN", clan.Name)
                .ToString();
            var houseGuard = CreateAIClanRetinue(clan.Culture, houseGuardName, targetLevel);
            if (houseGuard?.Base != null)
            {
                clan.AddRetinue(houseGuard);
                Log.Debug(
                    $"[AIClanRetinue] Created '{houseGuard.Name}' for {clan.Name} (tier {clanTier})."
                );
            }

            if (clanTier >= 5)
            {
                var houseChampionName = L.T(
                        "retinue_ai_clan_house_champion",
                        "{CLAN} House Champion"
                    )
                    .SetTextVariable("CLAN", clan.Name)
                    .ToString();
                var houseChampion = CreateAIClanRetinue(
                    clan.Culture,
                    houseChampionName,
                    targetLevel
                );
                if (houseChampion?.Base != null)
                {
                    clan.AddRetinue(houseChampion);
                    Log.Debug(
                        $"[AIClanRetinue] Created '{houseChampion.Name}' for {clan.Name} (tier {clanTier})."
                    );
                }
            }

            var clanLeader = clan.Base.Leader;
            if (clanLeader?.IsKingdomLeader == true)
            {
                var kingdom = WKingdom.Get(clan.Base.Kingdom);
                if (kingdom?.Base != null)
                {
                    var guardName = clanLeader.IsFemale
                        ? L.T("retinue_kingdom_default_name_female", "{KINGDOM} Queen's Guard")
                            .SetTextVariable("KINGDOM", kingdom.Name)
                            .ToString()
                        : L.T("retinue_kingdom_default_name_male", "{KINGDOM} King's Guard")
                            .SetTextVariable("KINGDOM", kingdom.Name)
                            .ToString();
                    var guard = CreateAIClanRetinue(clan.Culture, guardName, targetLevel);
                    if (guard?.Base != null)
                    {
                        clan.AddRetinue(guard);
                        Log.Debug(
                            $"[AIClanRetinue] Created '{guard.Name}' for {clan.Name} ruling lord (tier {clanTier})."
                        );
                    }
                }
            }
        }

        private static WCharacter CreateAIClanRetinue(
            WCulture culture,
            string name,
            int targetLevel
        )
        {
            if (!Configuration.EnableRetinues || !Configuration.EnableAIClanRetinues)
                return null;

            var template = culture?.RootElite ?? culture?.RootBasic;
            if (template?.Base == null)
                return null;

            return Cloner.BuildFromTemplate(
                template,
                new Cloner.TroopBuildRequest
                {
                    Name = name,
                    CultureContext = culture,
                    CopySkills = true,
                    CreateCivilianSet = true,
                    UnlockItems = false,
                    NotifyUnlocks = false,
                    TargetLevel = targetLevel,
                    ForceRandomEquipment = true,
                    // 6 means the only cap is the troop's own tier (owner.Tier in ItemRandomizer).
                    MaxRandomItemTierOverride = 6,
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Encyclopedia Visibility                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows or hides AI clan retinues in the encyclopedia for all AI clans.
        /// Retinues are visible when enabled and the clan meets the minimum tier; hidden otherwise.
        /// </summary>
        internal void SyncAIClanRetinueEncyclopediaVisibility()
        {
            foreach (var clan in WClan.All)
            {
                if (clan?.Base == null)
                    continue;

                SyncEncyclopediaVisibilityForClan(clan);
            }
        }

        private void SyncEncyclopediaVisibilityForClan(WClan clan)
        {
            if (clan.Base == Clan.PlayerClan)
                return;

            if (clan.GetRawRetinues().Count == 0)
                return;

            bool shouldShow =
                Configuration.EnableRetinues
                && Configuration.EnableAIClanRetinues
                && !clan.IsEliminated
                && !clan.IsBanditFaction
                && clan.Base.Tier >= Configuration.AIClanRetinueMinTier;

            foreach (var retinue in clan.GetRawRetinues())
            {
                if (retinue?.Base == null)
                    continue;

                retinue.HiddenInEncyclopedia = !shouldShow;
            }
        }
    }
}
