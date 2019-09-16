﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/>
    /// </summary>
    public class ToDateTimeTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (TryParseDateTime(value, args, out var result))
            {
                transformed = result;
                return true;
            };

            transformed = value;

            return false;
        }

        public static bool TryParseDateTime(object value, string[] formats, out DateTime result)
        {
            return TryParseDateTime(value, formats, DateTimeStyles.None, out result);
        }

        public static bool TryParseDateTime(object value, string[] formats, DateTimeStyles dateTimeStyles, out DateTime result)
        {
            if (value == null)
            {
                result = default;
                return false;
            }

            var valueString = value
                .ToString()
                .SubstringBeforeNewLine();
            
            if (string.IsNullOrWhiteSpace(valueString))
            {
                result = default;
                return false;
            }

            if (formats == null || formats.Length == 0 || string.IsNullOrEmpty(formats[0]))
            {
                if (DateTime.TryParse(valueString, CultureInfo.InvariantCulture, dateTimeStyles, out result))
                {
                    return true;
                }
            }
            else
            {
                foreach (var format in formats)
                {
                    if (string.IsNullOrWhiteSpace(format)) continue;

                    var valueToFormat = valueString;

                    if (format.Contains(" d ") || 
                        format.Contains(" dd ") || 
                        format.StartsWith("d ") ||
                        format.StartsWith("dd "))
                    {
                        valueToFormat = Regex.Replace(valueToFormat, @"\b(\d+)(?:st|nd|rd|th)\b", "$1"); 
                    }

                    if (DateTime.TryParseExact(valueToFormat, format, CultureInfo.InvariantCulture, dateTimeStyles, out result))
                    {
                        return true;
                    }
                }
            }

            result = default;

            return false;
        }
    }
}
