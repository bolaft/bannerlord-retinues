using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Safety.Uninstall
{
    // Shared by V1 and V2. Deliberately not SafeClass: a failed cleanup must reach the caller.
    internal sealed class UninstallReferencePlan
    {
        internal sealed class IdentityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        private readonly HashSet<object> _seen = new(new IdentityComparer());
        private readonly List<Action> _changes = new();
        private readonly Func<CharacterObject, CharacterObject> _replacement;
        private readonly bool _remove;
        private readonly bool _includeBehaviorSnapshots;
        internal int Changes => _changes.Count;

        internal UninstallReferencePlan(Func<CharacterObject, CharacterObject> replacement, bool remove, bool includeBehaviorSnapshots = false)
        {
            _replacement = replacement;
            _remove = remove;
            _includeBehaviorSnapshots = includeBehaviorSnapshots;
        }

        internal static bool IsCustom(CharacterObject troop) =>
            troop?.StringId != null && (troop.StringId.StartsWith("retinues_custom_", StringComparison.Ordinal)
                || troop.StringId.StartsWith("ret_", StringComparison.Ordinal));

        private CharacterObject Replacement(CharacterObject troop)
        {
            if (troop.IsHero)
                throw new InvalidOperationException($"Cannot remove custom hero '{troop.StringId}'.");
            var replacement = _replacement(troop);
            if (replacement == null || IsCustom(replacement) || replacement.IsHero)
                throw new InvalidOperationException($"No non-Retinues replacement for '{troop.StringId}'.");
            return replacement;
        }

        internal void Apply()
        {
            foreach (var change in _changes)
                change();
        }

        internal object Visit(object value, string path)
        {
            if (value == null) return null;
            if (value is CharacterObject custom && IsCustom(custom)) return Replacement(custom);
            var type = value.GetType();
            if (_includeBehaviorSnapshots && type.Assembly == typeof(UninstallReferencePlan).Assembly)
                throw new InvalidOperationException("Saved data still requires Retinues: " + path + " (" + type.FullName + ")");
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal
                || value is DateTime || value is TimeSpan || value is Type || value is Delegate)
                return value;
            // Object registries contain XML definitions, not usages of those definitions.
            if (value is MBObjectManager) return value;
            // This is the previous save's snapshot, not live campaign state. It is rebuilt
            // by OnBeforeSave; the persistence guard excludes Retinues records then.
            if (!_includeBehaviorSnapshots && value is TaleWorlds.CampaignSystem.CampaignBehaviors.CampaignBehaviorManager) return value;
            if (!type.IsValueType && !_seen.Add(value)) return value;
            if (_seen.Count > 1000000) throw new InvalidOperationException("Uninstall scan exceeded its object limit.");

            if (value is TroopRoster roster) { PlanRoster(roster); return value; }
            if (value is TaleWorlds.CampaignSystem.Party.PartyTemplateStack stack)
            {
                if (!IsCustom(stack.Character)) return value;
                stack.Character = Replacement(stack.Character);
                return stack;
            }
            if (value is IDictionary dictionary) { PlanDictionary(dictionary, path); return value; }
            if (value is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    int index = i;
                    var old = list[i];
                    var next = Visit(old, path + "[" + i + "]");
                    if (Changed(old, next))
                    {
                        if (list.IsReadOnly) throw new InvalidOperationException("Read-only troop list: " + path);
                        _changes.Add(() => list[index] = next);
                    }
                }
                return value;
            }
            if (value is IEnumerable enumerable)
            {
                foreach (var entry in enumerable)
                    if (Changed(entry, Visit(entry, path + "[]")))
                        throw new InvalidOperationException("Unsupported troop collection: " + path);
                return value;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                foreach (string name in new[] { "Key", "Value" })
                {
                    var child = type.GetProperty(name).GetValue(value, null);
                    if (Changed(child, Visit(child, path + "." + name)))
                        throw new InvalidOperationException("Unsupported read-only dictionary: " + path);
                }
                return value;
            }

            // Structs must be written back to their owning field/list after their boxed copy changes.
            object target = type.IsValueType ? RuntimeHelpers.GetObjectValue(value) : value;
            int before = Changes;
            bool tuple = type.IsGenericType && (type.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
                || type.FullName.StartsWith("System.Tuple`", StringComparison.Ordinal));
            foreach (var field in Fields(type))
                if (tuple || field.IsDefined(typeof(SaveableFieldAttribute), false)) PlanField(target, field, path);
            foreach (var property in Properties(type))
            {
                if (!property.IsDefined(typeof(SaveablePropertyAttribute), false)) continue;
                var old = property.GetValue(target, null);
                var next = Visit(old, path + "." + property.Name);
                if (!Changed(old, next)) continue;
                var setter = property.GetSetMethod(true);
                if (setter == null) throw new InvalidOperationException("Read-only saved troop property: " + path + "." + property.Name);
                _changes.Add(() => setter.Invoke(target, new[] { next }));
            }
            return type.IsValueType && Changes != before ? target : value;
        }

        private static bool Changed(object old, object next) => !ReferenceEquals(old, next);

        // Campaign behaviors declare persistence in SyncData rather than Saveable attributes.
        // Inspect their instance fields as roots, then traverse only saved members/containers.
        // This avoids executing arbitrary SyncData callbacks outside an actual save.
        internal void VisitBehavior(object behavior)
        {
            foreach (var field in Fields(behavior.GetType()))
            {
                // These native records are keyed to a troop definition, not an individual.
                // Discard only the removed definitions' cooldown/training records. Applying
                // them to a replacement could overwrite that troop's existing records.
                string name = field.DeclaringType.FullName + "." + field.Name;
                bool disposable = field.DeclaringType.Assembly == typeof(Campaign).Assembly &&
                    (name == "TaleWorlds.CampaignSystem.CampaignBehaviors.PrisonerRecruitCampaignBehavior.PrisonerTalkRecords"
                     || name == "TaleWorlds.CampaignSystem.CampaignBehaviors.RetrainOutlawPartyMembersBehavior._retrainTable"
                     || name == "TaleWorlds.CampaignSystem.CampaignBehaviors.ViewDataTrackerCampaignBehavior._examinedPrisonerCharacterList");
                if (disposable && field.GetValue(behavior) is IDictionary records)
                {
                    if (_seen.Add(records)) PlanDictionary(records, name, dropCustomKeys: true);
                }
                else PlanField(behavior, field, behavior.GetType().FullName);
            }
        }

        internal void PlanUpgradeTargets(CharacterObject troop)
        {
            var targets = troop.UpgradeTargets;
            if (targets == null || !targets.Any(IsCustom)) return;
            var kept = targets.Where(t => !IsCustom(t)).ToArray();
            var setter = typeof(CharacterObject).GetProperty(nameof(CharacterObject.UpgradeTargets)).GetSetMethod(true);
            if (setter == null) throw new InvalidOperationException("Cannot detach custom troop upgrade targets.");
            _changes.Add(() => setter.Invoke(troop, new object[] { kept }));
        }

        private void PlanField(object owner, FieldInfo field, string path)
        {
            var old = field.GetValue(owner);
            var next = Visit(old, path + "." + field.Name);
            if (Changed(old, next)) _changes.Add(() => field.SetValue(owner, next));
        }

        internal static IEnumerable<FieldInfo> Fields(Type type)
        {
            for (; type != null; type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    yield return field;
        }

        private static IEnumerable<PropertyInfo> Properties(Type type)
        {
            for (; type != null; type = type.BaseType)
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    yield return property;
        }

        private void PlanDictionary(IDictionary dictionary, string path, bool dropCustomKeys = false)
        {
            var entries = new List<DictionaryEntry>();
            var keys = new HashSet<object>();
            bool changed = false;
            foreach (DictionaryEntry entry in dictionary)
            {
                // Preferences for deleted definitions have no meaning; keep existing native preferences.
                if (entry.Key is CharacterObject troop && IsCustom(troop) && (dropCustomKeys || entry.Value is TaleWorlds.Core.FormationClass))
                { changed = true; continue; }
                var key = Visit(entry.Key, path + ".key");
                var item = Visit(entry.Value, path + ".value");
                if (!keys.Add(key)) throw new InvalidOperationException("Replacement would merge conflicting dictionary keys: " + path);
                entries.Add(new DictionaryEntry(key, item));
                changed |= Changed(entry.Key, key) || Changed(entry.Value, item);
            }
            if (!changed) return;
            if (dictionary.IsReadOnly || dictionary.IsFixedSize) throw new InvalidOperationException("Read-only troop dictionary: " + path);
            _changes.Add(() =>
            {
                dictionary.Clear();
                foreach (var entry in entries) dictionary.Add(entry.Key, entry.Value);
            });
        }

        private void PlanRoster(TroopRoster roster)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var dataField = typeof(TroopRoster).GetField("data", flags);
            var countField = typeof(TroopRoster).GetField("_count", flags);
            var refresh = typeof(TroopRoster).GetMethod("InitializeCachedData", flags);
            if (dataField == null || countField == null || refresh == null)
                throw new InvalidOperationException("Unsupported troop roster layout.");
            var original = (TroopRosterElement[])dataField.GetValue(roster);
            int count = roster.Count;
            if (original == null || count < 0 || count > original.Length)
                throw new InvalidOperationException("Invalid roster storage; reload the backup before uninstalling.");
            if (!original.Take(count).Any(e => IsCustom(e.Character))) return;

            var rows = new List<TroopRosterElement>();
            foreach (var saved in original.Take(count))
            {
                var row = saved;
                if (IsCustom(row.Character))
                {
                    if (row.Character.IsHero) throw new InvalidOperationException("Cannot purge a custom hero.");
                    if (_remove) continue;
                    row.Character = Replacement(row.Character);
                }
                if (row.Character == null) throw new InvalidOperationException("Roster contains a missing troop.");
                int index = rows.FindIndex(e => ReferenceEquals(e.Character, row.Character));
                if (index < 0) rows.Add(row);
                else
                {
                    var merged = rows[index];
                    merged.Number = checked(merged.Number + row.Number);
                    merged.WoundedNumber = checked(merged.WoundedNumber + row.WoundedNumber);
                    merged.Xp = checked(merged.Xp + row.Xp);
                    rows[index] = merged;
                }
            }
            var replacement = new TroopRosterElement[Math.Max(original.Length, rows.Count)];
            rows.CopyTo(replacement);
            _changes.Add(() =>
            {
                // No XP/training/recruitment callbacks during cleanup. They can recreate custom troops.
                dataField.SetValue(roster, replacement);
                countField.SetValue(roster, rows.Count);
                refresh.Invoke(roster, null);
                roster.UpdateVersion();
            });
        }
    }
}
