using System.Collections.Generic;
using Retinues.Model.Equipments;
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
                    ListSortKey.Category,
                    L.S("sort_by_category", "Category"),
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

        protected override void BuildSections(ListVM list)
        {
            // Build headers and rows off-screen first.
            var headers = GetHeaders(list);

            // Single binding update instead of N inserts.
            list.SetHeaders(headers);
        }

        /// <summary>
        /// Builds the sections for the equipment list.
        /// </summary>
        private List<ListHeaderVM> GetHeaders(ListVM list)
        {
            var headers = new List<ListHeaderVM>();

            if (
                State.Instance.Slot
                is EquipmentIndex.Weapon0
                    or EquipmentIndex.Weapon1
                    or EquipmentIndex.Weapon2
                    or EquipmentIndex.Weapon3
            )
            {
                // Weapons are grouped by type.
                var types = new Dictionary<ItemObject.ItemTypeEnum, List<WItem>>();

                foreach (var item in WItem.GetEquipmentsForSlot(State.Instance.Slot))
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
            else if (
                State.Instance.Slot
                is EquipmentIndex.Head
                    or EquipmentIndex.Cape
                    or EquipmentIndex.Body
                    or EquipmentIndex.Gloves
                    or EquipmentIndex.Leg
            )
            {
                // Armor pieces are not grouped.
                headers.Add(
                    CreateHeader(
                        list,
                        State.Instance.Slot.ToString().ToLowerInvariant(),
                        Format.CamelCaseToTitle(State.Instance.Slot.ToString()),
                        WItem.GetEquipmentsForSlot(State.Instance.Slot)
                    )
                );
            }
            else
            {
                // Other equipment is grouped by category.
                var categories = new Dictionary<ItemCategory, List<WItem>>();

                foreach (var item in WItem.GetEquipmentsForSlot(State.Instance.Slot))
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

            // Sort headers alphabetically.
            headers.Sort((a, b) => a.Name.CompareTo(b.Name));

            // Return headers.
            return headers;
        }

        private EquipmentListHeaderVM CreateHeader(
            ListVM list,
            string headerId,
            string headerText,
            List<WItem> items
        )
        {
            var header = new EquipmentListHeaderVM(list, headerId, headerText);

            // Build rows into the *unbound* buffer (ListHeaderVM._rows).
            var expand = false;

            if (items != null && items.Count > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null)
                        continue;

                    var row = new EquipmentListRowVM(header, item);
                    header.Rows.Add(row);

                    // Only expand the header that contains the selected row.
                    if (!expand && row.IsSelected)
                        expand = true;
                }
            }

            header.UpdateRowCount();

            // Default: collapsed (equipment headers closed by default).
            // Exception: expand the header that contains the selected row.
            if (expand)
                header.IsExpanded = true;

            return header;
        }
    }
}
