using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Core.Game;
using Retinues.Core.Game.Features.Unlocks.Behaviors;
using Retinues.Core.Game.Features.Xp.Behaviors;
using Retinues.Core.Game.Features.Tech.Behaviors;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Persistence.Item;
using Retinues.Core.Persistence.Troop;
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

                // Gameplay behaviors
                cs.AddBehavior(new UnlocksBehavior());
                cs.AddBehavior(new TroopXpBehavior());

                // Tech/Feat behavior
                cs.AddBehavior(new FeatServiceBehavior());

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
            WItem.UnlockedItems.Clear();
            // Clear item stocks
            WItem.Stocks.Clear();
        }
    }
}
