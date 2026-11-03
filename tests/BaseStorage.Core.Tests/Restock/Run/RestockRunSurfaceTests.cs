namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Restock.Run;

/// <summary>GW-U-2616: the exported surface of Restock.Run and the unchanged five-type surface of Restock (S-QR-002 contract section 1 and 3).</summary>
public sealed class RestockRunSurfaceTests
{
    /// <summary>GW-U-2616: the Run namespace exports exactly the five types, the stop reason is an int 0..4, the result has no public constructor and only get-only properties, the text is static, and Restock still exports five types.</summary>
    [Fact]
    public void TheRestockRunSurfaceIsExactlyFiveTypes()
    {
        string[] exported = typeof(IRestockLayer).Assembly.GetExportedTypes()
            .Where(t => t.Namespace == "BaseStorage.Core.Restock.Run")
            .Select(t => t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "IRestockLayer", "RestockExecutor", "RestockRunResult", "RestockRunText", "RestockStopReason" }, exported);

        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(RestockStopReason)));
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, Enum.GetValues<RestockStopReason>().Select(value => (int)value).ToArray());

        Assert.Empty(typeof(RestockRunResult).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.All(typeof(RestockRunResult).GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.False(property.CanWrite));

        Assert.True(typeof(RestockRunText).IsAbstract && typeof(RestockRunText).IsSealed);

        int restockTypes = typeof(RestockPlan).Assembly.GetExportedTypes().Count(t => t.Namespace == "BaseStorage.Core.Restock");
        Assert.Equal(5, restockTypes);
    }
}
