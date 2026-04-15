using System;
using System.Diagnostics.Tracing;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Character.Views.Column;
using Retinues.Editor.MVC.Pages.Equipment.Controllers;
using Retinues.Editor.MVC.Pages.Equipment.Views.Column;
using Retinues.Editor.MVC.Pages.Equipment.Views.List;
using Retinues.Editor.MVC.Pages.Library.Services;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Common.Column.Views
{
    /// <summary>
    /// ViewModel for the column in the editor GUI.
    /// </summary>
    public class ColumnVM : EventListenerVM
    {
        public ColumnVM(EquipmentListVM equipmentList)
        {
            _equipmentControls = new EquipmentControlsVM(equipmentList);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => State.Page != EditorPage.Settings;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Controls                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CustomizationControlsVM _customizationControls = new();

        [DataSourceProperty]
        public CustomizationControlsVM CustomizationControls => _customizationControls;

        private readonly EquipmentControlsVM _equipmentControls;

        [DataSourceProperty]
        public EquipmentControlsVM EquipmentControls => _equipmentControls;

        private readonly CharacterControlsVM _characterControls = new();

        [DataSourceProperty]
        public CharacterControlsVM CharacterControls => _characterControls;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Model                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private CharacterViewModel _model;

        [DataSourceProperty]
        public CharacterViewModel Model
        {
            get => _model;
            private set
            {
                if (ReferenceEquals(value, _model))
                    return;

                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        /// <summary>
        /// Applies the planned/staged equipment to the given equipment instance for the 3D model.
        /// </summary>
        private static void ApplyPlannedEquipmentForModel(
            TaleWorlds.Core.Equipment equipmentForModel
        )
        {
            // Preview mode must win: the preview clone already contains the temporary items.
            // Overlaying planned/staged equipment would overwrite the preview and break it.
            if (PreviewController.Enabled)
                return;

            var me = State.Equipment;
            if (me == null || equipmentForModel == null)
                return;

            int slots = (int)TaleWorlds.Core.EquipmentIndex.NumEquipmentSetSlots;

            for (int i = 0; i < slots; i++)
            {
                var slot = (TaleWorlds.Core.EquipmentIndex)i;
                var item = me.Get(slot);

                equipmentForModel[slot] =
                    item == null
                        ? TaleWorlds.Core.EquipmentElement.Invalid
                        : new TaleWorlds.Core.EquipmentElement(item.Base);
            }
        }

        [EventListener(UIEvent.Appearance, UIEvent.Page, UIEvent.Library, UIEvent.Doctrine)]
        private void RebuildModel()
        {
            CharacterPreviewLease.Lease lease = null;

            try
            {
                WCharacter character;

                if (State.Page == EditorPage.Library)
                {
                    var item = EditorState.Instance.LibraryItem;

                    if (item == null)
                    {
                        Model = null;
                        return;
                    }

                    // Extract the first character payload from the export file.
                    // (For faction exports this means "preview first troop" which matches old behavior.)
                    if (
                        !ExportXMLReader.TryExtractModelCharacterPayloads(item, out var payloads)
                        || payloads.Count == 0
                    )
                    {
                        Model = null;
                        return;
                    }

                    var p = payloads[0];

                    lease = CharacterPreviewLease.LeaseFromPayload(
                        p.Payload,
                        p.ModelStringId,
                        out _
                    );
                    character = lease?.Character;

                    if (character?.Base == null)
                    {
                        Model = null;
                        return;
                    }
                }
                else if (State.Page == EditorPage.Doctrines)
                {
                    // Try doctrine-specific preview character first.
                    var previewId = State.Doctrine?.PreviewCharacterId?.Invoke();
                    var previewChar = !string.IsNullOrEmpty(previewId)
                        ? WCharacter.Get(previewId)
                        : null;

                    if (previewChar?.Base != null)
                    {
                        var previewVM = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                        previewVM.FillFrom(previewChar.Base, seed: -1);

                        var previewEq = previewChar.FirstBattleEquipment?.Base;
                        if (previewEq != null)
                            previewVM.SetEquipment(previewEq);

                        Model = previewVM;
                        return;
                    }

                    // Fall back to the player hero with civilian equipment.
                    var playerVM = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                    playerVM.FillFrom(Player.Hero.Character.Base, seed: -1);
                    playerVM.SetEquipment(Player.Hero.CivilianEquipment.Base);

                    Model = playerVM;
                    return;
                }
                else
                {
                    character = State.Character;

                    if (character?.Base == null)
                    {
                        Model = null;
                        return;
                    }
                }

                var co = character.Base;

                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);

                try
                {
                    vm.FillFrom(co, seed: -1);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    Model = null;
                    return;
                }

                // Important: apply equipment after FillFrom.
                // In Library mode, we want the equipment from the XML-applied character,
                // not the editor-selected equipment.
                TaleWorlds.Core.Equipment equipmentForModel = null;

                if (State.Page == EditorPage.Library)
                {
                    var src = character?.FirstBattleEquipment?.Base ?? co?.FirstBattleEquipment;
                    if (src != null)
                        equipmentForModel = new TaleWorlds.Core.Equipment(src);
                }
                else
                {
                    equipmentForModel = PreviewController.GetEquipmentForModel();
                    if (equipmentForModel == null)
                        return;

                    // Overlay planned/staged items so the 3D model reflects them.
                    ApplyPlannedEquipmentForModel(equipmentForModel);
                }

                if (equipmentForModel != null)
                    vm.SetEquipment(equipmentForModel);

                // Apply faction visuals (colors, heraldry).
                var faction = State.Faction;
                if (faction != null)
                {
                    // Armor colors — fall back to culture colors when the clan has none
                    // (e.g. companion clans created by vanilla which leave Color/Color2 at 0).
                    var color1 = faction.Color;
                    var color2 = faction.Color2;

                    if (
                        (color1 == 0 || color2 == 0)
                        && faction is Retinues.Domain.Factions.Wrappers.WClan wClan
                    )
                    {
                        var kingdomBase = wClan.Kingdom?.Base;
                        var cultureBase = wClan.Culture?.Base;

                        if (color1 == 0)
                            color1 =
                                (
                                    kingdomBase?.Color is uint kc1 && kc1 != 0
                                        ? kc1
                                        : cultureBase?.Color
                                ) ?? 0;
                        if (color2 == 0)
                            color2 =
                                (
                                    kingdomBase?.Color2 is uint kc2 && kc2 != 0
                                        ? kc2
                                        : cultureBase?.Color2
                                ) ?? 0;
                    }

                    vm.ArmorColor1 = color1;
                    vm.ArmorColor2 = color2;

                    // Heraldic items
                    vm.BannerCodeText = faction.Banner.Serialize();
                }

                Model = vm;
            }
            finally
            {
                lease?.Dispose();
            }
        }

        [EventListener(UIEvent.Item)]
        private void RefreshEquipmentOnModel()
        {
            if (State.Page == EditorPage.Library)
                return;

            var equipment = PreviewController.GetEquipmentForModel();
            if (equipment == null || Model == null)
                return;

            try
            {
                // Overlay planned/staged items so the 3D model reflects them.
                ApplyPlannedEquipmentForModel(equipment);

                Model.SetEquipment(equipment);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Equipment Button                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool ShowEquipmentButton =>
            State.Page == EditorPage.Character || State.Page == EditorPage.Equipment;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public string EquipmentButtonText =>
            State.Page == EditorPage.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public string EquipmentButtonBrush =>
            State.Page == EditorPage.Equipment ? "ButtonBrush3" : "ButtonBrush1";

        [DataSourceMethod]
        public void ExecuteToggleEquipmentMode()
        {
            if (State.Page == EditorPage.Equipment)
                State.SetPage(EditorPage.Character);
            else
                State.SetPage(EditorPage.Equipment);
        }
    }
}
