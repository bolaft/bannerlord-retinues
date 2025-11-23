using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Mods;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class CulturalPride : Doctrine
    {
        public override TextObject Name => L.T("cultural_pride", "Cultural Pride");
        public override TextObject Description =>
            L.T("cultural_pride_description", "Unlocks custom militia troops.");
        public override int Column => 1;
        public override int Row => 0;
        public override bool IsDisabled => Config.NoDoctrineRequirements;
        public override TextObject DisabledMessage =>
            L.T(
                "cultural_pride_disabled_message",
                "Disabled: special troops unlocked from config."
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_TournamentOwnCultureGear : Feat
        {
            public override TextObject Description =>
                L.T(
                    "cultural_pride_tournament_own_culture_gear",
                    "Win a tournament wearing only armor of your own culture."
                );
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner != Player.Character)
                    return;

                if (ModCompatibility.SkipItemCultureChecks)
                    AdvanceProgress(1); // Items don't have cultures in those mods, skip the check.

                var blockedBy = new List<ItemObject>();

                // NOTE: Player character's wrapper gives wrong equipment for some reason in 1.3
                // This is why we access the equipment directly from the CharacterObject's FirstBattleEquipment
                Equipment eq = CharacterObject.PlayerCharacter.FirstBattleEquipment;

                EquipmentIndex[] indices =
                [
                    EquipmentIndex.Head,
                    EquipmentIndex.Cape,
                    EquipmentIndex.Body,
                    EquipmentIndex.Gloves,
                    EquipmentIndex.Leg,
                ];
                foreach (EquipmentIndex index in indices)
                {
                    var item = eq[index].Item;

                    if (item == null)
                        continue; // Missing armor piece

                    if (item.Culture?.StringId != Player.Culture.StringId)
                    {
                        Log.Info(
                            $"CP_TournamentOwnCultureGear: item {item.Name} ({item.Culture?.StringId}) does not match player culture ({Player.Culture})"
                        );
                        if (!blockedBy.Contains(item))
                            blockedBy.Add(item);
                    }
                }

                if (blockedBy.Count > 0)
                {
                    Log.Message(
                        $"Cultural Pride: progress for tournament win blocked by: {string.Join(", ", blockedBy.Select(item => $"{item.Name} ({item.Culture?.Name})"))}."
                    );
                }
                else
                {
                    AdvanceProgress(1);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_FullSet100Kills : Feat
        {
            public override TextObject Description =>
                L.T(
                    "cultural_pride_full_set_100_kills",
                    "Get 100 kills in battle with troops wearing no foreign gear."
                );
            public override int Target => 100;

            public override void OnBattleEnd(Battle battle)
            {
                var blockedBy = new Dictionary<WCharacter, List<WItem>>();

                foreach (var kill in battle.Kills)
                {
                    if (!kill.KillerIsPlayerTroop)
                        continue; // Only consider player troops

                    var troop = kill.Killer;

                    if (!troop.IsCustom)
                        continue; // Only consider custom troops

                    bool hasFullSet = true;
                    foreach (var item in troop.Loadout.Battle.Items)
                    {
                        if (item.Culture == null)
                            continue; // Ignore culture-less items
                        if (item.Culture != troop.Culture)
                        {
                            hasFullSet = false;
                            if (!blockedBy.ContainsKey(troop))
                                blockedBy[troop] = [];
                            if (!blockedBy[troop].Contains(item))
                                blockedBy[troop].Add(item);
                        }
                    }

                    if (hasFullSet)
                        AdvanceProgress(1);
                }

                foreach (var kvp in blockedBy)
                {
                    Log.Message(
                        $"Cultural Pride: progress for {kvp.Key.Name} ({kvp.Key.Culture?.Name}) blocked by: {string.Join(", ", kvp.Value.Select(item => $"{item.Name} ({item.Culture?.Name})"))}."
                    );
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_DefeatForeignRuler : Feat
        {
            public override TextObject Description =>
                L.T(
                    "cultural_pride_defeat_foreign_ruler",
                    "Defeat a ruler of a different culture in battle."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;

                foreach (var leader in battle.EnemyLeaders)
                {
                    if (!(leader?.IsRuler ?? false))
                        continue; // Not a ruler

                    if (leader?.Culture == Player.Culture)
                        continue; // Same culture

                    AdvanceProgress(1);
                    break;
                }
            }
        }
    }
}
