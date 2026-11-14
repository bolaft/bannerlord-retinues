using System.Collections.Generic;
using System.Linq;
using Retinues.Editor.MVC.Pages.Library.Services;
using Retinues.Exports;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Library.Controllers
{
    public partial class LibraryController
    {
        private static void ApplyStableImportWithSelection(string path)
        {
            if (!StableExportParser.TryRead(path, out var sections, out var error))
            {
                Log.Warning($"Stable export parse failed: {error}");
                Inquiries.Popup(L.T("library_import_failed_title", "Import Failed"),
                    L.T("library_stable_import_error", "The stable export could not be fully imported.\n\n{DETAIL}")
                        .SetTextVariable("DETAIL", error ?? string.Empty));
                return;
            }

            void Select(StableExportSection section) => ChooseFactionImportTarget(new FactionExportData
            {
                StableSection = section,
            });

            if (sections.Count == 1)
            {
                Select(sections[0]);
                return;
            }

            var elements = new List<InquiryElement>();
            foreach (var section in sections)
            {
                string label = section.Key switch
                {
                    "clan" => L.S("library_stable_clan", "Clan troops"),
                    "kingdom" => L.S("library_stable_kingdom", "Kingdom troops"),
                    _ => L.T("library_stable_culture", "Culture / minor clan {NUMBER}")
                        .SetTextVariable("NUMBER", section.Key.Substring("culture:".Length)).ToString(),
                };
                var sample = section.Faction.RootBasic ?? section.Faction.RootElite ?? section.Troops.Values.First();
                label += " — " + ExportXMLReader.ResolveTextObjectString(sample.Name ?? sample.StringId);
                elements.Add(new InquiryElement(section, label, null));
            }
            Inquiries.SelectPopup(
                title: L.T("library_stable_section_title", "Choose Troops to Import"),
                description: L.T("library_stable_section_desc",
                    "This stable export contains several groups of troops. Choose one, then choose its destination. You can import the other groups separately."),
                elements: elements,
                onSelect: el => { if (el?.Identifier is StableExportSection section) Select(section); });
        }
    }
}
