using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace MudToMail
{
    /// <summary>
    /// Module entry point used by Bannerlord.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called when the module DLL is loaded by the game.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // Enable UIExtenderEx
            EnableUIExtender();

            // Apply Harmony patches
            ApplyHarmonyPatches();

            // Register behavior overrides
            RegisterBehaviorOverrides();
        }

        /// <summary>
        /// Called when a game (campaign) starts or loads.
        /// </summary>
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter cs)
            {
                // Add MudToMail behaviors
                AddBehaviors(cs);
            }
        }

        /// <summary>
        /// Called when the module is unloaded.
        /// </summary>
        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

            // Remove Harmony patches
            RemoveHarmonyPatches();

            // Disable UIExtenderEx
            DisableUIExtender();

            Log.Debug("SubModule unloaded.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Harmony                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Harmony _harmony;

        [SafeMethod]
        private void ApplyHarmonyPatches()
        {
            _harmony = new Harmony("Retinues");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Apply safe method patcher
            SafeMethodPatcher.ApplyAll(_harmony, Assembly.GetExecutingAssembly());

            Log.Debug("Harmony patches applied.");
        }

        [SafeMethod]
        private void RemoveHarmonyPatches()
        {
            _harmony?.UnpatchAll("Retinues");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      UIExtenderEx                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private UIExtender _extender;

        [SafeMethod]
        public void EnableUIExtender()
        {
            _extender = UIExtender.Create("Retinues");
            _extender.Register(typeof(SubModule).Assembly);
            _extender.Enable();

            Log.Debug("UIExtender enabled & assembly registered.");
        }

        [SafeMethod]
        public void DisableUIExtender()
        {
            _extender?.Disable();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Behaviors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds all MudToMail campaign behaviors to the game starter.
        /// </summary>
        private void AddBehaviors(CampaignGameStarter cs)
        {
            Log.Debug("Behaviors registered.");
        }

        /// <summary>
        /// Registers Retinues behavior overrides.
        /// </summary>
        private void RegisterBehaviorOverrides()
        {
            Log.Debug("Retinues behavior overrides registered.");
        }
    }
}
