using System;
using System.Collections.Generic;
using System.Text;

namespace Retinues.Framework.Model.Attributes
{
    internal static class PersistenceLoadLog
    {
        [ThreadStatic]
        static Scope _current;

        internal sealed class Scope : IDisposable
        {
            readonly Scope _prev;
            readonly string _uid;
            readonly List<KeyValuePair<string, string>> _pairs = [];

            internal Scope(string uid)
            {
                _uid = uid ?? string.Empty;
                _prev = _current;
                _current = this;
            }

            public void Dispose()
            {
                _current = _prev;
            }

            internal void Add(string name, string value)
            {
                if (string.IsNullOrEmpty(name))
                    return;

                value ??= string.Empty;

                // Replace if already present (keeps order stable).
                for (int i = 0; i < _pairs.Count; i++)
                {
                    if (string.Equals(_pairs[i].Key, name, StringComparison.Ordinal))
                    {
                        _pairs[i] = new KeyValuePair<string, string>(name, value);
                        return;
                    }
                }

                _pairs.Add(new KeyValuePair<string, string>(name, value));
            }

            internal string BuildLine()
            {
                if (_pairs.Count == 0)
                    return $"[LOAD] {_uid}";

                var sb = new StringBuilder(256);
                sb.Append("[LOAD] ");
                sb.Append(_uid);

                for (int i = 0; i < _pairs.Count; i++)
                {
                    sb.Append(" | ");
                    sb.Append(_pairs[i].Key);
                    sb.Append('=');
                    sb.Append(_pairs[i].Value);
                }

                return sb.ToString();
            }
        }

        public static Scope Begin(string uid) => new Scope(uid);

        public static bool IsActive => _current != null;

        public static void Add(string name, string value)
        {
            _current?.Add(name, value);
        }

        public static string BuildCurrentLine()
        {
            return _current?.BuildLine();
        }
    }
}
