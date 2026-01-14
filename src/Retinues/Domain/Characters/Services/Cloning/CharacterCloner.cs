using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Services.Cloning
{
    public static class CharacterCloner
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clone a character into a free custom stub (default),
        /// or into the provided stub if specified.
        /// This is a pure structural clone (no gameplay rules).
        /// </summary>
        public static WCharacter Clone(
            WCharacter source,
            bool skills = true,
            bool equipments = true,
            WCharacter stub = null
        )
        {
            if (source?.Base == null)
                return null;

            // Use provided stub or get a free one
            stub ??= WCharacter.GetFreeStub();

            if (stub == null)
            {
                Log.Warn("No free stub available");
                return null;
            }

            try
            {
                CopyIntoStubCore(source.Base, stub.Base);
            }
            catch (Exception ex)
            {
                Log.Error($"Clone core copy failed: {ex}");
            }

            // Break the shared roster created by FillFrom
            DetachEquipmentRoster(stub);

            // Copy main persisted scalars through attributes so they survive reload
            stub.Name = source.Name;
            stub.Level = source.Level;
            stub.Culture = source.Culture;
            stub.IsFemale = source.IsFemale;
            stub.Race = source.Race;
            stub.Age = source.Age;
            stub.IsMariner = source.IsMariner;
            stub.SkillPoints = source.SkillPoints;

            // Always ignore upgrades (builder can re-wire later if needed)
            stub.UpgradeTargets = [];
            SetBaseUpgradeTargets(stub, []);

            // Body: serialize from source, apply to stub (also breaks shared refs)
            try
            {
                var body = source.SerializeBodyEnvelope();
                stub.ApplySerializedBodyEnvelope(body);
            }
            catch (Exception ex)
            {
                Log.Warn($"Clone body copy failed: {ex}");
            }

            // Detach skills container, then apply skill values
            DetachSkillsContainer(stub);

            foreach (var (skill, value) in stub.Skills)
                stub.Skills.Set(skill, value);

            // Equipment: coarse toggle only (policy handled by callers)
            if (equipments)
                stub.EquipmentRoster.Copy(source.EquipmentRoster, EquipmentCopyMode.All);
            else
                stub.EquipmentRoster.Reset();

            return stub;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Base Upgrade Wiring                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensure CharacterObject.UpgradeTargets backing field is kept in sync.
        /// FillFrom copies origin upgrade targets onto the stub's BASE object.
        /// This is structural engine detail, not gameplay logic.
        /// </summary>
        public static void SetBaseUpgradeTargets(WCharacter wc, IReadOnlyList<WCharacter> targets)
        {
            if (wc?.Base == null)
                return;

            var list = targets ?? Array.Empty<WCharacter>();
            int count = 0;

            for (int i = 0; i < list.Count; i++)
                if (list[i]?.Base != null)
                    count++;

            var arr = count == 0 ? Array.Empty<CharacterObject>() : new CharacterObject[count];

            int idx = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i]?.Base;
                if (b == null)
                    continue;

                arr[idx++] = b;
            }

            try
            {
                // Auto-property backing field in TW sources.
                Reflection.SetFieldValue(wc.Base, "<UpgradeTargets>k__BackingField", arr);
            }
            catch
            {
                // Best-effort only: if the backing field name changes, do not crash.
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Core Copy                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void CopyIntoStubCore(CharacterObject src, CharacterObject tgt)
        {
            if (src == null || tgt == null)
                return;

            /// <summary>
            /// Copy a private field from source to target via reflection.
            /// Logs a warning on failure but continues.
            /// </summary>
            void CopyField(string fieldName)
            {
                try
                {
                    var value = Reflection.GetFieldValue<object>(src, fieldName);
                    Reflection.SetFieldValue(tgt, fieldName, value);
                }
                catch
                {
                    Log.Warn($"Copy field '{fieldName}' failed");
                }
            }

            // Copy key private fields
            CopyField("_originCharacter");
            CopyField("_occupation");
            CopyField("_persona");
            CopyField("_civilianEquipmentTemplate");
            CopyField("_battleEquipmentTemplate");

            // Copy character traits (try copy ctor first)
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
                    catch
                    {
                        Log.Warn("Character traits copy ctor failed");
                    }

                    Reflection.SetFieldValue(tgt, "_characterTraits", cloned ?? traits);
                }
            }
            catch
            {
                Log.Warn("Copy character traits failed");
            }

            // Invoke FillFrom to copy the rest
            try
            {
                Reflection.InvokeMethod(tgt, "FillFrom", [typeof(CharacterObject)], src);
            }
            catch
            {
                Log.Warn("FillFrom invocation failed");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Detachers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Detach the skills container from the character.
        /// </summary>
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
            wc.ClearSkillsCache();
        }

        /// <summary>
        /// Detach the equipment roster from the character.
        /// </summary>
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
