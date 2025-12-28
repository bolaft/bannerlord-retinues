using System.Collections.Generic;
using System.Xml.Linq;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Model.Characters
{
    public partial class WCharacter
    {
        /// <summary>
        /// Exports this character as a single NPCCharacter XML element.
        /// </summary>
        public string ExportAsNPC()
        {
            var el = BuildNpcCharacterElement();
            return el.ToString(SaveOptions.DisableFormatting);
        }

        private XElement BuildNpcCharacterElement()
        {
            var c = Base;

            var npc = new XElement("NPCCharacter");

            npc.SetAttributeValue("id", StringId);

            // Default group: Infantry/Ranged/Cavalry/HorseArcher (FormationClass.ToString()).
            npc.SetAttributeValue("default_group", FormationClass.ToString());

            npc.SetAttributeValue("level", Level);

            // Name: export as plain string.
            npc.SetAttributeValue("name", Name ?? StringId);

            // Occupation is read-only on CharacterObject; reflection keeps this version-safe.
            var occ = Reflection.GetPropertyValue<object>(c, "Occupation");
            if (occ != null)
                npc.SetAttributeValue("occupation", occ.ToString());

            npc.SetAttributeValue("is_basic_troop", IsBasic ? "true" : "false");

            if (Culture?.Base != null)
                npc.SetAttributeValue("culture", "Culture." + Culture.Base.StringId);

            var req = Reflection.GetPropertyValue<object>(c, "UpgradeRequiresItemFromCategory");
            var reqId = GetStringId(req);
            if (!string.IsNullOrWhiteSpace(reqId))
                npc.SetAttributeValue("upgrade_requires", "ItemCategory." + reqId);

            var face = BuildFaceElement(c);
            if (face != null)
                npc.Add(face);

            npc.Add(BuildSkillsElement());
            npc.Add(BuildUpgradeTargetsElement(c));
            npc.Add(BuildEquipmentsElement(c));

            return npc;
        }

        private XElement BuildFaceElement(CharacterObject c)
        {
            // Prefer BodyPropertyRange.StringId when available.
            var range = Reflection.GetPropertyValue<object>(c, "BodyPropertyRange");
            var rangeId = GetStringId(range);
            if (string.IsNullOrWhiteSpace(rangeId))
                return null;

            return new XElement(
                "face",
                new XElement(
                    "face_key_template",
                    new XAttribute("value", "BodyProperty." + rangeId)
                )
            );
        }

        private XElement BuildSkillsElement()
        {
            var skills = new XElement("skills");

            // Use your helper list so this includes modded skills too.
            foreach (
                var skill in Helpers.Skills.GetSkillListForCharacter(IsHero, includeModded: true)
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

            foreach (var roster in EnumerateEquipmentRosters(c))
                eqs.Add(roster);

            return eqs;
        }

        private IEnumerable<XElement> EnumerateEquipmentRosters(CharacterObject c)
        {
            // Prefer AllEquipments when available.
            var all = Reflection.GetPropertyValue<object>(c, "AllEquipments");

            // AllEquipments is usually IEnumerable<Equipment>.
            if (all is IEnumerable<Equipment> list)
            {
                foreach (var e in list)
                {
                    if (e == null || IsEquipmentEmpty(e))
                        continue;

                    yield return BuildEquipmentRosterElement(e);
                }

                yield break;
            }

            // Fallback: try BattleEquipments + CivilianEquipments if present (older patterns).
            var battle =
                Reflection.GetPropertyValue<object>(c, "BattleEquipments")
                as IEnumerable<Equipment>;
            if (battle != null)
            {
                foreach (var e in battle)
                {
                    if (e == null || IsEquipmentEmpty(e))
                        continue;

                    yield return BuildEquipmentRosterElement(e);
                }
            }

            var civ =
                Reflection.GetPropertyValue<object>(c, "CivilianEquipments")
                as IEnumerable<Equipment>;
            if (civ != null)
            {
                foreach (var e in civ)
                {
                    if (e == null || IsEquipmentEmpty(e))
                        continue;

                    yield return BuildEquipmentRosterElement(e);
                }
            }
        }

        private XElement BuildEquipmentRosterElement(Equipment e)
        {
            var roster = new XElement("EquipmentRoster");

            // Some versions expose Equipment.IsCivilian.
            var isCiv = Reflection.GetPropertyValue<bool>(e, "IsCivilian");
            if (isCiv)
                roster.SetAttributeValue("civilian", "true");

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

            AddEquipment(roster, e, EquipmentIndex.Horse, "Horse");
            AddEquipment(roster, e, EquipmentIndex.HorseHarness, "HorseHarness");

            return roster;
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
