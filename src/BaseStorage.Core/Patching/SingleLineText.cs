namespace BaseStorage.Core.Patching;

using System;
using System.Text;

internal static class SingleLineText
{
    internal static string Of(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string value = text!;
        var builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
            {
                i++;
                builder.Append(' ');
            }
            else if (c == '\r' || c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Trim();
    }

    internal static string Describe(Exception error)
    {
        string type = error.GetType().FullName ?? error.GetType().Name;
        string message;
        try
        {
            message = Of(error.Message);
        }
        catch (Exception)
        {
            // A throwing Message getter must not break boot logging; treat it as an empty message.
            message = string.Empty;
        }

        return message.Length == 0 ? type : type + ": " + message;
    }

    internal static string DescribeWithInner(Exception error)
    {
        string text = Describe(error);
        return error.InnerException is null ? text : text + " ---> " + Describe(error.InnerException);
    }
}
