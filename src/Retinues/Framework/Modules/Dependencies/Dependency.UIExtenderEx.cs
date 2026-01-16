using System;
using Bannerlord.UIExtenderEx;
using Retinues.Utilities;

namespace Retinues.Modules.Dependencies
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                      UIExtenderEx                      //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// UIExtenderEx dependency: registers & enables Retinues UI mixins.
    /// Mirrors the old SubModule EnableUIExtender logic.
    /// </summary>
    public sealed class UIExtenderExDependency : Dependency
    {
        private UIExtender _extender;

        public UIExtenderExDependency()
            : base(
                moduleId: "Bannerlord.UIExtenderEx",
                displayName: "UIExtenderEx",
                kind: DependencyKind.Required
            ) { }

        public override void Initialize()
        {
            if (!IsModuleLoaded)
            {
                Log.Error("[UIExtenderEx] UIExtenderEx module not loaded; UI patches disabled.");
                MarkError();
                return;
            }

            if (_extender != null)
                return; // Already initialized

            try
            {
                var asm = typeof(UIExtenderExDependency).Assembly;

                _extender = UIExtender.Create("Retinues");
                _extender.Register(asm);
                _extender.Enable();

                MarkInitialized();
                Log.Debug("[UIExtenderEx] UIExtenderEx enabled & Retinues assembly registered.");
            }
            catch (Exception e)
            {
                MarkError();
                Log.Exception(e, "[UIExtenderEx] Enabling UIExtenderEx failed.");
            }
        }

        public override void Shutdown()
        {
            if (_extender == null)
                return;

            try
            {
                _extender.Disable();
                Log.Debug("[UIExtenderEx] UIExtenderEx disabled.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "[UIExtenderEx] Disabling UIExtenderEx failed.");
            }
            finally
            {
                _extender = null;
                IsInitialized = false;
            }
        }
    }
}
