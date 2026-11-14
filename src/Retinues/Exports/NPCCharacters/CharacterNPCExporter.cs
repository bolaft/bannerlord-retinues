using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Exports.NPCCharacters
{
    /// <summary>
    /// Exports a Retinues WCharacter to an NPCCharacter XML element string.
    /// </summary>
    public static class NPCCharacterExporter
    {
        /// <summary>
        /// Returns the NPCCharacter XML string for the given character, optionally overriding the id.
        /// </summary>
        public static string ExportAsNPC(WCharacter character, string overrideStringId = null)
        {
            if (character == null)
                return string.Empty;

            var el = BuildNPCCharacterElement(character, overrideStringId);
            return el?.ToString(SaveOptions.DisableFormatting) ?? string.Empty;
        }

        /// <summary>
        /// Builds the XElement representing a single NPCCharacter for the given wrapped character.
        /// </summary>
        private static XElement BuildNPCCharacterElement(
            WCharacter character,
            string overrideStringId
        )
        {
            var c = character.Base;
            if (c == null)
                return null;

            var npc = new XElement("NPCCharacter");

            var finalId = !string.IsNullOrWhiteSpace(overrideStringId)
                ? overrideStringId
                : character.StringId;

            npc.SetAttributeValue("id", finalId);
            npc.SetAttributeValue("default_group", character.FormationClass.ToString());
            npc.SetAttributeValue("level", character.Level);
            npc.SetAttributeValue("is_hidden_encyclopedia", "false");
            npc.SetAttributeValue("name", character.Name ?? finalId);
            // NPC XML accepts an integer here; the exact age range is stored in <face>.
            npc.SetAttributeValue("age", Convert.ToInt32(character.Age));

            if (c.Race != 0)
            {
                var races = FaceGen.GetRaceNames();
                if (
                    races == null
                    || c.Race < 0
                    || c.Race >= races.Length
                    || string.IsNullOrWhiteSpace(races[c.Race])
                )
                    throw new InvalidOperationException(
                        $"Cannot export '{finalId}': race {c.Race} has no XML identifier."
                    );
                npc.SetAttributeValue("race", races[c.Race]);
            }

            var occ = Reflection.GetPropertyValue<object>(c, "Occupation");
            if (occ != null)
                npc.SetAttributeValue("occupation", occ.ToString());

            var isBasicTroop = Reflection.GetPropertyValue<bool>(c, "IsBasicTroop");
            npc.SetAttributeValue("is_basic_troop", isBasicTroop ? "true" : "false");

            var isFemale = Reflection.GetPropertyValue<bool>(c, "IsFemale");
            npc.SetAttributeValue("is_female", isFemale ? "true" : "false");

            if (character.Culture?.Base != null)
                npc.SetAttributeValue("culture", "Culture." + character.Culture.Base.StringId);

            var req = Reflection.GetPropertyValue<object>(c, "UpgradeRequiresItemFromCategory");
            var reqId = GetStringId(req);
            if (!string.IsNullOrWhiteSpace(reqId))
                npc.SetAttributeValue("upgrade_requires", "ItemCategory." + reqId);

            npc.Add(BuildFaceElement(character));

            npc.Add(BuildSkillsElement(character));
            npc.Add(BuildUpgradeTargetsElement(c));
            npc.Add(BuildEquipmentsElement(character, c));

            return npc;
        }

        /// <summary>
        /// Embeds the troop's own appearance range, including copies created at runtime.
        /// A generated module must load without the exporting campaign's object registry.
        /// </summary>
        private static XElement BuildFaceElement(WCharacter character)
        {
            if (character.Base.BodyPropertyRange == null)
                throw new InvalidOperationException(
                    $"Cannot export '{character.StringId}': missing body properties."
                );

            // Use the same source appearance as regular exports, even when body-mod
            // compatibility temporarily substitutes a culture template for rendering.
            var parts = character
                .SerializeBodyEnvelope()
                .Split([WCharacter.BodySerializedSeparator], StringSplitOptions.None);
            if (parts.Length != 2)
                throw new InvalidOperationException(
                    $"Cannot export '{character.StringId}': invalid body properties."
                );

            var minEl = ParseBodyElement(parts[0], character.StringId);
            var maxEl = ParseBodyElement(parts[1], character.StringId);
            maxEl.Name = "BodyPropertiesMax";

            var face = new XElement("face", minEl, maxEl);
#if BL13 || BL14
            var tags = character.Base.BodyPropertyRange;
#else
            var tags = character.Base;
#endif
            AddFaceTags(face, "hair", tags.HairTags);
            AddFaceTags(face, "beard", tags.BeardTags);
            AddFaceTags(face, "tattoo", tags.TattooTags);
            return face;
        }

        private static XElement ParseBodyElement(string xml, string characterId)
        {
            var element = XElement.Parse(xml);
            if (element.Name != "BodyProperties")
                throw new InvalidOperationException(
                    $"Cannot export '{characterId}': invalid body properties."
                );

            // The game's BodyProperties.ToString uses the current decimal separator.
            // Module XML must use invariant numbers, including cached source appearances.
            foreach (var name in new[] { "age", "weight", "build" })
            {
                var text = (string)element.Attribute(name);
                if (
                    !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    && !float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                    || float.IsNaN(value)
                    || float.IsInfinity(value)
                )
                    throw new InvalidOperationException(
                        $"Cannot export '{characterId}': invalid body {name}."
                    );
                element.SetAttributeValue(name, value);
            }
            return element;
        }

        private static void AddFaceTags(XElement face, string kind, string tags)
        {
            var names = (tags ?? string.Empty)
                .Split(',')
                .Select(tag => tag.Trim())
                .Where(tag => tag.Length > 0)
                .ToArray();
            if (names.Length > 0)
                face.Add(
                    new XElement(
                        kind + "_tags",
                        names.Select(name => new XElement(kind, new XAttribute("name", name)))
                    )
                );
        }

        /// <summary>
        /// Builds the <skills/> XML element listing this character's skills and values.
        /// </summary>
        private static XElement BuildSkillsElement(WCharacter character)
        {
            var skills = new XElement("skills");

            foreach (var (skill, value) in character.Skills)
            {
                skills.Add(
                    new XElement(
                        "skill",
                        new XAttribute("id", skill.StringId),
                        new XAttribute("value", value)
                    )
                );
            }

            return skills;
        }

        /// <summary>
        /// Builds the <upgrade_targets/> XML element from the CharacterObject's UpgradeTargets.
        /// </summary>
        private static XElement BuildUpgradeTargetsElement(CharacterObject c)
        {
            var root = new XElement("upgrade_targets");

            var targetsObj = Reflection.GetPropertyValue<object>(c, "UpgradeTargets");
            if (targetsObj is CharacterObject[] targets && targets.Length > 0)
            {
                foreach (var t in targets)
                {
                    if (t == null)
                        continue;

                    root.Add(
                        new XElement(
                            "upgrade_target",
                            new XAttribute("id", "NPCCharacter." + t.StringId)
                        )
                    );
                }
            }

            return root;
        }

        /// <summary>
        /// Builds the <Equipments/> XML element containing battle rosters and mount entries.
        /// </summary>
        private static XElement BuildEquipmentsElement(WCharacter character, CharacterObject c)
        {
            var eqs = new XElement("Equipments");

            var battle = new List<Equipment>();
            var sawCivilian = false;

            ItemObject horse = null;
            ItemObject harness = null;

            foreach (var e in EnumerateEquipmentSets(c))
            {
                if (e == null || IsEquipmentEmpty(e))
                    continue;

                var isCiv = Reflection.GetPropertyValue<bool>(e, "IsCivilian");
                if (isCiv)
                {
                    sawCivilian = true;
                    continue;
                }

                if (horse == null)
                    horse = e[EquipmentIndex.Horse].Item;
                if (harness == null)
                    harness = e[EquipmentIndex.HorseHarness].Item;

                battle.Add(e);
            }

            for (int i = 0; i < battle.Count; i++)
                eqs.Add(BuildBattleEquipmentRosterElement(battle[i]));

            if (sawCivilian)
            {
                var cultureId = character.Culture?.Base?.StringId ?? string.Empty;
                var tier = Reflection.GetPropertyValue<int>(c, "Tier");
                if (tier <= 0)
                    tier = 1;

                var templateId = $"{cultureId}_troop_civilian_template_t{tier}";

                if (HasEquipmentRoster(templateId))
                {
                    eqs.Add(
                        new XElement(
                            "EquipmentSet",
                            new XAttribute("id", templateId),
                            new XAttribute("civilian", "true")
                        )
                    );
                }
                else
                {
                    Log.Warning(
                        $"NPC export: civilian template '{templateId}' not found, omitting civilian equipment for '{character.StringId}'."
                    );
                }
            }

            if (horse != null)
            {
                eqs.Add(
                    new XElement(
                        "equipment",
                        new XAttribute("slot", "Horse"),
                        new XAttribute("id", "Item." + horse.StringId)
                    )
                );
            }

            if (harness != null)
            {
                eqs.Add(
                    new XElement(
                        "equipment",
                        new XAttribute("slot", "HorseHarness"),
                        new XAttribute("id", "Item." + harness.StringId)
                    )
                );
            }

            return eqs;
        }

        /// <summary>
        /// Enumerates available equipment sets for the given CharacterObject in a version-safe way.
        /// </summary>
        private static IEnumerable<Equipment> EnumerateEquipmentSets(CharacterObject c)
        {
            var all = Reflection.GetPropertyValue<object>(c, "AllEquipments");

            if (all is IEnumerable<Equipment> list)
            {
                foreach (var e in list)
                    yield return e;

                yield break;
            }

            var battle =
                Reflection.GetPropertyValue<object>(c, "BattleEquipments")
                as IEnumerable<Equipment>;
            if (battle != null)
            {
                foreach (var e in battle)
                    yield return e;
            }

            var civ =
                Reflection.GetPropertyValue<object>(c, "CivilianEquipments")
                as IEnumerable<Equipment>;
            if (civ != null)
            {
                foreach (var e in civ)
                    yield return e;
            }
        }

        /// <summary>
        /// Builds an EquipmentRoster XML element for a battle equipment set.
        /// </summary>
        private static XElement BuildBattleEquipmentRosterElement(Equipment e)
        {
            var roster = new XElement("EquipmentRoster");

            AddEquipment(roster, e, EquipmentIndex.Weapon0, "Item0");
            AddEquipment(roster, e, EquipmentIndex.Weapon1, "Item1");
            AddEquipment(roster, e, EquipmentIndex.Weapon2, "Item2");
            AddEquipment(roster, e, EquipmentIndex.Weapon3, "Item3");
            AddEquipment(roster, e, EquipmentIndex.ExtraWeaponSlot, "Item4");

            AddEquipment(roster, e, EquipmentIndex.Head, "Head");
            AddEquipment(roster, e, EquipmentIndex.Body, "Body");
            AddEquipment(roster, e, EquipmentIndex.Leg, "Leg");
            AddEquipment(roster, e, EquipmentIndex.Gloves, "Gloves");
            AddEquipment(roster, e, EquipmentIndex.Cape, "Cape");

            return roster;
        }

        /// <summary>
        /// Checks whether an equipment roster template with the given id exists.
        /// </summary>
        private static bool HasEquipmentRoster(string rosterId)
        {
            if (string.IsNullOrWhiteSpace(rosterId))
                return false;

            try
            {
                var t =
                    Type.GetType("TaleWorlds.Core.MBEquipmentRoster, TaleWorlds.Core")
                    ?? Type.GetType("TaleWorlds.Core.EquipmentRoster, TaleWorlds.Core");

                if (t == null)
                    return false;

                var mi = typeof(MBObjectManager)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "GetObject"
                        && m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(string)
                    );

                if (mi == null)
                    return false;

                var g = mi.MakeGenericMethod(t);
                var obj = g.Invoke(MBObjectManager.Instance, [rosterId]);

                return obj != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a single equipment XML node for the given slot to the roster element.
        /// </summary>
        private static void AddEquipment(
            XElement roster,
            Equipment e,
            EquipmentIndex idx,
            string slotName
        )
        {
            var item = e[idx].Item;
            if (item == null)
                return;

            roster.Add(
                new XElement(
                    "equipment",
                    new XAttribute("slot", slotName),
                    new XAttribute("id", "Item." + item.StringId)
                )
            );
        }

        /// <summary>
        /// Returns true if the equipment set contains no items.
        /// </summary>
        private static bool IsEquipmentEmpty(Equipment e)
        {
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var item = e[(EquipmentIndex)i].Item;
                if (item != null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Safely retrieves a string id from an MBObject-like object.
        /// </summary>
        private static string GetStringId(object obj)
        {
            if (obj == null)
                return string.Empty;

            var s = Reflection.GetPropertyValue<string>(obj, "StringId");
            return s ?? string.Empty;
        }
    }
}
