using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Migration.Legacy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Migration
{
    // The troop definition conversion is shared by save migration and stable XML import.
    // Campaign progression and equipment usage policies live in separate save partitions.
    internal static class LegacyCharacterConverter
    {
        internal static void Apply(WCharacter wc, TroopSaveData troop)
        {
            // Captain properties redirect writes to their base troop. Keep each record
            // independent until every definition and XP pool has been restored, including
            // when importing over a captain that is already linked in this campaign.
            wc.CaptainBase = null;

            // ── Core identity ─────────────────────────────────────────
            if (!string.IsNullOrEmpty(troop.Name))
                wc.Name = troop.Name;
            wc.Level = troop.Level;
            wc.IsFemale = troop.IsFemale;
            if (troop.Race >= 0)
                wc.Race = troop.Race;
            if (!string.IsNullOrEmpty(troop.CultureId))
            {
                var co = MBObjectManager.Instance.GetObject<CultureObject>(troop.CultureId);
                if (co != null)
                    wc.Culture = WCulture.Get(co);
            }

            // ── Source origin (for TOR and other id-keyed mod data) ───
            // Carry the v1 template id so the source-keyed compat sync (e.g. The Old
            // Realms) keeps migrated custom troops' special attributes immediately.
            if (
                !string.IsNullOrEmpty(troop.VanillaStringId)
                && troop.VanillaStringId != troop.StringId
            )
                wc.SourceStringId = troop.VanillaStringId;

            // ── Formation / captain flags ─────────────────────────────
            wc.FormationClassOverride = troop.FormationClassOverride;
            wc.IsMariner = troop.IsMariner;
            wc.IsCaptain = troop.IsCaptain;
            wc.IsCaptainEnabled = troop.CaptainEnabled;

            // ── Skills ────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(troop.SkillData?.Code))
            {
                foreach (var part in troop.SkillData.Code.Split(';'))
                {
                    var kv = part.Split(':');
                    if (kv.Length != 2)
                        continue;
                    var skill = MBObjectManager.Instance.GetObject<SkillObject>(kv[0].Trim());
                    if (skill == null)
                        continue;
                    if (!int.TryParse(kv[1].Trim(), out var sv) || sv < 0)
                        continue;
                    wc.Skills.Set(skill, sv);
                }
            }

            // ── Skill budget baseline (floor) ─────────────────────────
            // Keep migrated troops editable even if their v1 skills exceed the v2 tier
            // budget. Pre-field-17 saves store 0, so backfill from the migrated skill sum.
            wc.SkillBaseline = troop.SkillBaseline > 0 ? troop.SkillBaseline : wc.SkillTotalUsed;

            // ── Equipment ─────────────────────────────────────────────
            if (troop.EquipmentData?.Codes?.Count > 0)
            {
                bool hasCivilianFlags =
                    troop.EquipmentData.Civilians?.Count == troop.EquipmentData.Codes.Count;
                var roster = new List<MEquipment>();
                for (int i = 0; i < troop.EquipmentData.Codes.Count; i++)
                {
                    var code = troop.EquipmentData.Codes[i];
                    if (string.IsNullOrEmpty(code))
                        continue;
                    var me = MEquipment.FromCode(wc, code);
                    if (me == null)
                        continue;
                    me.IsCivilian = hasCivilianFlags
                        ? troop.EquipmentData.Civilians[i]
                        : (i == 1);
                    roster.Add(me);
                }
                if (roster.Count > 0)
                    wc.EquipmentRoster.Equipments = roster;
            }

            // ── Body ──────────────────────────────────────────────────
            if (troop.BodyData != null)
            {
                if (troop.BodyData.AgeMin > 0)
                    wc.AgeMin = troop.BodyData.AgeMin;
                if (troop.BodyData.AgeMax > 0)
                    wc.AgeMax = troop.BodyData.AgeMax;
                if (troop.BodyData.WeightMin > 0)
                    wc.WeightMin = troop.BodyData.WeightMin;
                if (troop.BodyData.WeightMax > 0)
                    wc.WeightMax = troop.BodyData.WeightMax;
                if (troop.BodyData.BuildMin > 0)
                    wc.BuildMin = troop.BodyData.BuildMin;
                if (troop.BodyData.BuildMax > 0)
                    wc.BuildMax = troop.BodyData.BuildMax;
                if (troop.BodyData.HeightMin > 0)
                    wc.HeightMin = troop.BodyData.HeightMin;
                if (troop.BodyData.HeightMax > 0)
                    wc.HeightMax = troop.BodyData.HeightMax;
            }
        }

        // Call only after all records and their progression have been applied.
        internal static void LinkCaptain(WCharacter wc, TroopSaveData troop,
            Func<string, WCharacter> resolveCaptain)
        {
            if (wc == null || troop.Captain?.StringId == null)
                return;
            var captain = resolveCaptain(troop.Captain.StringId);
            if (captain == null || captain.StringId == wc.StringId)
                return;
            wc.Captain = captain;
            captain.CaptainBase = wc;
        }
    }
}
