using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        /// <summary>
        /// Exports this character as a single NPCCharacter XML element.
        /// </summary>
        public string ExportAsNPC(string overrideStringId = null)
        {
            var el = BuildNpcCharacterElement(overrideStringId);
            return el.ToString(SaveOptions.DisableFormatting);
        }

        private XElement BuildNpcCharacterElement(string overrideStringId)
        {
            var c = Base;

            var npc = new XElement("NPCCharacter");

            // Use override id when provided (so NPC export doesn't inherit stub ids).
            var finalId = !string.IsNullOrWhiteSpace(overrideStringId)
                ? overrideStringId
                : StringId;
            npc.SetAttributeValue("id", finalId);

            // Default group: Infantry/Ranged/Cavalry/HorseArcher (FormationClass.ToString()).
            npc.SetAttributeValue("default_group", FormationClass.ToString());

            // Level: always export.
            npc.SetAttributeValue("level", Level);

            // Always export is_hidden_encyclopedia to false.
            npc.SetAttributeValue("is_hidden_encyclopedia", "false");

            // Name: export as plain string.
            npc.SetAttributeValue("name", Name ?? finalId);

            // Occupation is read-only on CharacterObject; reflection keeps this version-safe.
            var occ = Reflection.GetPropertyValue<object>(c, "Occupation");
            if (occ != null)
                npc.SetAttributeValue("occupation", occ.ToString());

            // IsBasicTroop is not the same as IsBasic.
            var isBasicTroop = Reflection.GetPropertyValue<bool>(c, "IsBasicTroop");
            npc.SetAttributeValue("is_basic_troop", isBasicTroop ? "true" : "false");

            // Always export IsFemale.
            var isFemale = Reflection.GetPropertyValue<bool>(c, "IsFemale");
            npc.SetAttributeValue("is_female", isFemale ? "true" : "false");

            if (Culture?.Base != null)
                npc.SetAttributeValue("culture", "Culture." + Culture.Base.StringId);

            var req = Reflection.GetPropertyValue<object>(c, "UpgradeRequiresItemFromCategory");
            var reqId = GetStringId(req);
            if (!string.IsNullOrWhiteSpace(reqId))
                npc.SetAttributeValue("upgrade_requires", "ItemCategory." + reqId);

            var face = BuildFaceElement(c, finalId);
            if (face != null)
                npc.Add(face);

            npc.Add(BuildSkillsElement());
            npc.Add(BuildUpgradeTargetsElement(c));
            npc.Add(BuildEquipmentsElement(c));

            return npc;
        }

        private XElement BuildFaceElement(CharacterObject c, string finalId)
        {
            try
            {
                // If we're overriding an existing NPCCharacter id, BodyPropertyRange is already set
                // from vanilla, and <BodyProperties/> won't be applied. Use face_key_template instead.
                if (!string.IsNullOrWhiteSpace(finalId))
                {
                    var existing = MBObjectManager.Instance.GetObject<CharacterObject>(finalId);
                    if (existing != null)
                        return BuildFaceTemplateElement();
                }

                // New troop id: export an exact face (min=max) sampled deterministically.
                return BuildExactFaceElement(c);
            }
            catch
            {
                return null;
            }
        }

        private XElement BuildFaceTemplateElement()
        {
            // Pick a sensible culture+gender template, then export its BodyPropertyRange id.
            var culture = Culture;
            if (culture == null)
                return null;

            WCharacter template = culture.RootBasic ?? culture.RootElite;

            if (template == null || template.IsFemale != IsFemale)
                template = IsFemale ? culture.VillageWoman : culture.Villager;

            if (template == null)
            {
                foreach (var t in culture.Troops)
                {
                    if (t == null)
                        continue;

                    template = t;
                    if (t.IsFemale == IsFemale)
                        break;
                }
            }

            if (template?.Base == null)
                return null;

            var rangeObj = Reflection.GetPropertyValue<object>(template.Base, "BodyPropertyRange");
            var rangeId = GetStringId(rangeObj);
            if (string.IsNullOrWhiteSpace(rangeId))
                return null;

            var face = new XElement("face");
            face.Add(
                new XElement(
                    "face_key_template",
                    new XAttribute("value", "BodyProperty." + rangeId)
                )
            );

            return face;
        }

        private XElement BuildExactFaceElement(CharacterObject c)
        {
            // Deterministic sample: this matches the engine’s usual “seeded face per troop id” approach.
            // Use rank 0 seed.
            var seed = c.GetDefaultFaceSeed(0);

            // Use first battle equipment if available (hair cover affects the sampled face).
            Equipment eq;
            try
            {
                eq = Reflection.GetPropertyValue<Equipment>(c, "FirstBattleEquipment");
            }
            catch
            {
                eq = default;
            }

            var props = c.GetBodyProperties(eq, seed);

            var face = new XElement("face");

            var minEl = XElement.Parse(props.ToString()); // <BodyProperties ... />
            var maxEl = XElement.Parse(props.ToString());
            maxEl.Name = "BodyPropertiesMax";

            face.Add(minEl);
            face.Add(maxEl);

            return face;
        }

        private XElement BuildSkillsElement()
        {
            var skills = new XElement("skills");

            // Use helper list so this includes modded skills too.
            foreach (
                var skill in Helpers.SkillsHelper.GetSkillListForCharacter(
                    IsHero,
                    includeModded: true
                )
            )
            {
                if (skill == null)
                    continue;

                var v = Skills.Get(skill);
                if (v <= 0)
                    continue;

                skills.Add(
                    new XElement(
                        "skill",
                        new XAttribute("id", skill.StringId),
                        new XAttribute("value", v)
                    )
                );
            }

            return skills;
        }

        private XElement BuildUpgradeTargetsElement(CharacterObject c)
        {
            var root = new XElement("upgrade_targets");

            // CharacterObject.UpgradeTargets is version-stable; keep it reflection-safe anyway.
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

        private XElement BuildEquipmentsElement(CharacterObject c)
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
                    continue; // do not export civilian rosters as battle rosters
                }

                // Capture mount once (vanilla NPCCharacter XML typically places these outside rosters)
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
                var cultureId = Culture?.Base?.StringId ?? string.Empty;
                var tier = Reflection.GetPropertyValue<int>(c, "Tier");
                if (tier <= 0)
                    tier = 1;

                // Example: aserai_troop_civilian_template_t2
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
                    Log.Warn(
                        $"NPC export: civilian template '{templateId}' not found; omitting civilian equipment for '{StringId}'."
                    );
                }
            }

            // Add horse/harness outside rosters (if any)
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

        private IEnumerable<Equipment> EnumerateEquipmentSets(CharacterObject c)
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

        private XElement BuildBattleEquipmentRosterElement(Equipment e)
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

            // Do NOT add Horse/Harness here; they are emitted at top-level in <Equipments>.
            return roster;
        }

        private static bool HasEquipmentRoster(string rosterId)
        {
            if (string.IsNullOrWhiteSpace(rosterId))
                return false;

            try
            {
                // In Bannerlord, equipment set templates are MBObjects.
                // The concrete type name varies across versions/modded trees, so we resolve it safely.
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
                var obj = g.Invoke(MBObjectManager.Instance, new object[] { rosterId });

                return obj != null;
            }
            catch
            {
                return false;
            }
        }

        private void AddEquipment(XElement roster, Equipment e, EquipmentIndex idx, string slotName)
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

        private bool IsEquipmentEmpty(Equipment e)
        {
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var item = e[(EquipmentIndex)i].Item;
                if (item != null)
                    return false;
            }
            return true;
        }

        private static string GetStringId(object obj)
        {
            if (obj == null)
                return string.Empty;

            // MBObjectBase.StringId in most cases
            var s = Reflection.GetPropertyValue<string>(obj, "StringId");
            return s ?? string.Empty;
        }
    }
}
