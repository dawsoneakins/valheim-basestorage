namespace BaseStorage.Core.Tests.Menu;

using BaseStorage.Core.Menu;

/// <summary>GW-U-2540: MenuRect and MenuButtonSpec values, and the open-interior overlap (S-QR-002 contract 2.1, 2.4).</summary>
public sealed class MenuRectTests
{
    /// <summary>GW-U-2540: values compare by value, -0f is stored as +0f, Right and Bottom are sums, and the overlap is open.</summary>
    [Fact]
    public void MenuRectAndSpecAreValuesAndOverlapIsOpenInterior()
    {
        var a = new MenuRect(10f, 20f, 30f, 40f);
        var same = new MenuRect(10f, 20f, 30f, 40f);
        Assert.Equal(a, same);
        Assert.True(a == same);
        Assert.False(a != same);
        Assert.Equal(a.GetHashCode(), same.GetHashCode());
        Assert.False(a == new MenuRect(10f, 20f, 30f, 41f));
        Assert.True(a != new MenuRect(10f, 20f, 30f, 41f));
        Assert.Equal(40f, a.Right);
        Assert.Equal(60f, a.Bottom);

        var negativeZero = new MenuRect(-0f, -0f, 1f, 1f);
        Assert.False(float.IsNegative(negativeZero.X));
        Assert.False(float.IsNegative(negativeZero.Top));
        Assert.True(negativeZero == new MenuRect(0f, 0f, 1f, 1f));

        Assert.False(new MenuRect(0f, 0f, 10f, 10f).Overlaps(new MenuRect(10f, 0f, 10f, 10f)));
        Assert.True(new MenuRect(0f, 0f, 10f, 10f).Overlaps(new MenuRect(9f, 9f, 10f, 10f)));

        var spec = new MenuButtonSpec(150f, true);
        var sameSpec = new MenuButtonSpec(150f, true);
        Assert.Equal(spec, sameSpec);
        Assert.True(spec == sameSpec);
        Assert.True(spec != new MenuButtonSpec(150f, false));
        Assert.Equal(spec.GetHashCode(), sameSpec.GetHashCode());
    }
}
