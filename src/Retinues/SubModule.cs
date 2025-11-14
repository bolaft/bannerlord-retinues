using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Effects;
using Retinues.Features.AutoJoin;
using Retinues.Features.Equipments;
using Retinues.Features.Experience;
using Retinues.Features.Staging;
using Retinues.Features.Stocks;
using Retinues.Features.Swaps;
using Retinues.Features.Unlocks;
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
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called before the initial module screen is set as root.
        /// </summary>
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            // Try to register with MCM
            TryRegisterWithMCM();
        }

        /// <summary>
        /// Called when the module DLL is loaded by the game.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // Truncate log file if needed
            TruncateLogFile();

            // Enable UIExtenderEx
            EnableUIExtender();

            // Apply Harmony patches
            ApplyHarmonyPatches();

            // Log module info
            LogModuleInfo();

            // Check for incompatible mods and display warnings
            ModCompatibility.IncompatibilityCheck();
        }

        /// <summary>
        /// Called when a game starts or loads.
        /// </summary>
        protected override void OnGameStart(TaleWorlds.Core.Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter cs)
            {
                // Clear all static lists
                ClearAll();

                // Add Retinues behaviors
                AddBehaviors(cs);
            }

            // Smoke test for localization
            Log.Debug(L.S("loc_smoke_test", "Localization test: default fallback (EN)."));
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
        //                     Mod Config Menu                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _mcmRegistered;
        private int _mcmRetryCount;
        private const int _mcmMaxRetries = 300; // ~5 seconds @ 60 FPS

        [SafeMethod]
        private void TryRegisterWithMCM()
        {
            // Try to register MCM once per tick until it works (or we time out)
            if (!_mcmRegistered && _mcmRetryCount < _mcmMaxRetries)
            {
                _mcmRetryCount++;
                _mcmRegistered = Config.RegisterWithMCM();

                if (_mcmRegistered)
                    Log.Info("MCM: registration succeeded.");
            }
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
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [SafeMethod]
        private void TruncateLogFile()
        {
            // Keep log file size manageable
            if (Log.LogFileLength > 20000)
                Log.Truncate(10000);
        }

        [SafeMethod]
        private void LogModuleInfo()
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Behaviors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly object _behaviorLock = new();
        private static readonly System.Collections.Generic.Dictionary<
            Type,
            Func<CampaignBehaviorBase>
        > _behaviorFactories = new(System.Collections.Generic.EqualityComparer<Type>.Default);

        /// <summary>
        /// Adds all Retinues campaign behaviors to the game starter.
        /// </summary>
        private void AddBehaviors(CampaignGameStarter cs)
        {
            Log.Info("Registering behaviors...");

            // Legacy behaviors
            AddBehavior<TroopBehavior>(cs);

            // Troop behaviors
            AddBehavior<FactionBehavior>(cs);

            // Safety behaviors
            AddBehavior<SanitizerBehavior>(cs);
            AddBehavior<VersionBehavior>(cs);

            // Item behaviors
            AddBehavior<UnlocksBehavior>(cs);
            AddBehavior<StocksBehavior>(cs);

            // Swap behaviors
            AddBehavior<MilitiaSwapBehavior>(cs);

            // Retinue behaviors
            AddBehavior<AutoJoinBehavior>(cs);

            // Combat equipment behavior
            AddBehavior<EquipmentPolicyBehavior>(cs);

            // Staging behaviors
            AddBehavior<TrainStagingBehavior>(cs);
            AddBehavior<EquipStagingBehavior>(cs);

            // XP behaviors
            AddBehavior<BattleXpBehavior>(cs);
            AddBehavior<BattleSimulationXpBehavior>(cs);

            // Doctrine behaviors (skip if doctrines disabled)
            if (Config.EnableDoctrines)
            {
                AddBehavior<DoctrineServiceBehavior>(cs);
                AddBehavior<DoctrineEffectRuntimeBehavior>(cs);

                if (Config.DisableFeatRequirements == false)
                {
                    AddBehavior<FeatServiceBehavior>(cs);
                    AddBehavior<FeatNotificationBehavior>(cs);
                }
            }

            // Mod compatibility behaviors
            ModCompatibility.AddBehaviors(cs);

            Log.Debug("Behaviors registered.");
        }

        /// <summary>
        /// Allow external mods to override a behavior used by Retinues.
        /// If multiple mods register for the same base type, the last registration wins.
        /// </summary>
        public static void RegisterBehavior<TBehavior>(Func<TBehavior> factory)
            where TBehavior : CampaignBehaviorBase
        {
            if (factory == null)
            {
                Log.Warn($"RegisterBehavior<{typeof(TBehavior).Name}> ignored: null factory.");
                return;
            }

            lock (_behaviorLock)
            {
                bool exists = _behaviorFactories.ContainsKey(typeof(TBehavior));
                _behaviorFactories[typeof(TBehavior)] = () => factory();
                Log.Info(
                    $"{(exists ? "Replaced" : "Registered")} behavior factory for {typeof(TBehavior).Name}."
                );
            }
        }

        /// <summary>
        /// Resolve a behavior instance: use an override if present, otherwise the provided default.
        /// </summary>
        private static TBehavior ResolveBehavior<TBehavior>(Func<TBehavior> defaultFactory)
            where TBehavior : CampaignBehaviorBase
        {
            lock (_behaviorLock)
            {
                if (_behaviorFactories.TryGetValue(typeof(TBehavior), out var f))
                {
                    try
                    {
                        if (f() is TBehavior b)
                            return b;
                        Log.Warn(
                            $"Factory for {typeof(TBehavior).Name} returned incompatible instance; falling back."
                        );
                    }
                    catch (Exception e)
                    {
                        Log.Exception(
                            e,
                            $"Factory for {typeof(TBehavior).Name} threw; falling back."
                        );
                    }
                }
            }

            return defaultFactory();
        }

        /// <summary>
        /// Helper to add a behavior with override support and logging.
        /// </summary>
        private static void AddBehavior<TBehavior>(
            CampaignGameStarter cs,
            Func<TBehavior> defaultFactory = null
        )
            where TBehavior : CampaignBehaviorBase, new()
        {
            var beh = ResolveBehavior(
                defaultFactory ?? (() => new TBehavior()),
                defaultFactory ?? (() => new TBehavior())
            );
            cs.AddBehavior(beh);
            Log.Debug($"Behavior active: {beh.GetType().FullName} (as {typeof(TBehavior).Name}).");
        }

        // Overload to satisfy ResolveBehavior private signature using same T
        private static TBehavior ResolveBehavior<TBehavior>(
            Func<TBehavior> df1,
            Func<TBehavior> df2
        )
            where TBehavior : CampaignBehaviorBase => ResolveBehavior(df1);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clears static caches and player-related state.
        /// </summary>
        private static void ClearAll()
        {
            Log.Debug("Clearing all static properties.");

            // Clear player info
            Player.Reset();

            // Clear active troops
            WCharacter.ActiveStubIds.Clear();

            // Clear vanilla id map
            WCharacter.VanillaStringIdMap.Clear();
        }
    }
}
