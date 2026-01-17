using System.Collections.Generic;
using Retinues.Behaviors.Recruitement.Models;
using Retinues.Compatibility.Interops;
using Retinues.Configuration;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Modules;
using Retinues.Framework.Modules.Dependencies;
using Retinues.Framework.Modules.Dependencies.Core;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues
{
    /// <summary>
    /// Module entry point used by Bannerlord.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Dependencies                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static List<BaseDependency> Dependencies =>
            [_harmony, _mcm, _uiextender, _butterlib];

        private static readonly HarmonyDependency _harmony = new();
        private static readonly MCMDependency _mcm = new();
        private static readonly UIExtenderExDependency _uiextender = new();
        private static readonly ButterLibDependency _butterlib = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Event Hooks                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called before the initial module screen is set as root.
        /// </summary>
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            // Keep trying to register MCM until successful or max retries reached.
            _mcm.TryRegister();
        }

        /// <summary>
        /// Called when the module DLL is loaded by the game.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // Initialize log file.
            Log.Initialize(truncate: 5000);

            // Discover active modules and log them.
            ModuleManager.Initialize(logModules: true);

            // Initialize dependencies.
            foreach (var dependency in Dependencies)
                dependency.Initialize();

            // Apply safety patches.
            SafeMethodPatcher.ApplyAll(_harmony.Harmony);

            // Check for incompatible or legacy mods.
            InteropsManager.DisplayWarnings();

            // Log configuration
            SettingsManager.LogSettings();

            Log.Debug("SubModule loaded.");
        }

        /// <summary>
        /// Called when a game starts or loads.
        /// </summary>
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            // Clear any static state from previous games.
            foreach (var clear in Statics.ClearActions)
                clear();

            if (gameStarter is CampaignGameStarter cs)
            {
                // Core Retinues behaviors (auto-discovered).
                BehaviorManager.RegisterCampaignBehaviors(cs);

                // Interops: add behaviors for mod compatibility.
                InteropsManager.RegisterBehaviors(cs);

                // Add upstream recruitment model wrapper.
                // This must be done after other mods have added their models.
                CustomVolunteerModel.TryAdd(cs);
            }

            Log.Debug("Game started.");
        }

        /// <summary>
        /// Called when mission behaviors should be initialized.
        /// </summary>
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            // Auto-register mission behaviors.
            BehaviorManager.RegisterMissionBehaviors(mission);
        }

        /// <summary>
        /// Called when the module is unloaded.
        /// </summary>
        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

            // Shutdown dependencies
            foreach (var dependency in Dependencies)
                dependency.Shutdown();

            Log.Debug("SubModule unloaded.");
        }
    }
}
