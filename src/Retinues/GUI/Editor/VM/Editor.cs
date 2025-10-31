using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.GUI.Editor.VM.Doctrines;
using Retinues.GUI.Editor.VM.Equipment;
using Retinues.GUI.Editor.VM.Troop;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM
{
    /// <summary>
    /// Available editor screens.
    /// </summary>
    public enum Screen
    {
        Troop = 0,
        Equipment = 1,
        Doctrine = 2,
    }

    /// <summary>
    /// Root view-model coordinating editor screens.
    /// </summary>
    [SafeClass]
    public class EditorVM : BaseVM
    {
        /// <summary>
        /// Initialize the editor and its child screens.
        /// </summary>
        public EditorVM()
        {
            TroopScreen = new TroopScreenVM();
            EquipmentScreen = new EquipmentScreenVM();
            DoctrineScreen = new DoctrineScreenVM();

            SwitchScreen(Screen.Troop);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Screen                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Screen Screen { get; set; }

        /// <summary>
        /// Switch the active editor screen and update visibility.
        /// </summary>
        public void SwitchScreen(Screen value)
        {
            if (Screen == value && IsVisible)
                return;

            Log.Info($"Switching screen from {Screen} to {value}");

            Screen = value;

            // Toggle screen visibility via Show/Hide helpers
            if (Screen == Screen.Troop)
                TroopScreen.Show();
            else
                TroopScreen.Hide();
            if (Screen == Screen.Equipment)
                EquipmentScreen.Show();
            else
                EquipmentScreen.Hide();
            if (Screen == Screen.Doctrine)
                DoctrineScreen.Show();
            else
                DoctrineScreen.Hide();

            // Notify UI
            OnPropertyChanged(nameof(InTroopScreen));
            OnPropertyChanged(nameof(InEquipmentScreen));
            OnPropertyChanged(nameof(InDoctrineScreen));
            OnPropertyChanged(nameof(ShowFactionButton));
            OnPropertyChanged(nameof(ShowDoctrinesButton));
            OnPropertyChanged(nameof(ShowEquipmentButton));
            OnPropertyChanged(nameof(EquipmentButtonText));
            OnPropertyChanged(nameof(DoctrinesButtonText));
            OnPropertyChanged(nameof(FactionButtonText));
            OnPropertyChanged(nameof(EquipmentButtonBrush));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Faction] = [nameof(FactionButtonText), nameof(ShowFactionButton)],
                [UIEvent.Appearance] = [nameof(Model)],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public TroopScreenVM TroopScreen { get; set; }

        [DataSourceProperty]
        public EquipmentScreenVM EquipmentScreen { get; set; }

        [DataSourceProperty]
        public DoctrineScreenVM DoctrineScreen { get; set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ 3D Model ━━━━━━━ */

        [DataSourceProperty]
        public CharacterViewModel Model => State.Troop?.GetModel(State.Equipment.Index);

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string EquipmentButtonText =>
            Screen == Screen.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [DataSourceProperty]
        public string DoctrinesButtonText =>
            Screen == Screen.Doctrine
                ? L.S("close_doctrines_button_text", "Back")
                : L.S("doctrines_button_text", "Doctrines");

        [DataSourceProperty]
        public string FactionButtonText =>
            State.Faction == Player.Clan
                ? L.S("switch_to_kingdom_troops", "Switch to\nKingdom Troops")
                : L.S("switch_to_clan_troops", "Switch to\nClan Troops");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowFactionButton =>
            Screen != Screen.Doctrine && Player.Kingdom != null && !Config.NoKingdomTroops;

        [DataSourceProperty]
        public bool ShowDoctrinesButton =>
            Screen != Screen.Equipment && (Config.EnableDoctrines ?? false);

        [DataSourceProperty]
        public bool ShowEquipmentButton => Screen != Screen.Doctrine;

        /* ━━━━━━━━ Brushes ━━━━━━━ */

        [DataSourceProperty]
        public string EquipmentButtonBrush =>
            Screen == Screen.Equipment ? "ButtonBrush3" : "ButtonBrush1";

        /* ━━━━━━━━ Screens ━━━━━━━ */

        [DataSourceProperty]
        public bool InTroopScreen => Screen == Screen.Troop;

        [DataSourceProperty]
        public bool InEquipmentScreen => Screen == Screen.Equipment;

        [DataSourceProperty]
        public bool InDoctrineScreen => Screen == Screen.Doctrine;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Mode Switch ━━━━━ */

        /// <summary>
        /// Toggle between equipment and troop screens.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteToggleEquipment() =>
            SwitchScreen(Screen == Screen.Equipment ? Screen.Troop : Screen.Equipment);

        /// <summary>
        /// Toggle between doctrines and troop screens.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteToggleDoctrines() =>
            SwitchScreen(Screen == Screen.Doctrine ? Screen.Troop : Screen.Doctrine);

        /* ━━━━ Faction Switch ━━━━ */

        /// <summary>
        /// Switch displayed faction between clan and kingdom troops.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSwitchFaction() =>
            State.UpdateFaction(State.Faction == Player.Clan ? Player.Kingdom : Player.Clan);
    }
}
