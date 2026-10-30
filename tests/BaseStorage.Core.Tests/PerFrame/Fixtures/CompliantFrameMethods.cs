namespace BaseStorage.Core.Tests.PerFrame.Fixtures.Compliant;

using BaseStorage.Core.Bases.Resolution;

/// <summary>An Update that reads only the allowed BaseCache members.</summary>
public sealed class CheapUpdate
{
    private readonly BaseCache cache = null!;

    public void Update()
    {
        _ = this.cache.IsCurrent(default);
        _ = this.cache.Generation;
    }
}

/// <summary>A LateUpdate that calls a Tick, which reaches the resolver only indirectly.</summary>
public sealed class TicksInLateUpdate
{
    public void LateUpdate()
    {
        Tick();
    }

    public static void Tick()
    {
        _ = BaseResolver.Resolve(null!, null!, default, 0);
    }
}

/// <summary>A method whose name is not a frame method, so it is not audited.</summary>
public sealed class NotAFrameMethod
{
    private readonly BaseCache cache = null!;

    public void UpdateCache()
    {
        _ = this.cache.Resolve(default, default);
    }
}

/// <summary>A class with a nested type whose Update calls nothing forbidden.</summary>
public sealed class Outer
{
    /// <summary>A nested type with an Update that is in scope and compliant.</summary>
    public sealed class Inner
    {
        public void Update()
        {
        }
    }
}
