namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.Tests.Categories.Defaults;
using BaseStorage.Core.Tests.Diagnostics;
using BaseStorage.Core.Tests.Patching;
using BaseStorage.Plugin.Categories;

/// <summary>
/// AC-7 and AC-8 (contract 6, F1-F8, ADR-GWIN-028, ADR-GWIN-029): the first-run writer, tested on disk through the linked source.
/// Every test works in its own folder under the OS temp directory. Nothing touches a real BepInEx folder.
/// </summary>
public sealed class DefaultCategoryFileWriterTests
{
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private static readonly DateTime Stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static IEnumerable<object[]> ExistingContents()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new object[] { i };
        }
    }

    // GW-U-461
    [Fact]
    public void WritesTheGeneratedBytesWhenTheFileIsMissing()
    {
        using var temp = new CategoryTempFolder();
        var sink = new RecordingLogSink();
        CategoryItem[] items = DefaultCategoryFixtures.SItems();

        DefaultCategoryFileWriter.WriteIfMissing(temp.Root, items, sink);

        string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);
        Assert.True(File.Exists(path));
        byte[] bytes = File.ReadAllBytes(path);
        Assert.Equal(Utf8.GetBytes(DefaultCategoryFile.Generate(items)), bytes);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "The file starts with a byte order mark.");
        Assert.DoesNotContain((byte)0x0D, bytes);
        Assert.Equal(
            DefaultCategoryFixtures.Normalise(DefaultCategoryFixtures.ExpectedS),
            DefaultCategoryFixtures.Normalise(Utf8.GetString(bytes)));
        Assert.Equal(
            new[] { (LogSeverity.Info, "Wrote default category file '" + path + "' with 26 item(s)") },
            sink.Entries);
    }

    // GW-U-462
    [Fact]
    public void CreatesAMissingConfigFolderAndWritesTheSameContent()
    {
        using var temp = new CategoryTempFolder();
        string folder = Path.Combine(temp.Root, "a", "b", "config");
        var sink = new RecordingLogSink();

        DefaultCategoryFileWriter.WriteIfMissing(folder, DefaultCategoryFixtures.SItems(), sink);

        string path = Path.Combine(folder, DefaultCategoryFile.FileName);
        Assert.True(Directory.Exists(folder));
        Assert.Equal(Utf8.GetBytes(DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems())), File.ReadAllBytes(path));
        Assert.Equal(new[] { (LogSeverity.Info, "Wrote default category file '" + path + "' with 26 item(s)") }, sink.Entries);

        string emptyFolder = Path.Combine(temp.Root, "empty");
        var emptySink = new RecordingLogSink();
        DefaultCategoryFileWriter.WriteIfMissing(emptyFolder, Array.Empty<CategoryItem>(), emptySink);

        string emptyPath = Path.Combine(emptyFolder, DefaultCategoryFile.FileName);
        Assert.Equal(
            DefaultCategoryFixtures.Normalise(DefaultCategoryFixtures.ExpectedEmpty),
            DefaultCategoryFixtures.Normalise(File.ReadAllText(emptyPath)));
        Assert.Equal(new[] { (LogSeverity.Info, "Wrote default category file '" + emptyPath + "' with 0 item(s)") }, emptySink.Entries);
    }

    // GW-U-463 (a)
    [Fact]
    public void AnExistingFileAsConfigFolderIsAWarningAndNeverThrows()
    {
        using var temp = new CategoryTempFolder();
        string blocker = Path.Combine(temp.Root, "notAFolder");
        File.WriteAllText(blocker, "x");
        string path = Path.Combine(blocker, DefaultCategoryFile.FileName);
        var sink = new RecordingLogSink();

        Assert.Null(Record.Exception(() => DefaultCategoryFileWriter.WriteIfMissing(blocker, DefaultCategoryFixtures.SItems(), sink)));

        Assert.False(File.Exists(path));
        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Warning, entry.Severity);
        Assert.StartsWith("Could not write default category file '" + path + "': System.IO.IOException", entry.Message);
    }

    // GW-U-463 (b)
    [Fact]
    public void AFailureAfterAPartialWriteLeavesNoFileAndWarnsOnce()
    {
        using var temp = new CategoryTempFolder();
        string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);
        var sink = new RecordingLogSink();
        Action<Stream, byte[]> halfThenFail = (stream, bytes) =>
        {
            stream.Write(bytes, 0, bytes.Length / 2);
            throw new IOException("disk full");
        };

        Assert.Null(Record.Exception(() => DefaultCategoryFileWriter.WriteIfMissing(temp.Root, DefaultCategoryFixtures.SItems(), sink, halfThenFail)));

        Assert.False(File.Exists(path));
        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Warning, entry.Severity);
        Assert.Contains("System.IO.IOException: disk full", entry.Message);
    }

    // GW-U-463 (c)
    [Fact]
    public void AThrowingLogSinkOnSuccessDoesNotThrowOrDeleteTheFinishedFile()
    {
        using var temp = new CategoryTempFolder();
        string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);

        Assert.Null(Record.Exception(() => DefaultCategoryFileWriter.WriteIfMissing(temp.Root, DefaultCategoryFixtures.SItems(), new ThrowingLogSink())));

        Assert.Equal(Utf8.GetBytes(DefaultCategoryFile.Generate(DefaultCategoryFixtures.SItems())), File.ReadAllBytes(path));
    }

    // GW-U-463 (d)
    [Fact]
    public void AThrowingLogSinkWithAFailingWriteDoesNotThrowAndLeavesNoFile()
    {
        using var temp = new CategoryTempFolder();
        string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);
        Action<Stream, byte[]> fail = (stream, bytes) => throw new IOException("disk full");

        Assert.Null(Record.Exception(() => DefaultCategoryFileWriter.WriteIfMissing(temp.Root, DefaultCategoryFixtures.SItems(), new ThrowingLogSink(), fail)));

        Assert.False(File.Exists(path));
    }

    // GW-U-464
    [Fact]
    public void AGeneratorFailureCreatesNoFolderNoFileAndWarns()
    {
        using var temp = new CategoryTempFolder();
        string folder = Path.Combine(temp.Root, "missing");
        var sink = new RecordingLogSink();
        var items = new List<CategoryItem> { new CategoryItem("Wood", ItemKind.Other), null! };

        Assert.Null(Record.Exception(() => DefaultCategoryFileWriter.WriteIfMissing(folder, items, sink)));

        Assert.False(Directory.Exists(folder));
        Assert.False(File.Exists(Path.Combine(folder, DefaultCategoryFile.FileName)));
        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Warning, entry.Severity);
        Assert.Contains("System.ArgumentException", entry.Message);
    }

    // GW-U-465
    [Theory]
    [MemberData(nameof(ExistingContents))]
    public void AnExistingFileIsNeverModifiedAndTheSeamIsNeverCalled(int which)
    {
        foreach (bool debug in new[] { true, false })
        {
            using var temp = new CategoryTempFolder();
            string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);
            byte[] original = ExistingBytes(which);
            File.WriteAllBytes(path, original);
            File.SetLastWriteTimeUtc(path, Stamp);
            var sink = new RecordingLogSink { DebugEnabled = debug };
            bool seamCalled = false;
            Action<Stream, byte[]> seam = (stream, bytes) => seamCalled = true;

            DefaultCategoryFileWriter.WriteIfMissing(temp.Root, DefaultCategoryFixtures.SItems(), sink, seam);

            Assert.False(seamCalled);
            Assert.Equal(original, File.ReadAllBytes(path));
            Assert.Equal(Stamp, File.GetLastWriteTimeUtc(path));
            Assert.Single(Directory.GetFiles(temp.Root));
            if (debug)
            {
                Assert.Equal(new[] { (LogSeverity.Debug, "Category file '" + path + "' already exists; left unchanged") }, sink.Entries);
            }
            else
            {
                Assert.DoesNotContain(sink.Entries, entry => entry.Severity is LogSeverity.Info or LogSeverity.Warning);
            }
        }
    }

    // GW-U-466 (a)
    [Fact]
    public void ADirectoryWithTheFileNameIsLeftAloneAndWarns()
    {
        using var temp = new CategoryTempFolder();
        string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);
        Directory.CreateDirectory(path);
        string inner = Path.Combine(path, "inner.txt");
        File.WriteAllText(inner, "keep");
        var sink = new RecordingLogSink();

        Assert.Null(Record.Exception(() => DefaultCategoryFileWriter.WriteIfMissing(temp.Root, DefaultCategoryFixtures.SItems(), sink)));

        Assert.True(Directory.Exists(path));
        Assert.Equal("keep", File.ReadAllText(inner));
        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Warning, entry.Severity);
    }

    // GW-U-466 (b)
    [Fact]
    public void AReadOnlyExistingFileKeepsItsBytesAttributesAndTimestamp()
    {
        using var temp = new CategoryTempFolder();
        string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);
        byte[] original = Utf8.GetBytes("Wood: Wood\n");
        File.WriteAllBytes(path, original);
        File.SetLastWriteTimeUtc(path, Stamp);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            var sink = new RecordingLogSink();

            DefaultCategoryFileWriter.WriteIfMissing(temp.Root, DefaultCategoryFixtures.SItems(), sink);

            Assert.Equal(original, File.ReadAllBytes(path));
            Assert.Equal(FileAttributes.ReadOnly, File.GetAttributes(path) & FileAttributes.ReadOnly);
            Assert.Equal(Stamp, File.GetLastWriteTimeUtc(path));
            Assert.DoesNotContain(sink.Entries, entry => entry.Severity == LogSeverity.Warning);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    // GW-U-466 (c)
    [Fact]
    public void ASecondCallLeavesTheFirstFileAndLogsKeptExisting()
    {
        using var temp = new CategoryTempFolder();
        string path = Path.Combine(temp.Root, DefaultCategoryFile.FileName);
        DefaultCategoryFileWriter.WriteIfMissing(temp.Root, DefaultCategoryFixtures.SItems(), new RecordingLogSink());
        byte[] first = File.ReadAllBytes(path);
        DateTime firstStamp = File.GetLastWriteTimeUtc(path);
        var sink = new RecordingLogSink { DebugEnabled = true };

        DefaultCategoryFileWriter.WriteIfMissing(temp.Root, Array.Empty<CategoryItem>(), sink);

        Assert.Equal(first, File.ReadAllBytes(path));
        Assert.Equal(firstStamp, File.GetLastWriteTimeUtc(path));
        Assert.Equal(new[] { (LogSeverity.Debug, "Category file '" + path + "' already exists; left unchanged") }, sink.Entries);
    }

    // GW-U-467
    [Fact]
    public void PathForIsPathCombineOfTheFileName()
    {
        using var temp = new CategoryTempFolder();
        string plain = Path.Combine(temp.Root, "cfg");
        string spaced = Path.Combine(temp.Root, "with space", "config");
        string trailing = Path.GetTempPath();

        foreach (string folder in new[] { plain, spaced, trailing })
        {
            Assert.Equal(Path.Combine(folder, DefaultCategoryFile.FileName), DefaultCategoryFileWriter.PathFor(folder));
        }
    }

    // GW-U-467
    [Fact]
    public void InfoDebugAndWarningLinesCarryThePathFor()
    {
        using var temp = new CategoryTempFolder();
        string folder = Path.Combine(temp.Root, "cfg");
        string path = DefaultCategoryFileWriter.PathFor(folder);

        var info = new RecordingLogSink();
        DefaultCategoryFileWriter.WriteIfMissing(folder, DefaultCategoryFixtures.SItems(), info);
        Assert.Equal(new[] { (LogSeverity.Info, "Wrote default category file '" + path + "' with 26 item(s)") }, info.Entries);

        var debug = new RecordingLogSink { DebugEnabled = true };
        DefaultCategoryFileWriter.WriteIfMissing(folder, DefaultCategoryFixtures.SItems(), debug);
        Assert.Equal(new[] { (LogSeverity.Debug, "Category file '" + path + "' already exists; left unchanged") }, debug.Entries);

        string blocked = Path.Combine(temp.Root, "blocked");
        File.WriteAllText(blocked, "x");
        var warn = new RecordingLogSink();
        DefaultCategoryFileWriter.WriteIfMissing(blocked, DefaultCategoryFixtures.SItems(), warn);
        (LogSeverity Severity, string Message) entry = Assert.Single(warn.Entries);
        Assert.StartsWith("Could not write default category file '" + DefaultCategoryFileWriter.PathFor(blocked) + "': ", entry.Message);
    }

    private static byte[] ExistingBytes(int which) => which switch
    {
        0 => Utf8.GetBytes("Wood: Wood\n# mine\n"),
        1 => Array.Empty<byte>(),
        2 => Utf8.GetBytes("Wood: Wood\r\nMisc: Coins\r\n"),
        3 => new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Utf8.GetBytes("Wood: Wood\n")).ToArray(),
        _ => RandomBytes(),
    };

    private static byte[] RandomBytes()
    {
        var bytes = new byte[64];
        new Random(20261026).NextBytes(bytes);
        return bytes;
    }

    /// <summary>A unique folder under the OS temp directory, deleted on Dispose on a best-effort basis (ADR-GWIN-029).</summary>
    private sealed class CategoryTempFolder : IDisposable
    {
        public CategoryTempFolder()
        {
            Root = Path.Combine(Path.GetTempPath(), "bs-cat-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
