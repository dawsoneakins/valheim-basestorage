namespace BaseStorage.PatchTargets.Tests.Fixtures;

/// <summary>
/// List types shaped like PatchTargetNames (contract 4.5). Each nested class is a list type that a shape, checker or
/// reader test enumerates by reflection. These are fixtures, not rows. Plain names are used where only the shape is
/// checked; G-prefixed names point at the fixture game types in Fixtures.Game.
/// </summary>
internal static class FixtureLists
{
    private const string G = "BaseStorage.PatchTargets.Tests.Fixtures.Game.";
    private const string RecipeParams = G + "Recipe,System.Boolean,System.Int32,System.Int32";
    private const string PieceParams = G + "Piece," + G + "Player+RequirementMode";

    /// <summary>The valid v2 list (contract 4.3, 16 constants) against the fixture game types.</summary>
    public static class Valid
    {
        public static class Player
        {
            public const string TypeName = G + "Player";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParametersRecipe = RecipeParams;
            public const string HaveRequirementsParametersPiece = PieceParams;
        }

        public static class Container
        {
            public const string TypeName = G + "Container";
            public const string RpcRequestOpen = "RPC_RequestOpen";
            public const string RpcOpenResponse = "RPC_OpenResponse";
            public const string RpcRequestStack = "RPC_RequestStack";
            public const string RpcStackResponse = "RPC_StackResponse";
            public const string FieldWidth = "m_width";
            public const string FieldWidthType = "System.Int32";
            public const string FieldHeight = "m_height";
            public const string FieldHeightType = "System.Int32";
        }

        public static class CraftingStation
        {
            public const string TypeName = G + "CraftingStation";
            public const string FieldRangeBuild = "m_rangeBuild";
            public const string FieldRangeBuildType = "System.Single";
        }

        public static class HarmonyPatched
        {
            public const string PlayerHaveRequirementsRecipe = G + "Player.HaveRequirements(" + RecipeParams + ")";
            public const string PlayerHaveRequirementsPiece = G + "Player.HaveRequirements(" + PieceParams + ")";
        }
    }

    /// <summary>An owner with no TypeName (BST2015 owner rule).</summary>
    public static class NoTypeName
    {
        public static class Player
        {
            public const string HaveRequirements = "HaveRequirements";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>An empty TypeName value.</summary>
    public static class EmptyValue
    {
        public static class Container
        {
            public const string TypeName = "";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>A TypeName with a space.</summary>
    public static class SpaceValue
    {
        public static class Container
        {
            public const string TypeName = "Con tainer";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>A TypeName with a tab.</summary>
    public static class TabValue
    {
        public static class Container
        {
            public const string TypeName = "Con\ttainer";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>A field entry with no Type companion.</summary>
    public static class FieldNoType
    {
        public static class Container
        {
            public const string TypeName = "Container";
            public const string FieldWidth = "m_width";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>Two owners with the same TypeName; the ordinally later class is reported.</summary>
    public static class DuplicateType
    {
        public static class Container
        {
            public const string TypeName = "Shared";
        }

        public static class CraftingStation
        {
            public const string TypeName = "Shared";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>Two overload companions with equal values; the ordinally later companion is reported.</summary>
    public static class DuplicateCompanion
    {
        public static class Player
        {
            public const string TypeName = "Player";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParametersPiece = "Piece,Player+RequirementMode";
            public const string HaveRequirementsParametersRecipe = "Piece,Player+RequirementMode";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>A parameterless overload companion, which is allowed to be empty.</summary>
    public static class ParameterlessOverload
    {
        public static class Player
        {
            public const string TypeName = "Player";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParametersNone = "";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>No HarmonyPatched class at all.</summary>
    public static class HarmonyMissing
    {
        public static class Player
        {
            public const string TypeName = "Player";
        }
    }

    /// <summary>HarmonyPatched values that are not valid compositions (contract 4.5.2, 4.5.3).</summary>
    public static class HarmonyBad
    {
        public static class Player
        {
            public const string TypeName = "Player";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParametersRecipe = "Recipe,System.Boolean,System.Int32,System.Int32";
            public const string HaveRequirementsParametersPiece = "Piece,Player+RequirementMode";
        }

        public static class Container
        {
            public const string TypeName = "Container";
            public const string RpcRequestOpen = "RPC_RequestOpen";
        }

        public static class HarmonyPatched
        {
            public const string PlayerUndeclared = "Player.HaveRequirements(Unknown)";
            public const string PlayerDotNested = "Player.HaveRequirements(Piece,Player.RequirementMode)";
            public const string ContainerNoParens = "Container.RPC_RequestOpen";
            public const string PlayerSpaceAfterComma = "Player.HaveRequirements(Recipe, System.Boolean,System.Int32,System.Int32)";
        }
    }

    /// <summary>Two violations at once; the lines come back sorted ordinally.</summary>
    public static class TwoViolations
    {
        public static class Container
        {
            public const string TypeName = "";
            public const string FieldWidth = "m_width";
        }

        public static class HarmonyPatched
        {
        }
    }

    /// <summary>Two HarmonyPatched constants with equal values; the ordinally later constant is reported.</summary>
    public static class HarmonyDuplicate
    {
        public static class Player
        {
            public const string TypeName = "Player";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParametersRecipe = "Recipe,System.Boolean,System.Int32,System.Int32";
        }

        public static class HarmonyPatched
        {
            public const string PlayerHaveRequirementsRecipe = "Player.HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32)";
            public const string PlayerHaveRequirementsRecipeAgain = "Player.HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32)";
        }
    }

    /// <summary>Classification edge cases (contract 4.5.1 rules 2 to 5 and the non-const fields).</summary>
    public static class RuleEdges
    {
        public static class Edges
        {
            public const string TypeName = "Edges";
            public const string FieldOrphanType = "System.Int32";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParameters = "HaveRequirementsParameters";
            public const string FieldWidth = "m_width";
            public const string FieldWidthType = "System.Int32";
            public const string FieldWidthParametersX = "System.Int32";
            public const string TypeNameParametersX = "Foo";
            public const string Sized = "Sized";
            public const string SizedParametersA = "System.Int32";
            public static readonly string Ro = "Ro";
            public const int Num = 1;
        }
    }

    /// <summary>The v1 values that the installed build does not declare (contract 4.6.3 negative fixtures).</summary>
    public static class V1Negative
    {
        public static class Player
        {
            public const string TypeName = "Player";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParametersPieceRequirements = "Piece+Requirement[],System.Boolean,System.Int32,System.Int32";
            public const string HaveRequirementsParametersPiece = "Piece,Player.RequirementMode";
        }

        public static class Container
        {
            public const string TypeName = "Container";
            public const string RpcOpenRespons = "RPC_OpenRespons";
        }
    }

    /// <summary>One broken entry per checker failure (BST2010 to BST2014, DeclaredOnly, constructors, non-public members).</summary>
    public static class CheckerCases
    {
        public static class BadType
        {
            public const string TypeName = G + "Containr";
            public const string RpcRequestOpen = "RPC_RequestOpen";
        }

        public static class BadName
        {
            public const string TypeName = G + "Container";
            public const string RpcMissing = "RPC_Missing";
        }

        public static class Overloaded
        {
            public const string TypeName = G + "Player";
            public const string HaveRequirements = "HaveRequirements";
        }

        public static class BadOverload
        {
            public const string TypeName = G + "Player";
            public const string HaveRequirements = "HaveRequirements";
            public const string HaveRequirementsParametersPiece = G + "Piece," + G + "Player.RequirementMode";
            public const string HaveRequirementsParametersRecipe = RecipeParams;
        }

        public static class NoMethod
        {
            public const string TypeName = G + "Container";
            public const string RpcNone = "RPC_None";
            public const string RpcNoneParametersX = "System.Int64,System.Int64";
        }

        public static class BadField
        {
            public const string TypeName = G + "Container";
            public const string FieldWidth = "m_widht";
            public const string FieldWidthType = "System.Int32";
        }

        public static class BadFieldType
        {
            public const string TypeName = G + "CraftingStation";
            public const string FieldRangeBuild = "m_rangeBuild";
            public const string FieldRangeBuildType = "System.Int32";
        }

        public static class DeclaredOnly
        {
            public const string TypeName = G + "Player";
            public const string InheritedHelper = "InheritedHelper";
        }

        public static class CtorExcluded
        {
            public const string TypeName = G + "Player";
            public const string Ctor = ".ctor";
        }

        public static class Hidden
        {
            public const string TypeName = G + "Container";
            public const string HiddenHelper = "HiddenHelper";
        }
    }
}
