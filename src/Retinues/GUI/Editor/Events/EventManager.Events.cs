using System.Collections.Generic;

namespace Retinues.GUI.Editor.Events
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                       Events Enum                      //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Types of UI events emitted by the editor.
    /// </summary>
    public enum UIEvent
    {
        /* ━━━━━━━━ General ━━━━━━━ */

        Page,

        /* ━━━━━━━ Character ━━━━━━ */

        Faction,
        Character,
        Name,
        Culture,
        Appearance,
        Skill,
        Gender,
        Trait,
        Tree,
        Formation,

        /* ━━━━━━━ Equipment ━━━━━━ */

        Equipment,
        Slot,
        Item,
        BattleType,
        Preview,
        Crafted,
        Clipboard,

        /* ━━━━━━━ Doctrines ━━━━━━ */

        Doctrine,

        /* ━━━━━━━━ Library ━━━━━━━ */

        Library,
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                      Event Hierarchy                   //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Manages the UI event hierarchy and related utilities for the editor.
    /// </summary>
    public static partial class EventManager
    {
        /// <summary>
        /// Declarative parent -> children relationships between events.
        /// Firing a parent will also fire all of its descendants once
        /// in the current burst.
        /// </summary>
        private static readonly Dictionary<UIEvent, UIEvent[]> _hierarchy = new()
        {
            [UIEvent.Faction] = [UIEvent.Character, UIEvent.Tree],

            [UIEvent.Character] =
            [
                UIEvent.Name,
                UIEvent.Culture,
                UIEvent.Skill,
                UIEvent.Gender,
                UIEvent.Equipment,
                UIEvent.Trait,
            ],

            [UIEvent.Equipment] = [UIEvent.Item, UIEvent.BattleType],

            [UIEvent.Slot] = [UIEvent.Item],

            [UIEvent.Item] = [UIEvent.Appearance, UIEvent.Formation],

            [UIEvent.Culture] = [UIEvent.Appearance],

            [UIEvent.Gender] = [UIEvent.Appearance],

            [UIEvent.Preview] = [UIEvent.Appearance],
        };

        /// <summary>
        /// Expanded node returned by ExpandWithParent.
        /// </summary>
        private readonly struct Expanded(UIEvent current, UIEvent parent)
        {
            public readonly UIEvent Current = current;
            public readonly UIEvent Parent = parent;
        }

        /// <summary>
        /// Expands a root event into itself + all transitive children,
        /// preserving declared order, with cycle protection and dedup.
        /// Also returns the direct parent of each expanded node.
        /// </summary>
        private static IEnumerable<Expanded> ExpandWithParent(UIEvent root)
        {
            var visited = new HashSet<UIEvent>();
            var stack = new Stack<Expanded>();

            // Root has itself as parent.
            stack.Push(new Expanded(root, root));

            while (stack.Count > 0)
            {
                var exp = stack.Pop();
                var current = exp.Current;

                if (!visited.Add(current))
                    continue;

                yield return exp;

                if (_hierarchy.TryGetValue(current, out var children) && children != null)
                {
                    // Push in reverse so array order is preserved on pop.
                    for (int i = children.Length - 1; i >= 0; i--)
                        stack.Push(new Expanded(children[i], current));
                }
            }
        }
    }
}
