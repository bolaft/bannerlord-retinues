using TaleWorlds.MountAndBlade;

namespace Retinues.MCM
{
    public sealed class SubModule : MBSubModuleBase
    {
        private bool _registered;

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            // Try once per frame until it succeeds (then stop).
            if (!_registered)
                _registered = SettingsBootstrap.Register();
        }
    }
}
