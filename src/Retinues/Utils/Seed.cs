using System.Text;
using TaleWorlds.SaveSystem;

namespace Retinues.Utils
{
    public static class Seed
    {
        public static int FromString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            var bytes = Encoding.UTF8.GetBytes(s);
            const uint fnvOffset = 2166136261u;
            const uint fnvPrime = 16777619u;

            uint hash = fnvOffset;
            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= fnvPrime;
            }

            // make non-negative
            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
