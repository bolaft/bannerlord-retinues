using System;
using System.Reflection;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Utils;

namespace CustomClanTroops
{
    public class SubModule : MBSubModuleBase
    {
        private UIExtender _extender;
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            try { System.IO.File.AppendAllText("CustomClanTroops.log", $"[EARLY] OnSubModuleLoad called at {System.DateTime.Now}\n"); } catch { }
            base.OnSubModuleLoad();

            try
            {
                _extender = UIExtender.Create("CustomClanTroops");
                _extender.Register(typeof(SubModule).Assembly);
                _extender.Enable();
                Log.Debug("UIExtender enabled & assembly registered.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to enable UIExtender: {ex}");
            }

            try
            {
                _harmony = new Harmony("CustomClanTroops");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Debug("Harmony patches applied.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to apply Harmony patches: {ex}");
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            Log.Debug($"SubModule.OnGameStart: {game?.GameType?.GetType().Name}");

            if (game.GameType is Campaign && gameStarter is CampaignGameStarter cs)
            {
                cs.AddBehavior(new CustomClanTroops.Behaviors.CampaignBehavior());
                Log.Debug("Registered CampaignBehavior.");
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            try
            {
                _harmony?.UnpatchAll("CustomClanTroops");
            }
            catch { /* ignore */ }

            base.OnSubModuleUnloaded();
            Log.Debug("SubModule unloaded.");
        }
    }
}
