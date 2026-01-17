using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using Retinues.Utilities;
using TaleWorlds.ScreenSystem;

namespace Retinues.Configuration.Menu
{
    static partial class MCM
    {
        private const string ModOptionsScreenTypeName =
            "MCM.UI.GUI.GauntletUI.ModOptionsGauntletScreen";

        static int _openNonce;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Launcher                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Try to open the MCM settings screen for the given settings ID.
        /// </summary>
        public static bool TryOpenSettings(string settingsId)
        {
            if (string.IsNullOrWhiteSpace(settingsId))
                return false;

            try
            {
                var screenType = FindType(ModOptionsScreenTypeName);
                if (screenType == null)
                {
                    Log.Warning($"MCM screen type not found: {ModOptionsScreenTypeName}");
                    return false;
                }

                var screenObj = CreateBest(screenType);
                if (screenObj is not ScreenBase screen)
                {
                    Log.Warning($"Could not create MCM screen instance: {screenType.FullName}");
                    return false;
                }

                var nonce = ++_openNonce;

                ScreenManager.PushScreen(screen);

                // Important: selection must be deferred (MCM populates the list via SyncContext.Send).
                QueueSelectWhenReady(screen, settingsId, nonce);

                Log.Debug($"Opened MCM settings screen (target: {settingsId})");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"TryOpenSettings failed: {ex}");
                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Queue a selection attempt for the given settings ID when the screen is ready.
        /// </summary>
        private static void QueueSelectWhenReady(ScreenBase screen, string settingsId, int nonce)
        {
            var ctx = SynchronizationContext.Current;
            if (ctx != null)
            {
                ctx.Post(_ => TrySelectPoll(screen, settingsId, nonce, attempt: 0), null);
                return;
            }

            // Fallback (should be rare in Bannerlord)
            TrySelectPoll(screen, settingsId, nonce, attempt: 0);
        }

        /// <summary>
        /// Poll for the ModOptionsVM and try to select our settings entry.
        /// </summary>
        private static void TrySelectPoll(
            ScreenBase screen,
            string settingsId,
            int nonce,
            int attempt
        )
        {
            // If another open happened after us, abort.
            if (nonce != _openNonce)
                return;

            // Give MCM some time to create VM + populate list.
            // 120 attempts ~= a couple seconds worth of ticks depending on queue load.
            const int maxAttempts = 120;

            try
            {
                var vm = TryGetModOptionsVm(screen);
                if (vm == null)
                {
                    RequeueIfNeeded(screen, settingsId, nonce, attempt, maxAttempts);
                    return;
                }

                var entry = TryFindSettingsEntryById(vm, settingsId);
                if (entry == null)
                {
                    // Some people might register under mod id instead of settings id; fallback.
                    entry = TryFindSettingsEntryById(vm, "Retinues");
                }

                if (entry == null)
                {
                    RequeueIfNeeded(screen, settingsId, nonce, attempt, maxAttempts);
                    return;
                }

                // Critical: call ExecuteSelect outside of MCM's Add/ListChanged callstack.
                // We are already deferred via SynchronizationContext.Post, so it's safe here.
                Reflection.InvokeMethod(entry, "ExecuteSelect", Type.EmptyTypes);

                Log.Debug($"Selected MCM entry: {settingsId}");
            }
            catch (Exception ex)
            {
                // If selection throws during early init, retry a few times.
                if (attempt < maxAttempts)
                {
                    Requeue(screen, settingsId, nonce, attempt + 1);
                    return;
                }

                Log.Warning($"Failed to auto-select MCM entry '{settingsId}': {ex.GetType().Name}");
            }
        }

        /// <summary>
        /// Requeue the selection attempt if we haven't exceeded max attempts.
        /// </summary>
        private static void RequeueIfNeeded(
            ScreenBase screen,
            string settingsId,
            int nonce,
            int attempt,
            int maxAttempts
        )
        {
            if (attempt >= maxAttempts)
            {
                Log.Debug($"Could not auto-select MCM entry '{settingsId}' (timed out).");
                return;
            }

            Requeue(screen, settingsId, nonce, attempt + 1);
        }

        /// <summary>
        /// Requeue the selection attempt on the SynchronizationContext.
        /// </summary>
        private static void Requeue(ScreenBase screen, string settingsId, int nonce, int attempt)
        {
            var ctx = SynchronizationContext.Current;
            if (ctx == null)
            {
                TrySelectPoll(screen, settingsId, nonce, attempt);
                return;
            }

            ctx.Post(_ => TrySelectPoll(screen, settingsId, nonce, attempt), null);
        }

        /// <summary>
        /// Try to get the ModOptionsVM from the given ModOptionsGauntletScreen.
        /// </summary>
        private static object TryGetModOptionsVm(ScreenBase screen)
        {
            // ModOptionsGauntletScreen has: private ModOptionsVM _dataSource;
            if (screen == null)
                return null;

            try
            {
                if (!Reflection.HasField(screen, "_dataSource"))
                    return null;

                return Reflection.GetFieldValue<object>(screen, "_dataSource");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Try to find a SettingsEntryVM by its ID in the given ModOptionsVM.
        /// </summary>
        private static object TryFindSettingsEntryById(object modOptionsVm, string id)
        {
            if (modOptionsVm == null || string.IsNullOrWhiteSpace(id))
                return null;

            try
            {
                if (!Reflection.HasProperty(modOptionsVm, "ModSettingsList"))
                    return null;

                var listObj = Reflection.GetPropertyValue<object>(modOptionsVm, "ModSettingsList");
                var enumerable = listObj as IEnumerable;
                if (enumerable == null)
                    return null;

                var idLower = id.ToLowerInvariant();

                foreach (var it in enumerable)
                {
                    if (it == null)
                        continue;

                    // SettingsEntryVM has: public string Id { get; }
                    if (!Reflection.HasProperty(it, "Id"))
                        continue;

                    var entryId = Reflection.GetPropertyValue<object>(it, "Id") as string;
                    if (string.IsNullOrWhiteSpace(entryId))
                        continue;

                    if (entryId.ToLowerInvariant() == idLower)
                        return it;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Type Helpers                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Find a type by its full name, searching loaded assemblies if needed.
        /// </summary>
        private static Type FindType(string fullName)
        {
            // Fast path
            var t = Type.GetType(fullName, throwOnError: false);
            if (t != null)
                return t;

            // Scan loaded assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                    if (t != null)
                        return t;
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }

        /// <summary>
        /// Create an instance of the given type using the "best" available constructor.
        /// </summary>
        private static object CreateBest(Type t)
        {
            try
            {
                var ctors = t.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );
                if (ctors == null || ctors.Length == 0)
                    return Activator.CreateInstance(t);

                // Prefer parameterless
                var ctor0 = ctors.FirstOrDefault(c => c.GetParameters().Length == 0);
                if (ctor0 != null)
                    return ctor0.Invoke([]);

                // Prefer "logger" style ctor: ctor(ILogger<ModOptionsGauntletScreen> logger)
                var ctorLogger = ctors.FirstOrDefault(c =>
                {
                    var ps = c.GetParameters();
                    if (ps.Length != 1)
                        return false;

                    var pt = ps[0].ParameterType;
                    var n = pt.FullName ?? pt.Name;
                    return n.IndexOf(
                            "Microsoft.Extensions.Logging.ILogger",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;
                });

                if (ctorLogger != null)
                    return ctorLogger.Invoke([null]);

                // Otherwise smallest-arity ctor with default args
                var best = ctors.OrderBy(c => c.GetParameters().Length).FirstOrDefault();
                if (best == null)
                    return Activator.CreateInstance(t);

                var args = best.GetParameters()
                    .Select(p =>
                    {
                        if (p.ParameterType == typeof(bool))
                            return false;
                        if (p.ParameterType == typeof(int))
                            return 0;
                        if (p.ParameterType == typeof(float))
                            return 0f;
                        if (p.ParameterType == typeof(string))
                            return null;
                        if (p.ParameterType.IsValueType)
                            return Activator.CreateInstance(p.ParameterType);
                        return null;
                    })
                    .ToArray();

                return best.Invoke(args);
            }
            catch
            {
                // Last resort
                return Activator.CreateInstance(t);
            }
        }
    }
}
