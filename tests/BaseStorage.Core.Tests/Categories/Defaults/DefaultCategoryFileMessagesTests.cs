namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.IO;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.Tests.Categories;
using BaseStorage.Core.Tests.Patching;

/// <summary>AC-7 and AC-8 (contract 5.1-5.3): the exact first-run log lines and their argument checks.</summary>
public sealed class DefaultCategoryFileMessagesTests
{
    private const string P = @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\config\BaseStorage.categories.txt";

    // GW-U-456
    [Fact]
    public void WrittenFormatsTheItemCountWithoutGrouping()
    {
        Assert.Equal("Wrote default category file '" + P + "' with 612 item(s)", DefaultCategoryFileMessages.Written(P, 612));
        Assert.Equal("Wrote default category file '" + P + "' with 0 item(s)", DefaultCategoryFileMessages.Written(P, 0));
    }

    // GW-U-456
    [Fact]
    public void WrittenIsCultureFree()
    {
        CategoryFixtures.WithCulture("de-DE", () =>
        {
            Assert.Equal("Wrote default category file '" + P + "' with 1234 item(s)", DefaultCategoryFileMessages.Written(P, 1234));
        });
    }

    // GW-U-456
    [Fact]
    public void WrittenSingleLinesAPathWithALineFeedAndAllowsAnEmptyPath()
    {
        Assert.Equal("Wrote default category file 'a b' with 1 item(s)", DefaultCategoryFileMessages.Written("a\nb", 1));
        Assert.Equal("Wrote default category file '' with 1 item(s)", DefaultCategoryFileMessages.Written(string.Empty, 1));
    }

    // GW-U-456
    [Fact]
    public void WriteFailedFormatsTheExceptionTypeAndMessage()
    {
        string line = DefaultCategoryFileMessages.WriteFailed(P, new UnauthorizedAccessException("Access to the path is denied."));

        Assert.Equal(
            "Could not write default category file '" + P + "': System.UnauthorizedAccessException: Access to the path is denied.",
            line);
    }

    // GW-U-456
    [Fact]
    public void WriteFailedSingleLinesTheMessageAndAppendsTheInnerException()
    {
        var error = new IOException("Disk full.\r\nRetry later.", new InvalidOperationException("inner"));

        Assert.Equal(
            "Could not write default category file '" + P + "': System.IO.IOException: Disk full. Retry later. ---> System.InvalidOperationException: inner",
            DefaultCategoryFileMessages.WriteFailed(P, error));
    }

    // GW-U-456
    [Fact]
    public void WriteFailedTreatsAThrowingMessageGetterAsAnEmptyMessage()
    {
        var error = new MessageThrowingException();

        Assert.Equal(
            "Could not write default category file '" + P + "': " + typeof(MessageThrowingException).FullName,
            DefaultCategoryFileMessages.WriteFailed(P, error));
    }

    // GW-U-456
    [Fact]
    public void ArgumentChecksUseTheContractParameterNames()
    {
        Assert.Equal("itemCount", Assert.Throws<ArgumentOutOfRangeException>(() => DefaultCategoryFileMessages.Written(P, -1)).ParamName);
        Assert.Equal("error", Assert.Throws<ArgumentNullException>(() => DefaultCategoryFileMessages.WriteFailed(P, null!)).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentNullException>(() => DefaultCategoryFileMessages.Written(null!, 1)).ParamName);
    }

    // GW-U-457
    [Fact]
    public void KeptExistingNamesTheFileAndSaysItIsLeftUnchanged()
    {
        Assert.Equal(
            "Category file '" + P + "' already exists; left unchanged",
            DefaultCategoryFileMessages.KeptExisting(P));
    }

    // GW-U-457
    [Fact]
    public void KeptExistingSingleLinesCarriageReturnLineFeedAndLineSeparators()
    {
        Assert.Equal("Category file 'C:\\a b' already exists; left unchanged", DefaultCategoryFileMessages.KeptExisting("C:\\a\r\nb"));
        Assert.Equal("Category file 'a b' already exists; left unchanged", DefaultCategoryFileMessages.KeptExisting("a\u2028b"));
    }

    // GW-U-457
    [Fact]
    public void KeptExistingRejectsANullPathWithTheParameterName()
    {
        Assert.Equal("path", Assert.Throws<ArgumentNullException>(() => DefaultCategoryFileMessages.KeptExisting(null!)).ParamName);
    }

    // GW-U-457
    [Fact]
    public void NoLineContainsANewlineOrEndsWithAPeriod()
    {
        foreach (string path in new[] { P, "x\r\ny", "x\u2028y", "x\ny." })
        {
            string[] lines =
            {
                DefaultCategoryFileMessages.KeptExisting(path),
                DefaultCategoryFileMessages.Written(path, 3),
                DefaultCategoryFileMessages.WriteFailed(path, new IOException("boom")),
            };

            foreach (string line in lines)
            {
                Assert.False(line.Contains('\n'), line);
                Assert.False(line.Contains('\r'), line);
                Assert.False(line.EndsWith(".", StringComparison.Ordinal), line);
            }
        }
    }
}
