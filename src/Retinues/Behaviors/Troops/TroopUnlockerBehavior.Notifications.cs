using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor;
using Retinues.Interface.Services;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Partial class for troop unlock notification utilities.
    /// </summary>
    public sealed partial class TroopUnlockerBehavior
    {
        /// <summary>
        /// Shows a UI popup announcing unlocked faction troops and offers to open the editor.
        /// </summary>
        private static void ShowUnlockPopup(
            IBaseFaction faction,
            WCharacter select,
            int count,
            bool addBannerHint = false
        )
        {
            var factionType = L.T("faction_type_faction", "faction");

            if (faction is WKingdom)
                factionType = L.T("faction_type_kingdom", "kingdom");
            else if (faction is WClan)
                factionType = L.T("faction_type_clan", "clan");

            var title = L.T("faction_troops_unlocked_title", "{FACTION} Troops")
                .SetTextVariable("FACTION", faction.Name);

            var desc = L.T(
                    "faction_troops_unlocked_desc",
                    "{COUNT} new troops are now available for the {FACTION} {FACTION_TYPE}."
                )
                .SetTextVariable("COUNT", count)
                .SetTextVariable("FACTION", faction.Name)
                .SetTextVariable("FACTION_TYPE", factionType);

            if (addBannerHint)
            {
                var hint = L.S(
                    "faction_troops_unlocked_banner_hint",
                    "\n\nUse the banner icons at the top of the Troops screen to select which troop trees to edit."
                );
                desc = new TextObject(desc.ToString() + hint);
            }

            var go = L.T("go_to_editor", "Go to editor");
            var ok = GameTexts.FindText("str_ok");

            Inquiries.Popup(
                title,
                onChoice1: () =>
                    EditorLauncher.Launch(
                        EditorLaunchArgs.Player(faction: faction, character: select)
                    ),
                onChoice2: () => { },
                choice1Text: go,
                choice2Text: ok,
                description: desc,
                pauseGame: true,
                delayUntilOnWorldMap: false
            );
        }
    }
}
