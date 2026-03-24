using System;
using System.Collections.Generic;
using System.Text;

namespace TimetrackerReportingClient.Extensions;

public static class StringExtension
{
    public static string EnsureStringIsNotNullOrEmpty(this string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            throw new ArgumentException("String cannot be null or empty.", nameof(parameter));
        }
        return parameter;
    }
}
