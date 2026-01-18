using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.Editor.Integration.Encyclopedia.Mixins
{
    [ViewModelMixin]
    public sealed class UnitPageMixin(EncyclopediaUnitPageVM vm)
        : BasePageMixin<EncyclopediaUnitPageVM>(vm)
    {
        [DataSourceProperty]
        public override EditorMode DesiredEditorMode
        {
            get
            {
                if (ViewModel.Obj is not CharacterObject co)
                    return EditorMode.Universal;

                var wc = WCharacter.Get(co);
                return wc != null && wc.IsFactionTroop ? EditorMode.Player : EditorMode.Universal;
            }
        }

        /// <summary>
        /// Opens the editor for the current unit page.
        /// </summary>
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

                if (character.IsFactionTroop)
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
