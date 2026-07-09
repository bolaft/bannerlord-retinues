namespace Retinues.Framework.Model
{
    /// <summary>
    /// Thread-local nesting counter marking that a persistence restore OR a user import is in
    /// progress. While active, MAttribute keeps deserialized attributes dirty (so they are written
    /// to the next save) instead of marking them clean.
    ///
    /// This MUST live on a NON-generic type. A static field on <see cref="MBase{TBase}"/> is unique
    /// per closed generic, so <c>MBase&lt;CharacterObject&gt;</c> (what a troop's Deserialize bumps)
    /// and <c>MBase&lt;IModel&gt;</c> (what MAttribute checks) would be different fields. That split
    /// let user imports run with the flag false, so the imported attributes were silently cleaned
    /// and dropped from the save — the troop reloaded blank. One shared counter fixes both paths.
    /// </summary>
    public static class MRestoreScope
    {
        [System.ThreadStatic]
        private static int _depth;

        /// <summary>True while inside at least one restore/import scope.</summary>
        public static bool IsActive => _depth > 0;

        /// <summary>Enters a restore/import scope. Balanced with <see cref="Exit"/>.</summary>
        public static void Enter() => _depth++;

        /// <summary>Exits a restore/import scope (never goes below zero).</summary>
        public static void Exit()
        {
            if (_depth > 0)
                _depth--;
        }
    }
}
