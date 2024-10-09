using System;

namespace CCM.Core.Helpers
{
    public static class StringExtensions
    {
        public static string Sanitize(this string input)
        {
            return input.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        }
    }
}
