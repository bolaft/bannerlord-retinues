using System;
using Retinues.Configuration;
using Retinues.Utils;

namespace Retinues.Tests
{
    /// <summary>
    /// Scoped MCM option override for tests. Captures the current value, applies a new one, and
    /// restores the original on dispose. Lets tests drive config-gated behavior deterministically.
    ///
    /// Usage:
    ///   using (TestConfig.Set(Config.EquippingTroopsCostsGold, false)) { ... }
    ///
    /// Note: options flagged RequiresRestart only re-register behaviors at startup, so flipping
    /// them at runtime affects value reads but not already-registered behaviors.
    /// </summary>
    public static class TestConfig
    {
        public static IDisposable Set<T>(Option<T> option, T value) => new Scope<T>(option, value);

        private sealed class Scope<T> : IDisposable
        {
            private readonly Option<T> _option;
            private readonly T _previous;

            public Scope(Option<T> option, T value)
            {
                _option = option;
                _previous = option.Value;
                option.Value = value;
            }

            public void Dispose()
            {
                try
                {
                    _option.Value = _previous;
                }
                catch (Exception e)
                {
                    Log.Exception(e, "TestConfig: failed to restore option.");
                }
            }
        }
    }
}
