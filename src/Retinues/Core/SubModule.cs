using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Core.Compatibility.Shokuho;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Effects;
using Retinues.Core.Features.Retinues.Behaviors;
using Retinues.Core.Features.Stocks.Behaviors;
using Retinues.Core.Features.Unlocks.Behaviors;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Persistence.Troop;
using Retinues.Core.Safety.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core
{
    public class SubModule : MBSubModuleBase
    {
        private UIExtender _extender;
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                _extender = UIExtender.Create("Retinues.Core");
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
                _harmony = new Harmony("Retinues.Core");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                // Apply safe method patcher
                SafeMethodPatcher.ApplyAll(_harmony, Assembly.GetExecutingAssembly());

                Log.Debug("Harmony patches applied.");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            var knownIncompatibilities = new string[]
            {
                "WarlordsBattlefield",
                // "AdonnaysTroopChanger",
                "SimpleBank",
            };

            try
            {
                Log.Info("Active modules:");
                foreach (var m in ModuleChecker.GetActiveModules())
                {
                    Log.Info($" - {m.Id} {m.Version}");
                    foreach (var inc in knownIncompatibilities)
                        if (string.Equals(m.Id, inc, StringComparison.OrdinalIgnoreCase))
                            Log.Critical(
                                $"WARNING: {m.Id} is known to be incompatible with Retinues!"
                            );
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        protected override void OnGameStart(TaleWorlds.Core.Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            Log.Debug($"{game?.GameType?.GetType().Name}");

            if (game.GameType is Campaign && gameStarter is CampaignGameStarter cs)
            {
                // Clear all static lists
                ClearAll();

                // Persistence behaviors
                cs.AddBehavior(new TroopSaveBehavior());

                // Safety behaviors
                cs.AddBehavior(new SafetyBehavior());
                cs.AddBehavior(new BackupBehavior());
                cs.AddBehavior(new SaveBackCompatibilityBehavior());

                // Item behaviors
                cs.AddBehavior(new UnlocksBehavior());
                cs.AddBehavior(new StocksBehavior());

                // Retinue buff behavior
                cs.AddBehavior(new RetinueBuffBehavior());

                // Shokuho behavior (skip if not Shokuho)
                if (ShokuhoDetect.IsShokuhoCampaign())
                {
                    Log.Debug("Shokuho detected, using ShokuhoVolunteerSwapBehavior.");
                    cs.AddBehavior(new ShokuhoVolunteerSwapBehavior());
                }

                // XP behavior (skip if both costs are 0)
                if (
                    Config.GetOption<int>("BaseSkillXpCost") > 0
                    || Config.GetOption<int>("SkillXpCostPerPoint") > 0
                )
                {
                    cs.AddBehavior(new TroopXpBehavior());
                }

                // Doctrine behaviors (skip if doctrines disabled)
                if (Config.GetOption<bool>("EnableDoctrines"))
                {
                    cs.AddBehavior(new DoctrineServiceBehavior());
                    cs.AddBehavior(new FeatServiceBehavior());
                    cs.AddBehavior(new FeatNotificationBehavior());
                    cs.AddBehavior(new DoctrineEffectRuntimeBehavior());
                }

                Log.Debug("Behaviors registered.");
            }

            // Smoke test for localization
            Log.Debug(L.S("loc_smoke_test", "Localization test: default fallback (EN)."));
        }

        protected override void OnSubModuleUnloaded()
        {
            try
            {
                _harmony?.UnpatchAll("Retinues.Core");
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
