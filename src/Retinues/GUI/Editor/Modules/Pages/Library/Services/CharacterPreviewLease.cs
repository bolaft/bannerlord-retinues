using System;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Modules.Pages.Library.Services
{
    /// <summary>
    /// Canonical leasing utility for temporary WCharacter stubs.
    /// - Optionally clones a vanilla baseline into the stub (for delta-only exports).
    /// - Applies a payload on top.
    /// - Restores the stub on dispose and releases it back to the pool.
    /// </summary>
    public static class CharacterPreviewLease
    {
        /// <summary>
        /// Lease container for a WCharacter stub.
        /// </summary>
        public sealed class Lease : IDisposable
        {
            private readonly string _snapshot;

            public WCharacter Character { get; }

            internal Lease(WCharacter character, string snapshot)
            {
                Character = character;
                _snapshot = snapshot;
            }

            /// <summary>
            /// Disposes the lease, restoring the stub to its original state.
            /// </summary>
            public void Dispose()
            {
                try
                {
                    if (Character == null)
                        return;

                    if (!string.IsNullOrWhiteSpace(_snapshot))
                        Character.Deserialize(_snapshot);

                    Character.IsActiveStub = false;
                    Character.MarkAllAttributesClean();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "CharacterStubLeaser.Lease.Dispose failed.");
                }
            }
        }

        /// <summary>
        /// Lease a stub and apply a payload to it.
        /// If modelStringId points to a vanilla troop, we clone it into the stub first (delta overlay support).
        /// </summary>
        public static Lease LeaseFromPayload(
            string payload,
            string modelStringId,
            out string missingVanillaBaseId
        )
        {
            missingVanillaBaseId = null;

            if (string.IsNullOrWhiteSpace(payload))
                return null;

            var stub = WCharacter.GetFreeStub();
            if (stub == null)
                return null;

            var snapshot = stub.Serialize();

            try
            {
                if (!string.IsNullOrWhiteSpace(modelStringId))
                {
                    var src = WCharacter.Get(modelStringId);

                    if (src != null && src.IsVanilla)
                        CharacterCloner.Clone(src, stub: stub);
                    else
                        missingVanillaBaseId = modelStringId;
                }

                stub.Deserialize(payload);
                stub.HiddenInEncyclopedia = true;

                return new Lease(stub, snapshot);
            }
            catch
            {
                try
                {
                    stub.Deserialize(snapshot);
                    stub.IsActiveStub = false;
                    stub.MarkAllAttributesClean();
                }
                catch
                {
                    // ignore
                }

                throw;
            }
        }
    }
}
