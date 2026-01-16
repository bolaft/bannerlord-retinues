using System.Collections.Generic;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.VM.List.Equipment
{
    /// <summary>
    /// Builds the equipment list for the editor.
    /// </summary>
    public class EquipmentListBuilder : BaseListBuilder
    {
        /// <summary>
        /// Builds the sort buttons for the equipment list.
        /// </summary>
        protected override void BuildSortButtons(ListVM list)
        {
            list.SortButtons.Clear();

            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Name, L.S("sort_by_name", "Name"), 2)
            );
            list.SortButtons.Add(
                new ListSortButtonVM(
                    list,
                    ListSortKey.Culture,
                    L.S("sort_by_culture", "Culture"),
                    2
                )
            );
            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Tier, L.S("sort_by_tier", "Tier"), 1)
            );
            list.SortButtons.Add(
                new ListSortButtonVM(list, ListSortKey.Value, L.S("sort_by_value", "Value"), 1)
            );

            list.RecomputeSortButtonProperties();
        }

        /// <summary>
        /// Builds the sections for the equipment list.
        /// </summary>
        protected override void BuildSections(ListVM list)
        {
            var headers = new List<ListHeaderVM>();

            if (
                EditorState.Instance.Slot
                is EquipmentIndex.Weapon0
                    or EquipmentIndex.Weapon1
                    or EquipmentIndex.Weapon2
                    or EquipmentIndex.Weapon3
            )
            {
                // Weapons are grouped by type.
                var types = new Dictionary<ItemObject.ItemTypeEnum, List<WItem>>();

                foreach (var item in WItem.GetEquipmentsForSlot(EditorState.Instance.Slot))
                {
                    var type = item.Type;
                    if (!types.ContainsKey(type))
                        types[type] = [];
                    types[type].Add(item);
                }

                foreach (var kvp in types)
                {
                    headers.Add(
                        CreateHeader(
                            list,
                            kvp.Key.ToString().ToLowerInvariant(),
                            Format.CamelCaseToTitle(kvp.Key.ToString()),
                            kvp.Value
                        )
                    );
                }
            }
            else if (EditorState.Instance.Slot == EquipmentIndex.Horse)
            {
                // Horses are grouped by category.
                var categories = new Dictionary<ItemCategory, List<WItem>>();

                foreach (var item in WItem.GetEquipmentsForSlot(EditorState.Instance.Slot))
                {
                    var category = item.Category;
                    if (!categories.ContainsKey(category))
                        categories[category] = [];
                    categories[category].Add(item);
                }

                foreach (var kvp in categories)
                {
                    headers.Add(
                        CreateHeader(
                            list,
                            kvp.Key.ToString().ToLowerInvariant(),
                            Format.CamelCaseToTitle(kvp.Key.ToString()),
                            kvp.Value
                        )
                    );
                }
            }
            else
            {
                // Armor pieces are not grouped.
                headers.Add(
                    CreateHeader(
                        list,
                        EditorState.Instance.Slot.ToString().ToLowerInvariant(),
                        Format.CamelCaseToTitle(EditorState.Instance.Slot.ToString()),
                        WItem.GetEquipmentsForSlot(EditorState.Instance.Slot)
                    )
                );
            }

            // Sort headers alphabetically.
            headers.Sort((a, b) => a.Name.CompareTo(b.Name));

            // Single binding update instead of N inserts.
            list.SetHeaders(headers);
        }

        /// <summary>
        /// Creates an equipment header with rows.
        /// </summary>
        private EquipmentListHeaderVM CreateHeader(
            ListVM list,
            string headerId,
            string headerText,
            List<WItem> items
        )
        {
            var header = new EquipmentListHeaderVM(list, headerId, headerText);

            bool isPlayerMode = EditorState.Instance.Mode == EditorMode.Player;
            bool includeCrafted = EditorState.Instance.ShowCrafted;

            if (items != null && items.Count > 0)
            {
                var normal = new List<WItem>();
                var unlocking = new List<WItem>();

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null)
                        continue;

                    // Skip crafted items if the filter is off.
                    if (!includeCrafted && item.IsCrafted)
                        continue;

                    // Player mode:
                    // - Fully locked (0%) items stay hidden.
                    // - Partially unlocked (1-99%) items are included, but should appear at the end.
                    if (isPlayerMode && !item.IsCrafted && !item.IsUnlocked)
                    {
                        if (item.UnlockProgress <= 0)
                            continue;

                        unlocking.Add(item);
                        continue;
                    }

                    normal.Add(item);
                }

                // Default view (no sort active): keep current order for normal items,
                // and append "unlocking" items at end by progress desc (higher first).
                unlocking.Sort(
                    (a, b) =>
                    {
                        int pa = a?.UnlockProgress ?? 0;
                        int pb = b?.UnlockProgress ?? 0;
                        int cmp = pb.CompareTo(pa); // desc
                        if (cmp != 0)
                            return cmp;

                        // deterministic tie-break
                        return string.Compare(
                            a?.Name,
                            b?.Name,
                            System.StringComparison.OrdinalIgnoreCase
                        );
                    }
                );

                for (int i = 0; i < normal.Count; i++)
                    header.AddRow(new EquipmentListRowVM(header, normal[i]));

                for (int i = 0; i < unlocking.Count; i++)
                    header.AddRow(new EquipmentListRowVM(header, unlocking[i]));
            }

            header.UpdateRowCount();
            return header;
        }
    }
}
