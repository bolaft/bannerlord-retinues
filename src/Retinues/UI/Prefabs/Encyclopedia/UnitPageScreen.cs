using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

namespace Retinues.UI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class UnitPageScreen(EncyclopediaUnitPageVM vm)
        : BaseEncyclopediaPageScreen<EncyclopediaUnitPageVM>(vm)
    {
        [DataSourceMethod]
        public override void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is not CharacterObject co)
                    return;

                var character = WCharacter.Get(co);
                if (character == null)
                    return;

                if (character.InCustomTree)
                {
                    // Player-mode editor, preselect the assigned map-faction (clan/kingdom).
                    var faction = character.AssignedMapFaction;
                    EditorLauncher.Launch(
                        EditorLaunchArgs.Player(faction: faction, character: character)
                    );
                    return;
                }

                // Vanilla/universal troop: keep universal semantics.
                EditorLauncher.Launch(EditorLaunchArgs.Universal(character: character));
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
