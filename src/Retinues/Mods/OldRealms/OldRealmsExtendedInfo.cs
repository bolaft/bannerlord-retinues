using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;

namespace Retinues.Mods.OldRealms
{
    /// <summary>
    /// Compatibility with The Old Realms (TOR_Core).
    ///
    /// TOR stores per-troop "extended info" (Ward Save, Armor Penetration, Unbreakable,
    /// resistances, damage proportions, resource cost, etc.) in a registry keyed by
    /// CharacterObject StringId, loaded from tor_extendedunitproperties.xml. Retinues custom
    /// troops use generated stub ids (retinues_custom_XXXX) that TOR has no entry for, so they
    /// lose those attributes in battle.
    ///
    /// Each custom troop is cloned from a template (its VanillaStringId), which — for a TOR-based
    /// troop — has an entry. We copy the template's entry onto the custom id at runtime: the
    /// automatic equivalent of the community's manual edit to tor_extendedunitproperties.xml.
    /// All reflection here is best-effort and no-ops when TOR is not installed.
    /// </summary>
    [SafeClass]
    internal static class OldRealmsExtendedInfo
    {
        private const string ManagerTypeName =
            "TOR_Core.Extensions.ExtendedInfoSystem.ExtendedInfoManager";
        private const string InfoTypeName =
            "TOR_Core.Extensions.ExtendedInfoSystem.CharacterExtendedInfo";

        private static Type _managerType;
        private static Type _infoType;
        private static FieldInfo _characterInfosField;
        private static FieldInfo _idField;
        private static bool _resolveFailed;

        private static bool Resolve()
        {
            if (_resolveFailed)
                return false;
            if (_characterInfosField != null && _idField != null)
                return true;

            _managerType ??= AccessTools.TypeByName(ManagerTypeName);
            _infoType ??= AccessTools.TypeByName(InfoTypeName);

            if (_managerType == null || _infoType == null)
            {
                _resolveFailed = true;
                return false;
            }

            _characterInfosField ??= _managerType.GetField(
                "_characterInfos",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            _idField ??= _infoType.GetField(
                "CharacterStringId",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (_characterInfosField == null || _idField == null)
            {
                _resolveFailed = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copies template TOR extended info onto every active custom troop id. Safe to call
        /// repeatedly (it overwrites, so re-based troops and TOR registry reloads stay in sync).
        /// </summary>
        public static void Sync()
        {
            if (!ModCompatibility.HasOldRealms || !Resolve())
                return;

            if (_characterInfosField.GetValue(null) is not IDictionary dict)
                return;

            foreach (var id in WCharacter.ActiveStubIds.ToArray())
            {
                try
                {
                    var troop = WCharacter.FromStringId(id);
                    if (troop?.IsCustom != true)
                        continue;

                    var templateId = troop.VanillaStringId;
                    if (string.IsNullOrEmpty(templateId) || templateId == id)
                        continue;

                    // Only sync troops whose template actually has TOR extended info.
                    if (!dict.Contains(templateId) || dict[templateId] is not object template)
                        continue;

                    dict[id] = CloneFor(template, id);
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"OldRealms: failed to sync extended info for '{id}'.");
                }
            }
        }

        /// <summary>
        /// Shallow-clones a CharacterExtendedInfo and stamps it with the custom troop id. The
        /// inner lists (abilities/attributes/resistances) are loaded-from-XML and read-only, so
        /// sharing their references is safe.
        /// </summary>
        private static object CloneFor(object template, string customId)
        {
            var clone = Activator.CreateInstance(_infoType);

            foreach (var field in _infoType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                field.SetValue(clone, field.GetValue(template));

            _idField.SetValue(clone, customId);
            return clone;
        }
    }
}
