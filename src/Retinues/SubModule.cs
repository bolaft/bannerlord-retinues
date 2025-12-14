using System.Collections.Generic;
using Retinues.Behaviors;
using Retinues.Configuration;
using Retinues.Helpers;
using Retinues.Module;
using Retinues.Module.Compatibility;
using Retinues.Module.Dependencies;
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

        public static List<Dependency> Dependencies => [_harmony, _mcm, _uiextender, _butterlib];

        private static readonly HarmonyDependency _harmony = new();
        private static readonly MCMDependency _mcm = new();
        private static readonly UIExtenderExDependency _uiextender = new();
        private static readonly ButterLibDependency _butterlib = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Event Hooks                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            // Check for incompatible or legacy mods.
            CompatibilityManager.CheckIncompatibilities();

            // Log configuration
            SettingsManager.LogSettings();

            Log.Info("SubModule loaded.");
        }

        /// <summary>
        /// Called when a game starts or loads.
        /// </summary>
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter cs)
            {
                // Core Retinues behaviors (auto-discovered).
                BehaviorManager.RegisterCampaignBehaviors(cs);

                // Interops: add behaviors for mod compatibility.
                CompatibilityManager.RegisterCompatibilityBehaviors(cs);
            }

            Log.Info("Game started.");
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

            Log.Info("SubModule unloaded.");
        }

        /// <summary>
        /// Called once per application tick.
        /// </summary>
        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            // Check for hotkeys.
            Inputs.HotkeyCheck();
        }
    }
}
