using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Effects;
using Retinues.Core.Features.Retinues.Behaviors;
using Retinues.Core.Features.Stocks.Behaviors;
using Retinues.Core.Features.Unlocks.Behaviors;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Mods;
using Retinues.Core.Safety.Backup;
using Retinues.Core.Safety.Legacy;
using Retinues.Core.Safety.Sanitizer;
using Retinues.Core.Safety.Version;
using Retinues.Core.Troops;
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
                // Keep log file size manageable
                if (Log.LogFileLength > 10000)
                    Log.Truncate(5000);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

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

            // Check for incompatible mods and display warnings
            ModCompatibility.IncompatibilityCheck();
        }

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
                cs.AddBehavior(new BackupBehavior());
                cs.AddBehavior(new VersionBehavior());

                // Item behaviors
                cs.AddBehavior(new UnlocksBehavior());
                cs.AddBehavior(new StocksBehavior());

                // Retinue buff behavior
                cs.AddBehavior(new RetinueBuffBehavior());

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

                // Legacy compatibility behaviors
                LegacyCompatibility.AddBehaviors(cs);

                // Mod compatibility behaviors
                ModCompatibility.AddBehaviors(cs);

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
