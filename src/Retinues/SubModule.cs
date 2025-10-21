using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Effects;
using Retinues.Features.Missions.Behaviors;
using Retinues.Features.Recruits.Behaviors;
using Retinues.Features.Stocks.Behaviors;
using Retinues.Features.Unlocks.Behaviors;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Features.Xp.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Mods;
using Retinues.Safety.Legacy;
using Retinues.Safety.Sanitizer;
using Retinues.Safety.Version;
using Retinues.Troops;
using Retinues.Utils;
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
        /// <summary>
        /// UIExtender instance used to register and enable UI-related modifications.
        /// </summary>
        private UIExtender _extender;

        /// <summary>
        /// Harmony instance used to apply and remove runtime patches.
        /// </summary>
        private Harmony _harmony;

        /// <summary>
        /// Flag indicating whether the mod has successfully registered with MCM.
        /// </summary>
        private bool _mcmRegistered;
        private int _mcmRetryCount;
        private const int _mcmMaxRetries = 300; // ~5 seconds @ 60 FPS

        /// <summary>
        /// Called before the initial module screen is set as root. Used to register with MCM.
        /// </summary>
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            // Try to register MCM once per tick until it works (or we time out)
            if (!_mcmRegistered && _mcmRetryCount < _mcmMaxRetries)
            {
                _mcmRetryCount++;
                _mcmRegistered = Config.RegisterWithMCM();

                if (_mcmRegistered)
                    Log.Info("MCM: registration succeeded.");
            }
        }

        /// <summary>
        /// Called when the module DLL is loaded by the game.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                // Keep log file size manageable
                if (Log.LogFileLength > 20000)
                    Log.Truncate(10000);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            try
            {
                Log.Info(
                    $"Bannerlord version: {BannerlordVersion.Version.Major}.{BannerlordVersion.Version.Minor}.{BannerlordVersion.Version.Revision}"
                );
                Log.Info("Modules:");

                foreach (var mod in ModuleChecker.GetActiveModules())
                {
                    Log.Info(
                        $"    {(mod.IsOfficial ? "[Official]" : "[Community]")} {mod.Id} {mod.Version}"
                    );
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            try
            {
                _extender = UIExtender.Create("Retinues");
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
                _harmony = new Harmony("Retinues");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                // Apply safe method patcher
                SafeMethodPatcher.ApplyAll(_harmony, Assembly.GetExecutingAssembly());

                Log.Debug("Harmony patches applied.");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            // Check for incompatible mods and display warnings
            ModCompatibility.IncompatibilityCheck();
        }

        /// <summary>
        /// Called when a game (campaign) starts or loads.
        /// </summary>
        protected override void OnGameStart(TaleWorlds.Core.Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter cs)
            {
                // Clear all static lists
                ClearAll();

                // Troop behaviors
                cs.AddBehavior(new TroopBehavior());

                // Safety behaviors
                cs.AddBehavior(new SanitizerBehavior());
                cs.AddBehavior(new VersionBehavior());

                // Item behaviors
                cs.AddBehavior(new UnlocksBehavior());
                cs.AddBehavior(new StocksBehavior());

                // Swap behaviors
                cs.AddBehavior(new MilitiaSwapBehavior());

                // Combat equipment behavior
                cs.AddBehavior(new CombatEquipmentBehavior());

                // Training behavior
                cs.AddBehavior(new TroopTrainBehavior());

                // Equipment behavior
                cs.AddBehavior(new TroopEquipBehavior());

                // XP behavior (skip if both costs are 0)
                cs.AddBehavior(new TroopXpBehavior());
                cs.AddBehavior(new TroopXpAutoResolveBehavior());

                // Doctrine behaviors (skip if doctrines disabled)
                if (Config.EnableDoctrines)
                {
                    cs.AddBehavior(new DoctrineServiceBehavior());
                    cs.AddBehavior(new FeatServiceBehavior());
                    cs.AddBehavior(new FeatNotificationBehavior());
                    cs.AddBehavior(new DoctrineEffectRuntimeBehavior());
                }

                // Legacy compatibility behaviors
                LegacyCompatibility.AddBehaviors(cs);

                // Mod compatibility behaviors
                ModCompatibility.AddBehaviors(cs);

                Log.Debug("Behaviors registered.");
            }

            // Smoke test for localization
            Log.Debug(L.S("loc_smoke_test", "Localization test: default fallback (EN)."));
        }

        /// <summary>
        /// Called when the module is unloaded.
        /// </summary>
        protected override void OnSubModuleUnloaded()
        {
            try
            {
                _harmony?.UnpatchAll("Retinues");
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

        /// <summary>
        /// Clears static caches and player-related state used by the mod to ensure a clean slate
        /// when starting or loading a new campaign to prevent cross-save contamination.
        /// </summary>
        private static void ClearAll()
        {
            Log.Debug("Clearing all static properties.");
            // Clear player info
            Player.Reset();
            // Clear active troops
            WCharacter.ActiveTroops.Clear();
            // Clear vanilla id map
            WCharacter.VanillaStringIdMap.Clear();
        }
    }
}
