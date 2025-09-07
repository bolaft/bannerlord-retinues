using System;
using System.Linq;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using Retinues.Core.Utils;

namespace Retinues.MCM
{
    public static class SettingsBootstrap
    {
        private const string Id = "Retinues.Core.Settings";
        private const string DisplayName = "Retinues";
        private const string FolderName = "Retinues.Core";
        private const string FormatType = "xml";

        public static bool Register()
        {
            try
            {
                var create = BaseSettingsBuilder.Create(Id, DisplayName);

                // MCM may not be loaded or adapter not ready
                if (create is null)
                    return false;

                var builder = create.SetFolderName(FolderName).SetFormat(FormatType);
                if (builder is null)
                    return false;

                var options = Config.Options;
                if (options == null || options.Count == 0)
                {
                    // nothing to build (or Config not initialized yet)
                    return false;
                }

                foreach (var section in options.GroupBy(o => string.IsNullOrWhiteSpace(o.Section) ? "General" : o.Section))
                {
                    var sectionName = section.Key; // never null/empty due to GroupBy key above

                    builder.CreateGroup(sectionName, group =>
                    {
                        int order = 0;

                        foreach (var opt in section)
                        {
                            // Defensive defaults
                            var id = string.IsNullOrWhiteSpace(opt.Key) ? Guid.NewGuid().ToString("N") : opt.Key;
                            var name = string.IsNullOrWhiteSpace(opt.Name) ? id : opt.Name;
                            var hint = opt.Hint ?? string.Empty;
                            var type = opt.Type ?? typeof(string);
                            var def = opt.Default;
                            var min = opt.MinValue;
                            var max = opt.MaxValue;

                            try
                            {
                                if (type == typeof(bool))
                                {
                                    group.AddBool(
                                        id,
                                        name,
                                        new ProxyRef<bool>(
                                            () => Config.GetOption(id, def is bool b && b),
                                            v => Config.SetOption(id, v, save: true)
                                        ),
                                        b => b.SetOrder(order++).SetHintText(hint).SetRequireRestart(false)
                                    );
                                }
                                else if (type == typeof(int))
                                {
                                    group.AddInteger(
                                        id,
                                        name,
                                        min, max,
                                        new ProxyRef<int>(
                                            () => Config.GetOption(id, def is int i ? i : 0),
                                            v => Config.SetOption(id, v, save: true)
                                        ),
                                        b => b.SetOrder(order++).SetHintText(hint).SetRequireRestart(false)
                                    );
                                }
                                else if (type == typeof(string))
                                {
                                    group.AddText(
                                        id,
                                        name,
                                        new ProxyRef<string>(
                                            () => Config.GetOption<string>(id, def as string ?? string.Empty),
                                            v => Config.SetOption(id, v, save: true)
                                        ),
                                        b => b.SetOrder(order++).SetHintText(hint).SetRequireRestart(false)
                                    );
                                }
                            }
                            catch
                            {
                                // Skip bad entry
                            }
                        }
                    });
                }

                var settings = builder.BuildAsGlobal();
                if (settings is null)
                    return false;

                settings.Register();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
