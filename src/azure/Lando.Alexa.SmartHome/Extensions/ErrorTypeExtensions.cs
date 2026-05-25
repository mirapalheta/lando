using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Lando.Alexa.SmartHome.Models.ErrorResponse;

internal static class ErrorTypeExtensions
{
    private static readonly ConcurrentDictionary<ErrorType, string> ErrorTypeNames = new();

    public static string ToErrorCode(this ErrorType errorType)
        => ErrorTypeNames.GetOrAdd(errorType, GetErrorCode);

    private static string GetErrorCode(ErrorType value)
        => typeof(ErrorType).GetMember(value.ToString()) is MemberInfo[] members && members.Length > 0
            ? members[0].GetCustomAttribute<DescriptionAttribute>(false)?.Description ?? value.ToString()
            : value.ToString();
}
