using Retinues.Utilities;
using static Retinues.Configuration.SettingsManager;

namespace Retinues.Configuration
{
    /// <summary>
    /// Definitions for all configuration options (no boilerplate here).
    /// </summary>
    public static class Settings
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     User Interface                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section UserInterface = CreateSection(
            name: L.F("mcm_section_user_interface", "User Interface")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EditorHotkey = CreateOption(
            section: UserInterface,
            name: L.F("mcm_option_editor_hotkey", "Editor Hotkey"),
            hint: L.F(
                "mcm_option_editor_hotkey_hint",
                "Enables the Shift + R hotkey to open the editor."
            ),
            @default: true
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Equipment = CreateSection(
            name: L.F("mcm_section_equipment", "Equipment")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> AllItemsAvailable = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_all_items_available", "All Items Available"),
            hint: L.F(
                "mcm_option_all_items_available_hint",
                "Allows equipping any item regardless of unlock progress."
            ),
            @default: false
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Debug                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Debug = CreateSection(
            name: L.F("mcm_section_debug", "Debug")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> DebugMode = CreateOption(
            section: Debug,
            name: L.F("mcm_option_debug_mode", "Debug Mode"),
            hint: L.F("mcm_option_debug_mode_hint", "Enables debug logging and additional checks."),
            @default: false
        );
    }
}
