using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Utils;

namespace Retinues.Tests
{
    /// <summary>
    /// Disposable test scope that provides isolation for tests which create custom troops.
    /// On dispose it removes every custom stub that became active during the scope, so a test
    /// run does not leak troops into the loaded campaign.
    ///
    /// Usage:
    ///   using var sandbox = new TestSandbox();
    ///   // ... create troops, run assertions ...
    /// </summary>
    public sealed class TestSandbox : IDisposable
    {
        private readonly HashSet<string> _stubsBefore;

        public TestSandbox()
        {
            _stubsBefore = new HashSet<string>(WCharacter.ActiveStubIds, StringComparer.Ordinal);
        }

        /// <summary>
        /// Allocates a fresh custom stub, marks it active, and returns it as a wrapper.
        /// The stub is automatically released when the sandbox is disposed.
        /// </summary>
        public WCharacter NewStub()
        {
            var stub = WCharacter.AllocateStub();
            if (!WCharacter.ActiveStubIds.Contains(stub.StringId))
                WCharacter.ActiveStubIds.Add(stub.StringId);
            return new WCharacter(stub);
        }

        public void Dispose()
        {
            // Remove any custom stub that became active during this scope.
            var created = WCharacter
                .ActiveStubIds.Where(id => !_stubsBefore.Contains(id))
                .ToList();

            foreach (var id in created)
            {
                try
                {
                    var troop = WCharacter.FromStringId(id);
                    if (troop?.IsCustom == true)
                        troop.Remove();
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"TestSandbox: failed to release stub '{id}'.");
                }
            }
        }
    }
}
