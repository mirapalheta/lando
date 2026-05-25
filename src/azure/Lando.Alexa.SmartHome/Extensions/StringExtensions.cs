using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace System;

internal static class StringExtensions
{
    private static readonly Regex Sanitizer =
        new(@"[^a-zA-Z0-9 ]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    extension(string str)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToFriendlyName()
            => string.Join(' ', str.Split('_').Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Sanitize()
            => Sanitizer.Replace(str, string.Empty);
    }
}
