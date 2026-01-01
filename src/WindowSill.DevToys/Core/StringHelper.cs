using System.Text;
using Microsoft.Extensions.Logging;
using WindowSill.API;

namespace WindowSill.DevToys.Core;
internal class StringHelper
{
    private static readonly ILogger logger = typeof(StringHelper).Log();

    internal static string UnescapeString(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return string.Empty;
        }

        StringBuilder decoded = PooledStringBuilder.Instance.Get();

        try
        {
            int i = 0;
            while (i < data!.Length)
            {
                string replacementString = string.Empty;
                int jumpLength = 0;
                if (TextMatchAtIndex(data, "\\n", i))
                {
                    jumpLength = 2;
                    replacementString = "\n";
                }
                else if (TextMatchAtIndex(data, "\\r", i))
                {
                    jumpLength = 2;
                    replacementString = "\r";
                }
                else if (TextMatchAtIndex(data, "\\t", i))
                {
                    jumpLength = 2;
                    replacementString = "\t";
                }
                else if (TextMatchAtIndex(data, "\\b", i))
                {
                    jumpLength = 2;
                    replacementString = "\b";
                }
                else if (TextMatchAtIndex(data, "\\f", i))
                {
                    jumpLength = 2;
                    replacementString = "\f";
                }
                else if (TextMatchAtIndex(data, "\\\"", i))
                {
                    jumpLength = 2;
                    replacementString = "\"";
                }
                else if (TextMatchAtIndex(data, "\\\\", i))
                {
                    jumpLength = 2;
                    replacementString = "\\";
                }

                if (!string.IsNullOrEmpty(replacementString) && jumpLength > 0)
                {
                    decoded.Append(replacementString);
                    i += jumpLength;
                }
                else
                {
                    decoded.Append(data[i]);
                    i++;
                }
            }

            return decoded.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to escape text");
        }
        finally
        {
            PooledStringBuilder.Instance.Return(decoded);
        }

        return string.Empty;
    }

    private static bool TextMatchAtIndex(string data, string test, int startIndex)
    {
        if (string.IsNullOrEmpty(test))
        {
            return false;
        }

        if (data.Length < test.Length)
        {
            return false;
        }

        for (int i = 0; i < test.Length; i++)
        {
            if (data[startIndex + i] != test[i])
            {
                return false;
            }
        }

        return true;
    }
}
