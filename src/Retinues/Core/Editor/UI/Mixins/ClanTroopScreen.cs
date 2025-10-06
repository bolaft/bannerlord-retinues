using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Core.Editor.UI.VM;
using Retinues.Core.Game;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.Mixins
{
    /// <summary>
    /// Mixin for ClanManagementVM to inject the custom troop editor screen and manage tab selection.
    /// </summary>
    [ViewModelMixin(
        "TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanManagementVM"
    )]
    public sealed class ClanTroopScreen : BaseViewModelMixin<ClanManagementVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ClanTroopScreen(ClanManagementVM vm)
            : base(vm)
        {
            try
            {
                _screen = new EditorScreenVM(Player.Clan, this);

                ViewModel.PropertyChangedWithBoolValue += OnVanillaTabChanged;

                ClanHotkeyGate.Active = true;
                ClanHotkeyGate.RequireShift = false;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public override void OnFinalize()
        {
            ClanHotkeyGate.Active = false; // restore default behavior
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly EditorScreenVM _screen;

        [DataSourceProperty]
        public EditorScreenVM EditorScreen => _screen;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unselects all vanilla clan management tabs when switching to troop editor.
        /// </summary>
        public void UnselectVanillaTabs()
        {
            ViewModel.IsMembersSelected = false;
            ViewModel.IsFiefsSelected = false;
            ViewModel.IsPartiesSelected = false;
            ViewModel.IsIncomeSelected = false;

            ViewModel.ClanMembers.IsSelected = false;
            ViewModel.ClanParties.IsSelected = false;
            ViewModel.ClanFiefs.IsSelected = false;
            ViewModel.ClanIncome.IsSelected = false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Handles vanilla tab selection changes to keep troop editor tab state in sync.
        /// </summary>
        private void OnVanillaTabChanged(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            if (!e.Value)
                return;

            if (
                e.PropertyName == "IsMembersSelected"
                || e.PropertyName == "IsFiefsSelected"
                || e.PropertyName == "IsPartiesSelected"
                || e.PropertyName == "IsIncomeSelected"
            )
            {
                if (EditorScreen != null)
                    EditorScreen.IsTroopsSelected = false;
            }
        }
    }
}
