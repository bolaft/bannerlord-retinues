using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using HarmonyLib;
using Bannerlord.UIExtenderEx;
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

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            Log.Debug($"{game?.GameType?.GetType().Name}");

            if (game.GameType is Campaign && gameStarter is CampaignGameStarter cs)
            {
                cs.AddBehavior(new Behaviors.CampaignBehavior());
                cs.AddBehavior(new Behaviors.EquipmentUnlockBehavior());
                // cs.AddBehavior(new Behaviors.EquipmentUnlockMissionBehavior());
                Log.Debug("Registered CampaignBehavior.");
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
    }
}
