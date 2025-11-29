using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Effects;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor;
using Retinues.GUI.Helpers;
using Retinues.Mods;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace Retinues
{
    /// <summary>
    /// Module entry point used by Bannerlord.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool HarmonyPatchesApplied = false;
        public static bool UIExtenderExEnabled = false;
        public static bool MCMRegistered = false;

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

        /// <summary>
        /// Called once per application tick.
        /// </summary>
        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            TryHandleEditorHotkeys(dt);
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
                {
                    Log.Info("MCM: registration succeeded.");
                    MCMRegistered = true;
                }

                Config.LogDump();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Harmony                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Harmony _harmony;

        private void ApplyHarmonyPatches()
        {
            try
            {
                _harmony = new Harmony("Retinues");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                // Apply safe method patcher
                SafeMethodPatcher.ApplyAll(_harmony, Assembly.GetExecutingAssembly());

                // Apply mod compatibility patches
                ModCompatibility.AddPatches(_harmony);

                Log.Debug("Harmony patches applied.");
                HarmonyPatchesApplied = true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Error while applying Harmony patches.");
            }
        }

        private void RemoveHarmonyPatches()
        {
            try
            {
                _harmony?.UnpatchAll("Retinues");
            }
            catch (Exception e)
            {
                Log.Exception(e, "Error while removing existing Harmony patches.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      UIExtenderEx                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private UIExtender _extender;

        [SafeMethod]
        public void EnableUIExtender()
        {
            try
            {
                _extender = UIExtender.Create("Retinues");
                _extender.Register(typeof(SubModule).Assembly);
                _extender.Enable();

                Log.Debug("UIExtender enabled & assembly registered.");
                UIExtenderExEnabled = true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "UIExtender enabling failed.");
            }
        }

        [SafeMethod]
        public void DisableUIExtender()
        {
            try
            {
                _extender?.Disable();
                Log.Debug("Disabling UIExtender...");
            }
            catch (Exception e)
            {
                Log.Exception(e, "UIExtender disabling failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Hotkeys                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [SafeMethod]
        private static void TryHandleEditorHotkeys(float dt)
        {
            try
            {
                // Config gate
                if (!Config.EnableEditorHotkey)
                    return;

                var game = TaleWorlds.Core.Game.Current;
                if (game == null)
                    return;

                // Only on campaign map
                if (game.GameStateManager.ActiveState is not MapState)
                    return;

                // Must be in a campaign
                if (Campaign.Current == null)
                    return;

                if (!Input.IsKeyDown(InputKey.LeftShift))
                    return;

                // Shift + R -> personal editor
                if (Input.IsKeyReleased(InputKey.R))
                {
                    Log.Info("EditorMapHotkey: Shift+R pressed on map (OnApplicationTick).");
                    ClanScreen.LaunchEditor(EditorMode.Personal);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
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

            // XP behaviors
            AddBehavior<Features.Experience.TroopXpBehavior>(cs);
            AddBehavior<Features.Experience.BattleSimulationXpBehavior>(cs);

            // Staging behaviors
            AddBehavior<Features.Staging.TrainStagingBehavior>(cs);
            AddBehavior<Features.Staging.EquipStagingBehavior>(cs);

            // Legacy staging behaviors
            AddBehavior<Safety.Legacy.TroopEquipBehavior>(cs);
            AddBehavior<Safety.Legacy.TroopTrainBehavior>(cs);

            // Legacy behaviors
            AddBehavior<Safety.Legacy.TroopBehavior>(cs);

            // Troop behaviors (after legacy migrations and xp)
            AddBehavior<Troops.FactionBehavior>(cs);

            // Safety behaviors
            AddBehavior<Safety.Sanitizer.SanitizerBehavior>(cs);
            AddBehavior<Safety.Version.VersionBehavior>(cs);
            AddBehavior<Safety.Version.DependenciesBehavior>(cs);
            AddBehavior<Safety.Fixes.PartyLeaderFixBehavior>(cs);

            // Item behaviors
            AddBehavior<Features.Unlocks.UnlocksBehavior>(cs);
            AddBehavior<Features.Stocks.StocksBehavior>(cs);

            // Swap behaviors
            AddBehavior<Features.Swaps.MilitiaSwapBehavior>(cs);
            AddBehavior<Features.Volunteers.VolunteerSwapBehavior>(cs);

            // Retinue behaviors
            AddBehavior<Features.AutoJoin.AutoJoinBehavior>(cs);

            // Combat equipment behavior
            AddBehavior<Features.Agents.CombatAgentBehavior>(cs);

            // Statistics behavior
            AddBehavior<Features.Statistics.TroopStatisticsBehavior>(cs);

            // Doctrine behaviors (skip if doctrines disabled)
            if (Config.EnableDoctrines)
            {
                AddBehavior<DoctrineServiceBehavior>(cs);
                AddBehavior<DoctrineEffectRuntimeBehavior>(cs);

                if (Config.EnableFeatRequirements)
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

            // Clear upgrade map
            WCharacter.UpgradeMap.Clear();

            // Clear captain caches
            WCharacter.ClearCaptainCaches();

            // Clear edited vanilla roots
            WCharacter.EditedVanillaRootIds.Clear();

            // Clear faction troop map
            BaseFaction.TroopFactionMap.Clear();

            Log.Debug("All static properties cleared.");
        }
    }
}
