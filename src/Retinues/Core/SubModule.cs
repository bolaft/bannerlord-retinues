using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using HarmonyLib;
using Bannerlord.UIExtenderEx;
using Retinues.Core.Persistence.Item;
using Retinues.Core.Persistence.Troop;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Game.Features.Stocks;
using Retinues.Core.Game.Features.Unlocks;
using Retinues.Core.Game.Features.Unlocks.Behaviors;
using Retinues.Core.Utils;

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
                // Persistence behaviors
                cs.AddBehavior(new ItemSaveBehavior());
                cs.AddBehavior(new TroopSaveBehavior());

                // Clear all static lists on new game
                ClearAll();

                // Unlock behaviors
                cs.AddBehavior(new UnlocksBehavior());

                Log.Debug("Behaviors registered.");
            }
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
            UnlocksManager.UnlockedItems.Clear();
            // Clear item stocks
            StocksManager.Stocks.Clear();
        }
    }
}
