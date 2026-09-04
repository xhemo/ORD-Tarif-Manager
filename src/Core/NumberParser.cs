using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OrdTarifManager.Core
{
    public static class NumberParser
    {
        public static double CleanNumber(string valStr)
        {
            if (string.IsNullOrWhiteSpace(valStr))
            {
                return 0.0;
            }

            valStr = valStr.Trim();

            // Match first contiguous number group with optional leading sign and optional dot or comma
            Match match = Regex.Match(valStr, @"([+-]?)\s*([0-9]+(?:[.,][0-9]+)*)");
            if (!match.Success)
            {
                return 0.0;
            }

            bool isNegative = match.Groups[1].Value == "-";
            string numStr = match.Groups[2].Value;

            // Heuristic for thousands and decimal separators
            if (numStr.Contains(",") && numStr.Contains("."))
            {
                // Format like 1.234,56 -> Dot is thousands, comma is decimal
                numStr = numStr.Replace(".", "").Replace(",", ".");
            }
            else if (numStr.Contains(","))
            {
                // Single comma -> German decimal separator (e.g. 123,45 -> 123.45)
                numStr = numStr.Replace(",", ".");
            }
            else if (numStr.Contains("."))
            {
                // Only dot: check if it represents thousands (e.g. 1.000 or 10.500)
                string[] parts = numStr.Split('.');
                if (parts.Length > 1)
                {
                    bool isThousands = true;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (parts[i].Length != 3)
                        {
                            isThousands = false;
                            break;
                        }
                    }

                    if (isThousands)
                    {
                        numStr = numStr.Replace(".", "");
                    }
                }
            }

            double result;
            if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return isNegative ? -result : result;
            }

            return 0.0;
        }

        public static bool TryParseClean(string valStr, out double result)
        {
            result = 0.0;
            if (string.IsNullOrWhiteSpace(valStr))
            {
                return false;
            }

            valStr = valStr.Replace("\u20AC", "").Replace("€", "").Trim();
            Match match = Regex.Match(valStr, @"^[+-]?[0-9]+(?:[.,][0-9]+)*$");
            if (!match.Success)
            {
                return false;
            }

            result = CleanNumber(valStr);
            return true;
        }

        public static string FormatNumber(double val, bool isIdOrInt = false)
        {
            var deCulture = new CultureInfo("de-DE");
            if (isIdOrInt || Math.Abs(val % 1) < 0.0000001)
            {
                if (isIdOrInt)
                {
                    return ((long)val).ToString("#,##0", deCulture);
                }
                return val.ToString("#,##0.00", deCulture);
            }

            return val.ToString("#,##0.00", deCulture);
        }

        public static string FormatDisplay(object val, string colName)
        {
            if (val == null || val == DBNull.Value)
            {
                return "";
            }

            bool isId = colName != null && colName.StartsWith("id_");

            if (val is double || val is float || val is decimal)
            {
                double d = Convert.ToDouble(val);
                if (isId)
                {
                    return ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture);
                }
                return d.ToString("0.00", CultureInfo.InvariantCulture);
            }

            if (val is int || val is long || val is short || val is byte)
            {
                long l = Convert.ToInt64(val);
                if (isId)
                {
                    return l.ToString(CultureInfo.InvariantCulture);
                }
                return ((double)l).ToString("0.00", CultureInfo.InvariantCulture);
            }

            string str = val.ToString().Trim();
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }

            double cleaned = CleanNumber(str);
            if (isId)
            {
                return ((long)Math.Round(cleaned)).ToString(CultureInfo.InvariantCulture);
            }
            return cleaned.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static bool IsPriceColumn(string colName)
        {
            if (string.IsNullOrEmpty(colName)) return false;
            return colName.IndexOf("price", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("preis", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("kosten", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("cost", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsNoDecimalColumn(string colName)
        {
            if (string.IsNullOrEmpty(colName)) return false;

            if (IsPriceColumn(colName)) return false;

            return colName.IndexOf("distance", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("distanz", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("weight", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("gewicht", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("volume", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("volumen", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("cbm", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("km", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("min", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("max", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("menge", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("qty", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("quantity", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("anzahl", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("colli", StringComparison.OrdinalIgnoreCase) >= 0
                || colName.IndexOf("palette", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string FormatValueForDisplay(object rawVal, string colName)
        {
            if (rawVal == null || rawVal == DBNull.Value) return "";
            string s = rawVal.ToString().Trim();
            if (string.IsNullOrEmpty(s)) return "";

            double d;
            if (!TryParseClean(s, out d))
            {
                return s;
            }

            var deCulture = new CultureInfo("de-DE");

            if (IsPriceColumn(colName))
            {
                return d.ToString("#,##0.00", deCulture) + " \u20AC";
            }

            if (IsNoDecimalColumn(colName))
            {
                return ((long)Math.Round(d)).ToString("#,##0", deCulture);
            }

            if (colName != null && colName.StartsWith("id_"))
            {
                return ((long)Math.Round(d)).ToString("#,##0", deCulture);
            }

            return d.ToString("#,##0.00", deCulture);
        }

        public static string ParseValueFromInput(string input, string colName)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            double d = CleanNumber(input);

            if (colName != null && colName.StartsWith("id_"))
            {
                return ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture);
            }

            // Always store as invariant standard 2-decimal format for XML export compatibility
            return d.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
