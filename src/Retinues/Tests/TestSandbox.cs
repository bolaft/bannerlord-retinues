using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;

namespace Retinues.Tests
{
    /// <summary>
    /// Allocates throwaway custom stubs for a test and releases them on dispose, so domain tests
    /// do not leak custom troops into the live campaign.
    /// </summary>
    public sealed class TestSandbox : IDisposable
    {
        private readonly List<WCharacter> _allocated = [];

        /// <summary>Allocates a fresh active stub, tracked for release on dispose.</summary>
        public WCharacter NewStub()
        {
            var stub = WCharacter.GetFreeStub();
            if (stub != null)
                _allocated.Add(stub);
            return stub;
        }

        /// <summary>Tracks an externally-created custom troop (e.g. a clone) for release.</summary>
        public WCharacter Track(WCharacter wc)
        {
            if (wc != null)
                _allocated.Add(wc);
            return wc;
        }

        public void Dispose()
        {
            var errors = new List<Exception>();
            foreach (var wc in _allocated)
            {
                try
                {
                    wc.Remove();
                    if (wc.IsActiveStub)
                        errors.Add(new InvalidOperationException("Test stub remained active: " + wc.StringId));
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "TestSandbox: failed to release stub.");
                    errors.Add(ex);
                }
            }
            if (errors.Count > 0)
                throw new GameTestCleanupException("Test sandbox cleanup failed.", new AggregateException(errors));
        }
    }
}
