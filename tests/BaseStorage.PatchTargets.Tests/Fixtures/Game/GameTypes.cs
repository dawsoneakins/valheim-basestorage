namespace BaseStorage.PatchTargets.Tests.Fixtures.Game;

/// <summary>Base class for the DeclaredOnly fixture: a method that only this class declares (contract 4.6.2).</summary>
public class Humanoid
{
    public void InheritedHelper()
    {
    }
}

/// <summary>Minimal stand-in for the game Player with both HaveRequirements overloads (contract 4.3).</summary>
public class Player : Humanoid
{
    public enum RequirementMode
    {
        CanBuild = 0,
        IsKnown = 1,
        CanAlmostBuild = 2,
    }

    public bool HaveRequirements(Recipe recipe, bool craft, int qualityLevel, int amount) => false;

    public bool HaveRequirements(Piece piece, RequirementMode mode) => false;
}

/// <summary>Fixture recipe type.</summary>
public sealed class Recipe
{
}

/// <summary>Fixture piece type.</summary>
public sealed class Piece
{
}

/// <summary>Fixture Container with the four RPC handlers and the two size fields (contract 4.3, 4.6.3).</summary>
public class Container
{
    public int m_width;

    public int m_height;

    public void RPC_RequestOpen(long a, long b)
    {
    }

    public void RPC_OpenResponse(long a, bool b)
    {
    }

    public void RPC_RequestStack(long a, long b)
    {
    }

    public void RPC_StackResponse(long a, bool b)
    {
    }

    private static void HiddenHelper()
    {
    }
}

/// <summary>Fixture CraftingStation with the build range field (contract 4.3).</summary>
public class CraftingStation
{
    public float m_rangeBuild;
}
