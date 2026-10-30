namespace BaseStorage.Core.Networking;

using System;

/// <summary>
/// The mod version of this build and the version grammar (contract version-handshake-contracts.md 2.1). The version is the
/// <c>Version.props</c> value captured at build time into a generated constant; nothing is read at runtime (rule V1).
/// </summary>
public static class ModVersion
{
    private const int Parts = 3;
    private const int MaxDigitsPerPart = 9;

    /// <summary>Gets the mod version this assembly was built with, taken from Version.props at build time. Always well formed.</summary>
    public static string Current => CoreBuildInfo.Version;

    /// <summary>
    /// Determines whether the text is a version of the form MAJOR.MINOR.PATCH (grammar V2): three ASCII-digit parts of 1 to 9 digits
    /// each, no leading zero, no sign, no whitespace and no suffix. Never throws.
    /// </summary>
    /// <param name="version">The text to check; null gives false.</param>
    /// <returns>True when the text is a well-formed version.</returns>
    public static bool IsWellFormed(string? version)
    {
        if (version is null)
        {
            return false;
        }

        int length = version.Length;
        int i = 0;
        for (int part = 0; part < Parts; part++)
        {
            if (part > 0)
            {
                if (i >= length || version[i] != '.')
                {
                    return false;
                }

                i++;
            }

            int start = i;
            while (i < length && version[i] >= '0' && version[i] <= '9')
            {
                i++;
            }

            int digits = i - start;
            if (digits == 0 || digits > MaxDigitsPerPart || (digits > 1 && version[start] == '0'))
            {
                return false;
            }
        }

        return i == length;
    }

    /// <summary>Validates a version argument: null throws ArgumentNullException, an ill-formed value throws ArgumentException.</summary>
    /// <param name="version">The argument value.</param>
    /// <param name="paramName">The C# parameter name reported by the exception.</param>
    /// <returns>The version, unchanged.</returns>
    internal static string Require(string? version, string paramName)
    {
        if (version is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (!IsWellFormed(version))
        {
            throw new ArgumentException("The version is not of the form MAJOR.MINOR.PATCH.", paramName);
        }

        return version;
    }
}
