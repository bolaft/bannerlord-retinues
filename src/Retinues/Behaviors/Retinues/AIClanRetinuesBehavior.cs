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
    public sealed partial class AIClanRetinuesBehavior
        : BaseCampaignBehavior<AIClanRetinuesBehavior>
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

            CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(
                this,
                (oldLeader, newLeader) => OnClanLeaderChanged(oldLeader, newLeader)
            );

            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(
                this,
                (clan, oldKingdom, newKingdom, detail, show) =>
                    OnClanChangedKingdom(clan, newKingdom)
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

        private void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
        {
            if (!Configuration.EnableRetinues || !Configuration.EnableAIClanRetinues)
                return;

            var clan = newLeader?.Clan;
            if (clan == null || clan == Clan.PlayerClan)
                return;

            // Only refresh the guard when this clan is still a ruling clan.
            if (!newLeader.IsKingdomLeader)
                return;

            var wClan = WClan.Get(clan.StringId);
            if (wClan?.Base == null)
                return;

            // The leader's gender may have changed; update King's/Queen's Guard accordingly.
            RefreshKingsGuardForClan(wClan, newLeader);
        }

        private void OnClanChangedKingdom(Clan clan, Kingdom newKingdom)
        {
            if (!Configuration.EnableRetinues || !Configuration.EnableAIClanRetinues)
                return;

            if (newKingdom == null || clan == Clan.PlayerClan)
                return;

            // Only act if this clan is now the ruling clan of its new kingdom.
            if (newKingdom.RulingClan != clan)
                return;

            var wClan = WClan.Get(clan.StringId);
            if (wClan?.Base == null)
                return;

            RefreshKingsGuardForClan(wClan, clan.Leader);
        }

        /// <summary>
        /// Removes any existing King's/Queen's Guard retinue from the clan and creates a fresh one
        /// keyed to the current leader's gender. No-op if the leader is not a kingdom leader.
        /// </summary>
        private void RefreshKingsGuardForClan(WClan clan, Hero leader)
        {
            if (clan?.Base == null)
                return;

            if (clan.Base.Tier < Configuration.AIClanRetinueMinTier)
                return;

            // Remove the existing guard (if any).
            var rawRetinues = clan.GetRawRetinues();
            var existing = rawRetinues.Find(IsKingsGuardRetinue);
            if (existing != null)
            {
                rawRetinues.Remove(existing);
                clan.SetRetinues(rawRetinues);
                try
                {
                    existing.Remove();
                }
                catch { }
                Log.Debug(
                    $"[AIClanRetinue] Removed stale king's guard '{existing.Name}' from {clan.Name}."
                );
            }

            // Only create a new guard when this clan is still a ruling clan.
            if (leader?.IsKingdomLeader != true)
                return;

            var guardName = leader.IsFemale
                ? L.T("retinue_clan_default_name_female", "{CLAN} Queen's Guard")
                    .SetTextVariable("CLAN", clan.Name)
                    .ToString()
                : L.T("retinue_clan_default_name_male", "{CLAN} King's Guard")
                    .SetTextVariable("CLAN", clan.Name)
                    .ToString();

            var guard = CreateAIClanRetinue(clan.Culture, guardName, 6);
            if (guard?.Base != null)
            {
                clan.AddRetinue(guard);
                Log.Debug($"[AIClanRetinue] Created king's guard '{guard.Name}' for {clan.Name}.");
            }
        }

        private static bool IsKingsGuardRetinue(WCharacter retinue)
        {
            var name = retinue?.Name ?? string.Empty;
            return name.EndsWith("King's Guard", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("Queen's Guard", StringComparison.OrdinalIgnoreCase);
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
        /// Flushes the previous day's recruitment activity as a single debug summary line,
        /// then runs the daily equipment upgrade pass for all AI retinues.
        /// </summary>
        protected override void OnDailyTick()
        {
            if (_pendingRecruitLog.Count > 0)
            {
                var sb = new StringBuilder("[AIClanRetinue] Daily promotions:");
                foreach (var kv in _pendingRecruitLog.Values)
                    sb.Append($" {kv.Name}×{kv.Count}");

                Log.Debug(sb.ToString());
                _pendingRecruitLog.Clear();
            }

            TryDailyEquipmentUpgradesForAllAIRetinues();
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

            if (Configuration.AIClanRetinueLeaderOnly && party.Base.LeaderHero != rawClan.Leader)
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
            int guardTier = Math.Min(3, clanTier);

            var houseGuardName = L.T("retinue_ai_clan_house_guard", "{CLAN} House Guard")
                .SetTextVariable("CLAN", clan.Name)
                .ToString();
            var houseGuard = CreateAIClanRetinue(clan.Culture, houseGuardName, guardTier);
            if (houseGuard?.Base != null)
            {
                clan.AddRetinue(houseGuard);
                Log.Debug(
                    $"[AIClanRetinue] Created '{houseGuard.Name}' for {clan.Name} (guard tier {guardTier})."
                );
            }

            if (clanTier >= 4)
            {
                var houseChampionName = L.T(
                        "retinue_ai_clan_house_champion",
                        "{CLAN} House Champion"
                    )
                    .SetTextVariable("CLAN", clan.Name)
                    .ToString();
                var houseChampion = CreateAIClanRetinue(clan.Culture, houseChampionName, clanTier);
                if (houseChampion?.Base != null)
                {
                    clan.AddRetinue(houseChampion);
                    Log.Debug(
                        $"[AIClanRetinue] Created '{houseChampion.Name}' for {clan.Name} (champion tier {clanTier})."
                    );
                }
            }

            var clanLeader = clan.Base.Leader;
            if (clanLeader?.IsKingdomLeader == true)
            {
                var guardName = clanLeader.IsFemale
                    ? L.T("retinue_clan_default_name_female", "{CLAN} Queen's Guard")
                        .SetTextVariable("CLAN", clan.Name)
                        .ToString()
                    : L.T("retinue_clan_default_name_male", "{CLAN} King's Guard")
                        .SetTextVariable("CLAN", clan.Name)
                        .ToString();
                var guard = CreateAIClanRetinue(clan.Culture, guardName, 6);
                if (guard?.Base != null)
                {
                    clan.AddRetinue(guard);
                    Log.Debug(
                        $"[AIClanRetinue] Created '{guard.Name}' for {clan.Name} ruling lord (tier 6)."
                    );
                }
            }
        }

        private static WCharacter CreateAIClanRetinue(WCulture culture, string name, int targetTier)
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
                    TargetLevel = targetTier * 5 + 1,
                    ForceRandomEquipment = true,
                    // MaxRandomItemTierOverride = 6: no config cap, only owner tier cap.
                    MaxRandomItemTierOverride = 6,
                    // MinRandomItemTierOverride: floor item selection at the retinue's own tier
                    // so T6 kingsguard always gets T6 gear rather than template-tier items.
                    MinRandomItemTierOverride = targetTier,
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
