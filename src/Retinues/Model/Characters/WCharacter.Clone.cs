using System;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Clone                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clone this character into a free custom stub.
        /// </summary>
        public WCharacter Clone(bool skills = true, bool equipments = true)
        {
            var stub = GetFreeStub();
            if (stub == null)
            {
                Log.Warn("No free stub available");
                return null;
            }

            try
            {
                CopyIntoStubCore(Base, stub.Base);
            }
            catch (Exception ex)
            {
                Log.Error($"Clone core copy failed: {ex}");
            }

            // Break the shared roster created by FillFrom
            DetachEquipmentRoster(stub);

            // Copy main persisted scalars through attributes so they survive reload
            stub.Name = Name;
            stub.Level = Level;
            stub.Culture = Culture;
            stub.IsFemale = IsFemale;
            stub.Race = Race;
            stub.Age = Age;
            stub.SkillPoints = SkillPoints;

            // Always ignore upgrades
            stub.UpgradeTargets = [];

            // Body: serialize from source, apply to stub (also breaks shared refs)
            try
            {
                var body = BodySerializedAttribute.Get();
                stub.BodySerializedAttribute.Set(body);
            }
            catch (Exception ex)
            {
                Log.Warn($"Clone body copy failed: {ex}");
            }

            // Detach skills container, then apply skill values
            DetachSkillsContainer(stub);

            foreach (
                var skill in Helpers.Skills.GetSkillListForCharacter(
                    stub.IsHero,
                    includeModded: true
                )
            )
            {
                if (skill == null)
                    continue;

                var value = skills ? Skills.Get(skill) : 0;
                stub.Skills.Set(skill, value);
            }

            // Equipment: either deep copy all sets, or create 2 empty sets (battle+civilian)
            if (equipments)
                stub.EquipmentRoster.Copy(EquipmentRoster);
            else
                stub.EquipmentRoster.Reset();

            Log.Info($"Cloned '{Name}' -> '{stub.StringId}'");
            return stub;
        }

        private static void CopyIntoStubCore(CharacterObject src, CharacterObject tgt)
        {
            if (src == null || tgt == null)
                return;

            // Replicate old helper behavior: seed important private fields, then FillFrom
            try
            {
                var origin = Reflection.GetFieldValue<CharacterObject>(src, "_originCharacter");
                Reflection.SetFieldValue(tgt, "_originCharacter", origin ?? src);
            }
            catch { }

            try
            {
                Reflection.SetFieldValue(
                    tgt,
                    "_occupation",
                    Reflection.GetFieldValue<object>(src, "_occupation")
                );
            }
            catch { }

            try
            {
                Reflection.SetFieldValue(
                    tgt,
                    "_persona",
                    Reflection.GetFieldValue<object>(src, "_persona")
                );
            }
            catch { }

            try
            {
                var traits = Reflection.GetFieldValue<object>(src, "_characterTraits");
                if (traits != null)
                {
                    // Try copy-ctor first, else fallback to reference assignment
                    object cloned = null;
                    try
                    {
                        cloned = Activator.CreateInstance(traits.GetType(), traits);
                    }
                    catch { }
                    Reflection.SetFieldValue(tgt, "_characterTraits", cloned ?? traits);
                }
            }
            catch { }

            try
            {
                Reflection.SetFieldValue(
                    tgt,
                    "_civilianEquipmentTemplate",
                    Reflection.GetFieldValue<object>(src, "_civilianEquipmentTemplate")
                );
            }
            catch { }

            try
            {
                Reflection.SetFieldValue(
                    tgt,
                    "_battleEquipmentTemplate",
                    Reflection.GetFieldValue<object>(src, "_battleEquipmentTemplate")
                );
            }
            catch { }

            try
            {
                Reflection.InvokeMethod(tgt, "FillFrom", [typeof(CharacterObject)], src);
            }
            catch { }
        }

        private static void DetachSkillsContainer(WCharacter wc)
        {
            if (wc == null)
                return;

            try
            {
                var fresh = (MBCharacterSkills)
                    Activator.CreateInstance(typeof(MBCharacterSkills), true);
                Reflection.SetFieldValue(wc.Base, "DefaultCharacterSkills", fresh);
            }
            catch { }

            // Ensure skills wrapper rebuilds its attribute map if it already existed
            wc._skills = null;
        }

        private static void DetachEquipmentRoster(WCharacter wc)
        {
            if (wc == null)
                return;

            try
            {
                // FillFrom assigns _equipmentRoster by reference, so we must break it.
                var fresh = new MBEquipmentRoster();
                Reflection.SetFieldValue(wc.Base, "_equipmentRoster", fresh);
            }
            catch (Exception ex)
            {
                Log.Warn($"Detach equipment roster failed: {ex}");
            }
        }
    }
}
