using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Editor.Events;
using Retinues.Editor.Services.Library.NPCCharacters;
using Retinues.Editor.VM.Column.Character;
using Retinues.Editor.VM.Column.Equipment;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Column
{
    public class ColumnVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Controls                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CustomizationControlsVM _customizationControls = new();

        [DataSourceProperty]
        public CustomizationControlsVM CustomizationControls => _customizationControls;

        private readonly EquipmentControlsVM _equipmentControls = new();

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

        [EventListener(UIEvent.Appearance, UIEvent.Page, UIEvent.Library)]
        private void RebuildModel()
        {
            CharacterStubLeaser.Lease lease = null;

            try
            {
                WCharacter character;

                if (EditorVM.Page == EditorPage.Library)
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
                        !LibraryExportPayloadReader.TryExtractModelCharacterPayloads(
                            item,
                            out var payloads
                        )
                        || payloads.Count == 0
                    )
                    {
                        Model = null;
                        return;
                    }

                    var p = payloads[0];

                    lease = CharacterStubLeaser.LeaseFromPayload(p.Payload, p.ModelStringId, out _);
                    character = lease?.Character;

                    if (character?.Base == null)
                    {
                        Model = null;
                        return;
                    }
                }
                else if (EditorVM.Page == EditorPage.Doctrines)
                {
                    // Use player character for doctrines preview.
                    var playerVM = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                    playerVM.FillFrom(Player.Hero.Character.Base, seed: -1);

                    // Use civilian equipment explicitly
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

                if (EditorVM.Page == EditorPage.Library)
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
                    // Armor colors
                    vm.ArmorColor1 = faction.Color;
                    vm.ArmorColor2 = faction.Color2;

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
            if (EditorVM.Page == EditorPage.Library)
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
            EditorVM.Page == EditorPage.Character || EditorVM.Page == EditorPage.Equipment;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public string EquipmentButtonText =>
            EditorVM.Page == EditorPage.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public string EquipmentButtonBrush =>
            EditorVM.Page == EditorPage.Equipment ? "ButtonBrush3" : "ButtonBrush1";

        [DataSourceMethod]
        public void ExecuteToggleEquipmentMode()
        {
            if (EditorVM.Page == EditorPage.Equipment)
                EditorVM.SetPage(EditorPage.Character);
            else
                EditorVM.SetPage(EditorPage.Equipment);
        }
    }
}
