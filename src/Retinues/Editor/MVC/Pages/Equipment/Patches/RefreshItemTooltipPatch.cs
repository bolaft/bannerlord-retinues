using System.Linq;
using HarmonyLib;
using Retinues.Interface.Services;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Equipment.Patches
{
    /// <summary>
    /// Adds extra tooltip properties in the editor for equipment items.
    /// </summary>
    [HarmonyPatch(
        typeof(TooltipRefresherCollection),
        nameof(TooltipRefresherCollection.RefreshItemTooltip)
    )]
    public static class RefreshItemTooltip_EditorExtrasPatch
    {
        public static void Postfix(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
        {
            // Only in the editor.
            if (!EditorScreen.IsOpen)
                return;

            if (propertyBasedTooltipVM == null || args == null || args.Length == 0)
                return;

            var ee = args[0] as EquipmentElement?;
            if (ee == null)
                return;

            var item = ee.Value.Item;
            if (item == null)
                return;

            var list = propertyBasedTooltipVM.TooltipPropertyList;
            if (list == null)
                return;

            bool HasDef(string def) => list.Any(p => p != null && p.DefinitionLabel == def);

            // Culture (vanilla only shows in dev mode, and uses "Culture: ").
            var cultureDef = L.S("tooltip_culture", "Culture");
            if (!HasDef(cultureDef) && !HasDef("Culture: "))
            {
                var cultureVal =
                    item.Culture?.Name?.ToString() ?? L.S("tooltip_culture_none", "None");
                propertyBasedTooltipVM.AddProperty(cultureDef, cultureVal);
            }

            // Weight (force-show in editor even when not in dev mode).
            // Vanilla label is localized with {=4Dd2xgPm}Weight.
            var weightDef = new TextObject("{=4Dd2xgPm}Weight").ToString();
            if (!HasDef(weightDef))
            {
                // Match vanilla-ish formatting but a bit cleaner (2 decimals).
                var weightVal = MathF.Round(item.Weight, 2).ToString();
                propertyBasedTooltipVM.AddProperty(weightDef, weightVal);
            }
        }
    }
}
