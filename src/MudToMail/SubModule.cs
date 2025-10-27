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
        /// <summary>
        /// UIExtender instance used to register and enable UI-related modifications.
        /// </summary>
        private UIExtender _extender;

        /// <summary>
        /// Harmony instance used to apply and remove runtime patches.
        /// </summary>
        private Harmony _harmony;

        /// <summary>
        /// Called when the module DLL is loaded by the game.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                _extender = UIExtender.Create("MudToMail");
                _extender.Register(typeof(SubModule).Assembly);
                _extender.Enable();
                Log.Debug("UIExtender enabled & assembly registered.");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            try
            {
                _harmony = new Harmony("MudToMail");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                // Apply safe method patcher
                SafeMethodPatcher.ApplyAll(_harmony, Assembly.GetExecutingAssembly());

                Log.Debug("Harmony patches applied.");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// Called when a game (campaign) starts or loads.
        /// </summary>
        protected override void OnGameStart(TaleWorlds.Core.Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter cs)
            {
                Log.Debug("Behaviors registered.");
            }
        }

        /// <summary>
        /// Called when the module is unloaded.
        /// </summary>
        protected override void OnSubModuleUnloaded()
        {
            try
            {
                _harmony?.UnpatchAll("MudToMail");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            try
            {
                _extender?.Disable();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            base.OnSubModuleUnloaded();
            Log.Debug("SubModule unloaded.");
        }
    }
}
