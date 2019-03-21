using System.Linq;
using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Sharp7
{
    public static class PlcResult
    {
        public const string Unknown = "<unknown>";

        public static string GetResultText(int resultCode)
        {
            var codes = typeof(global::Sharp7.S7Consts)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(fi => fi.Name.StartsWith("err"));

            foreach (var fieldInfo in codes)
            {
                var value = (int)fieldInfo.GetRawConstantValue();
                if (value == resultCode)
                {
                    return fieldInfo.Name;
                }
            }

            return Unknown;
        }
    }
}

