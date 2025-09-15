using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Core.Game;
using Retinues.Core.Game.Features.Unlocks.Behaviors;
using Retinues.Core.Game.Features.Xp.Behaviors;
using Retinues.Core.Game.Features.Doctrines;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Persistence.Item;
using Retinues.Core.Persistence.Troop;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Localization;

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
                Log.Debug("Harmony patches applied.");
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
                cs.AddBehavior(new ItemSaveBehavior());
                cs.AddBehavior(new TroopSaveBehavior());

                // XP behavior (skip if both costs are 0)
                if (Config.GetOption<int>("BaseSkillXpCost") > 0 || Config.GetOption<int>("SkillXpCostPerPoint") > 0)
                {
                    cs.AddBehavior(new TroopXpBehavior());
                    Log.Debug("Troop XP enabled.");
                }

                // Unlocks behavior (skip if disabled)
                if (Config.GetOption<bool>("UnlockFromKills") && !Config.GetOption<bool>("AllEquipmentUnlocked"))
                {
                    cs.AddBehavior(new UnlocksBehavior());
                    Log.Debug("Item unlocks enabled.");
                }

                // Doctrine behaviors (skip if doctrines disabled)
                if (Config.GetOption<bool>("EnableDoctrines"))
                {
                    cs.AddBehavior(new DoctrineServiceBehavior());
                    cs.AddBehavior(new FeatRuntimeBehavior());
                    cs.AddBehavior(new FeatUnlockNotifierBehavior());
                    Log.Debug("Doctrines enabled.");
                }

                Log.Debug("Behaviors registered.");
            }

            Log.Debug(L.S("loc_smoke_test", "Localization test FAILED."));
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

            base.OnSubModuleUnloaded();
            Log.Debug("SubModule unloaded.");
        }

        private static void ClearAll()
        {
            // Clear player factions and troops
            Player.Clear();
            // Clear vanilla string id map
            WCharacter.VanillaStringIdMap.Clear();
            // Clear item unlocks
            WItem.UnlockedItems.Clear();
            // Clear item stocks
            WItem.Stocks.Clear();
        }
    }
}
