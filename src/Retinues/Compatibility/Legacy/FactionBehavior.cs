using System;
using System.Collections.Generic;
using Retinues.Compatibility.Legacy.Save;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Modules;
using Retinues.Framework.Modules.Versions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Compatibility.Legacy
{
    /// <summary>
    /// Compatibility behavior to load legacy Retinues troop save data and migrate it
    /// into the current wrapper-based persistence system.
    /// </summary>
    public sealed class FactionBehavior : BaseCampaignBehavior
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string KeyClanTroops = "Retinues_ClanTroops";
        private const string KeyKingdomTroops = "Retinues_KingdomTroops";
        private const string KeyCultureTroops = "Retinues_CultureTroops";
        private const string KeyMinorClanTroops = "Retinues_MinorClanTroops";

        private const string KeyMigrationDone = "Retinues_LegacyMigration_TroopsDone";

        private FactionSaveData _clanTroops;
        private FactionSaveData _kingdomTroops;
        private List<FactionSaveData> _cultureTroops;
        private List<FactionSaveData> _minorClanTroops;

        private bool _migrationDone;

        /// <summary>
        /// Sync legacy payload and migration marker to/from the save.
        /// Uses the same keys as legacy Retinues versions so older saves deserialize.
        /// When saving after a successful migration, clears the legacy payload so it is dropped.
        /// </summary>
        /// <param name="ds">Bannerlord save datastore.</param>
        public override void SyncData(IDataStore ds)
        {
            try
            {
                // Migration marker
                ds.SyncData(KeyMigrationDone, ref _migrationDone);

                // Legacy payload
                ds.SyncData(KeyClanTroops, ref _clanTroops);
                ds.SyncData(KeyKingdomTroops, ref _kingdomTroops);
                ds.SyncData(KeyCultureTroops, ref _cultureTroops);
                ds.SyncData(KeyMinorClanTroops, ref _minorClanTroops);

                // If we've already migrated, keep legacy fields cleared so the next save
                // will naturally drop the old payload.
                if (ds.IsSaving && _migrationDone)
                {
                    _clanTroops = null;
                    _kingdomTroops = null;
                    _cultureTroops = null;
                    _minorClanTroops = null;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "Legacy FactionBehavior.SyncData failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Migration                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Performs one-time migration after the campaign has finished loading.
        /// Applies legacy troop edits into the current wrapper-based persistence system.
        /// </summary>
        protected override void OnGameLoadFinished()
        {
            try
            {
                if (_migrationDone)
                    return;

                if (!HasLegacyPayload())
                    return;

                if (!ShouldRunMigrationBasedOnVersion())
                {
                    Log.Debug("Legacy migration skipped due to version gating.");
                    return;
                }

                var migratedTroops = MigrateAllTroops();

                // Mark done even if we only migrated a subset; we don't want to re-run forever.
                _migrationDone = true;

                // Clear legacy payload so it gets dropped on next save.
                _clanTroops = null;
                _kingdomTroops = null;
                _cultureTroops = null;
                _minorClanTroops = null;

                Log.Info($"Legacy troop migration completed. Updated {migratedTroops} troop(s).");
            }
            catch (Exception e)
            {
                Log.Exception(e, "Legacy troop migration failed.");
            }
        }

        /// <summary>
        /// Returns true if any legacy payload data was loaded from the save.
        /// </summary>
        private bool HasLegacyPayload()
        {
            if (_clanTroops != null)
                return true;
            if (_kingdomTroops != null)
                return true;
            if ((_cultureTroops?.Count ?? 0) > 0)
                return true;
            if ((_minorClanTroops?.Count ?? 0) > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Uses the same parsing / direct-upgrade rules as VersionBehavior to avoid applying
        /// legacy migrations in downgrade or mismatch scenarios.
        /// If version info is missing/unparseable, we allow migration (old saves).
        /// </summary>
        private static bool ShouldRunMigrationBasedOnVersion()
        {
            try
            {
                var currentModule = ModuleManager.GetModule("Retinues");
                if (currentModule == null)
                    return true; // can't reason about versions, don't block migration

                var currentAppVersion = currentModule.AppVersion;
                if (currentAppVersion == ApplicationVersion.Empty)
                    return true;

                var vb = Campaign.Current?.GetCampaignBehavior<VersionBehavior>();
                if (vb == null)
                    return true;

                // VersionBehavior stores the loaded version in a private field.
                var savedVersionString = Reflection.GetFieldValue<string>(vb, "_savedVersion");
                if (string.IsNullOrWhiteSpace(savedVersionString))
                    return true;

                if (savedVersionString == ModuleManager.UnknownVersionString)
                    return true;

                if (!TryParseAppVersion(savedVersionString, out var saveAppVersion))
                    return true;

                // Only run migration when the current mod is newer than the save.
                // (Direct upgrade semantics are kept consistent with VersionBehavior.)
                return IsDirectUpgrade(saveAppVersion, currentAppVersion)
                    || currentAppVersion.IsNewerThan(saveAppVersion);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Parses a stored module version string into an <see cref="ApplicationVersion"/>.
        /// Accepts both "vX.Y.Z" and bare "X.Y.Z" styles.
        /// </summary>
        private static bool TryParseAppVersion(string versionString, out ApplicationVersion version)
        {
            version = ApplicationVersion.Empty;

            if (string.IsNullOrWhiteSpace(versionString))
                return false;

            if (versionString == ModuleManager.UnknownVersionString)
                return false;

            var s = versionString.Trim();
            if (!char.IsLetter(s[0]))
                s = "v" + s;

            try
            {
                version = ApplicationVersion.FromString(s, defaultChangeSet: 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="currentVersion"/> is a "direct upgrade" step from
        /// <paramref name="saveVersion"/> according to the same major/minor stepping rules
        /// used by the version behavior.
        /// </summary>
        private static bool IsDirectUpgrade(
            ApplicationVersion saveVersion,
            ApplicationVersion currentVersion
        )
        {
            if (!currentVersion.IsNewerThan(saveVersion))
                return false;

            int saveMajor = saveVersion.Revision;
            int saveMinor = saveVersion.ChangeSet;
            int curMajor = currentVersion.Revision;
            int curMinor = currentVersion.ChangeSet;

            bool isMinorStep = curMajor == saveMajor && curMinor == saveMinor + 1;
            bool isMajorStep = curMajor == saveMajor + 1 && curMinor == 0;

            return isMinorStep || isMajorStep;
        }

        /// <summary>
        /// Migrates all legacy troop save entries into the current persistence system.
        /// Runs in two passes: first applies core scalar state, then applies links.
        /// </summary>
        /// <returns>The number of troops successfully updated in the first pass.</returns>
        private int MigrateAllTroops()
        {
            var byId = new Dictionary<string, TroopSaveData>(StringComparer.Ordinal);

            AddFaction(byId, _clanTroops);
            AddFaction(byId, _kingdomTroops);

            if (_cultureTroops != null)
                for (int i = 0; i < _cultureTroops.Count; i++)
                    AddFaction(byId, _cultureTroops[i]);

            if (_minorClanTroops != null)
                for (int i = 0; i < _minorClanTroops.Count; i++)
                    AddFaction(byId, _minorClanTroops[i]);

            int migrated = 0;

            // First pass: apply scalar fields so all referenced troops exist/are updated.
            foreach (var kv in byId)
                if (ApplyTroopCore(kv.Value))
                    migrated++;

            // Second pass: apply links (upgrade targets / captains) after cores.
            foreach (var kv in byId)
                ApplyTroopLinks(kv.Value);

            return migrated;
        }

        /// <summary>
        /// Adds all troops referenced by the legacy faction save payload into the lookup.
        /// </summary>
        private static void AddFaction(Dictionary<string, TroopSaveData> byId, FactionSaveData f)
        {
            if (f == null)
                return;

            AddTroop(byId, f.RetinueElite);
            AddTroop(byId, f.RetinueBasic);
            AddTroop(byId, f.RootElite);
            AddTroop(byId, f.RootBasic);
            AddTroop(byId, f.MilitiaMelee);
            AddTroop(byId, f.MilitiaMeleeElite);
            AddTroop(byId, f.MilitiaRanged);
            AddTroop(byId, f.MilitiaRangedElite);
            AddTroop(byId, f.CaravanGuard);
            AddTroop(byId, f.CaravanMaster);
            AddTroop(byId, f.Villager);
            AddTroop(byId, f.PrisonGuard);

            AddTroops(byId, f.Mercenaries);
            AddTroops(byId, f.Bandits);
            AddTroops(byId, f.Civilians);
            AddTroops(byId, f.Heroes);
        }

        /// <summary>
        /// Adds a list of troops into the lookup.
        /// </summary>
        private static void AddTroops(
            Dictionary<string, TroopSaveData> byId,
            List<TroopSaveData> list
        )
        {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
                AddTroop(byId, list[i]);
        }

        /// <summary>
        /// Adds a single troop into the lookup (deduplicated by StringId) and recurses
        /// into upgrade targets and captain payload.
        /// </summary>
        private static void AddTroop(Dictionary<string, TroopSaveData> byId, TroopSaveData t)
        {
            if (t == null)
                return;

            if (string.IsNullOrWhiteSpace(t.StringId))
                return;

            if (!byId.ContainsKey(t.StringId))
                byId[t.StringId] = t;

            // Recurse: upgrade targets and captain save data.
            if (t.UpgradeTargets != null)
                for (int i = 0; i < t.UpgradeTargets.Count; i++)
                    AddTroop(byId, t.UpgradeTargets[i]);

            if (t.Captain != null)
                AddTroop(byId, t.Captain);
        }

        /// <summary>
        /// Applies the legacy troop's core scalar data (name/level/culture/skills/equipment/body/etc.)
        /// into the current <see cref="WCharacter"/> instance.
        /// </summary>
        /// <returns>True if the troop was found and updated; otherwise false.</returns>
        private static bool ApplyTroopCore(TroopSaveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.StringId))
                return false;

            var wc = WCharacter.Get(data.StringId);
            if (wc?.Base == null)
            {
                Log.Warning($"Legacy migration: troop '{data.StringId}' not found; skipping.");
                return false;
            }

            // Scalars we can safely map 1:1.
            if (!string.IsNullOrWhiteSpace(data.Name))
                wc.Name = data.Name;

            if (data.Level > 0)
                wc.Level = data.Level;

            wc.IsFemale = data.IsFemale;
            wc.Race = data.Race;
            wc.FormationClassOverride = data.FormationClassOverride;
            wc.IsMariner = data.IsMariner;

            // Culture (best effort)
            if (!string.IsNullOrWhiteSpace(data.CultureId))
            {
                var culture = WCulture.Get(data.CultureId);
                if (culture?.Base != null)
                    wc.Culture = culture;
            }

            // Skills (best effort)
            try
            {
                var dict = data.SkillData?.Deserialize();
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        if (kv.Key == null)
                            continue;

                        wc.Skills.Set(kv.Key, kv.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(
                    e,
                    $"Legacy migration: failed to apply skills for '{data.StringId}'."
                );
            }

            // Equipment (best effort)
            try
            {
                var equipments = data.EquipmentData?.Deserialize(wc);
                if (equipments != null && equipments.Count > 0)
                    wc.EquipmentRoster.Equipments = equipments;
            }
            catch (Exception e)
            {
                Log.Exception(
                    e,
                    $"Legacy migration: failed to apply equipment for '{data.StringId}'."
                );
            }

            // Body ranges (best effort)
            try
            {
                var body = data.BodyData;
                if (body != null)
                {
                    wc.AgeMin = body.AgeMin;
                    wc.AgeMax = body.AgeMax;
                    wc.WeightMin = body.WeightMin;
                    wc.WeightMax = body.WeightMax;
                    wc.BuildMin = body.BuildMin;
                    wc.BuildMax = body.BuildMax;

                    // Height is optional in old logic; apply only if values look set.
                    if (body.HeightMin > 0 && body.HeightMax > 0)
                        wc.SetHeightRange(body.HeightMin, body.HeightMax);

                    // Pick a reasonable visual age from the range.
                    if (body.AgeMin > 0 || body.AgeMax > 0)
                        wc.Age = (body.AgeMin + body.AgeMax) * 0.5f;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Legacy migration: failed to apply body for '{data.StringId}'.");
            }

            // Captain flags (best effort)
            wc.IsCaptain = data.IsCaptain;
            wc.IsCaptainEnabled = data.CaptainEnabled;

            return true;
        }

        /// <summary>
        /// Applies legacy links after core migration: upgrade targets and captain binding.
        /// </summary>
        private static void ApplyTroopLinks(TroopSaveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.StringId))
                return;

            var wc = WCharacter.Get(data.StringId);
            if (wc?.Base == null)
                return;

            // Upgrade targets
            if (data.UpgradeTargets != null && data.UpgradeTargets.Count > 0)
            {
                var targets = new List<WCharacter>(data.UpgradeTargets.Count);

                for (int i = 0; i < data.UpgradeTargets.Count; i++)
                {
                    var t = data.UpgradeTargets[i];
                    if (t == null || string.IsNullOrWhiteSpace(t.StringId))
                        continue;

                    var wt = WCharacter.Get(t.StringId);
                    if (wt?.Base != null)
                        targets.Add(wt);
                }

                wc.UpgradeTargets = targets;
            }

            // Captain link (legacy stored captain object on the base troop)
            if (
                !data.IsCaptain
                && data.Captain != null
                && !string.IsNullOrWhiteSpace(data.Captain.StringId)
            )
            {
                var captain = WCharacter.Get(data.Captain.StringId);
                if (captain?.Base != null)
                {
                    wc.Captain = captain;
                    captain.CaptainBase = wc;

                    // Ensure captain is marked as a captain variant.
                    captain.IsCaptain = true;
                }
            }
        }
    }
}
