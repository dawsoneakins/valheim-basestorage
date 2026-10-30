namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// Reflection access to the BaseStorage.Core.Persistence surface (contracts/persistence-contracts.md v1, ADR-GWIN-012).
/// Every type is resolved by name through <see cref="ContractTypes.Require"/>, so this project keeps compiling before the
/// types exist, and each test fails with "Contract type ... does not exist" until they do (ADR-GWIN-013).
/// Calls go through <see cref="ContractTypes.Invoke"/>, so contract exceptions arrive unwrapped with their ParamName.
/// </summary>
public static class PersistenceContract
{
    /// <summary>Namespace of the eight contracted public types (contract section 1).</summary>
    public const string Namespace = "BaseStorage.Core.Persistence";

    /// <summary>The eight exported type names of the namespace, in contract order.</summary>
    public static readonly string[] SurfaceTypeNames =
    [
        "PersistenceKeys",
        "CategoryAssignment",
        "CategoryCodec",
        "OrganizeLockRecord",
        "OrganizeLockCodec",
        "InventorySlot",
        "FavouriteSlots",
        "FavouritesCodec",
    ];

    private const BindingFlags StaticPublic = BindingFlags.Public | BindingFlags.Static;
    private const BindingFlags InstancePublic = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Resolves a type of the namespace, or fails the calling test with the missing name.</summary>
    public static Type Resolve(string simpleName) => ContractTypes.Require(Namespace + "." + simpleName);

    /// <summary>Resolves all eight contracted types.</summary>
    public static Type[] ResolveAll() => SurfaceTypeNames.Select(Resolve).ToArray();

    /// <summary>Constructs a contract value through its public constructor.</summary>
    public static object Make(string simpleName, params object?[] args) =>
        ContractTypes.Make(Resolve(simpleName), args);

    /// <summary>Reads a public static property, or a public static field such as a const key.</summary>
    public static object? Static(string simpleName, string member)
    {
        Type type = Resolve(simpleName);
        MethodInfo? getter = type.GetProperty(member, StaticPublic)?.GetGetMethod();
        if (getter is not null)
        {
            return ContractTypes.Invoke(getter, null, Array.Empty<object?>());
        }

        FieldInfo? field = type.GetField(member, StaticPublic);
        Assert.True(field is not null, $"{type.FullName} has no public static {member}.");
        return field!.GetValue(null);
    }

    /// <summary>Reads a public instance property.</summary>
    public static object? Get(object target, string property)
    {
        Type type = target.GetType();
        MethodInfo? getter = type.GetProperty(property, InstancePublic)?.GetGetMethod();
        Assert.True(getter is not null, $"{type.FullName} has no public {property} getter.");
        return ContractTypes.Invoke(getter!, target, Array.Empty<object?>());
    }

    /// <summary>Reads a public bool property.</summary>
    public static bool Flag(object target, string property) => (bool)Get(target, property)!;

    /// <summary>Reads a public string property.</summary>
    public static string? Text(object target, string property) => (string?)Get(target, property);

    /// <summary>Reads a public int property.</summary>
    public static int Int(object target, string property) => (int)Get(target, property)!;

    /// <summary>Calls a public static method whose argument types match <paramref name="args"/>.</summary>
    public static object? CallStatic(string simpleName, string method, params object?[] args) =>
        Invoke(Resolve(simpleName), method, StaticPublic, null, args);

    /// <summary>Calls a public instance method whose argument types match <paramref name="args"/>.</summary>
    public static object? CallInstance(object target, string method, params object?[] args) =>
        Invoke(target.GetType(), method, InstancePublic, target, args);

    /// <summary>Encode of the named codec. Returns null only when the contract is broken.</summary>
    public static string Encode(string codec, object? value) => (string)CallStatic(codec, "Encode", value)!;

    /// <summary>Decode of the named codec. Returns the decoded value, or null for "no value".</summary>
    public static object? Decode(string codec, string? encoded) => CallStatic(codec, "Decode", encoded);

    /// <summary>Decodes and captures what happened: the exception (null when none) and the result.</summary>
    public static (Exception? Thrown, object? Result) DecodeCapture(string codec, string? encoded)
    {
        _ = Resolve(codec);
        object? result = null;
        Exception? thrown = Record.Exception(() => { result = Decode(codec, encoded); });
        return (thrown, result);
    }

    /// <summary>Asserts P4 and P5 for one input: Decode throws nothing and returns no value.</summary>
    public static void AssertNoValue(string codec, string? encoded)
    {
        (Exception? thrown, object? result) = DecodeCapture(codec, encoded);
        if (thrown is not null)
        {
            Assert.Fail($"{codec}.Decode({Describe(encoded)}) threw {thrown.GetType().Name}: {thrown.Message}");
        }

        Assert.True(result is null, $"{codec}.Decode({Describe(encoded)}) returned a value; the contract requires null.");
    }

    /// <summary>Quotes a text for a failure message, or says null.</summary>
    public static string Describe(string? text) => text is null ? "null" : "\"" + text + "\"";

    /// <summary>CategoryAssignment.Keep.</summary>
    public static object Keep() => Static("CategoryAssignment", "Keep")!;

    /// <summary>CategoryAssignment.ForCategory(name).</summary>
    public static object CategoryOf(string name) => CallStatic("CategoryAssignment", "ForCategory", name)!;

    /// <summary>Keep for null, otherwise ForCategory(name): the value a decoded category row should equal.</summary>
    public static object CategoryOrKeep(string? name) => name is null ? Keep() : CategoryOf(name);

    /// <summary>new OrganizeLockRecord(holderId, holderName, TimeSpan.FromTicks(ticks)).</summary>
    public static object LockOf(long holderId, string? holderName, long ticks) =>
        Make("OrganizeLockRecord", holderId, holderName, TimeSpan.FromTicks(ticks));

    /// <summary>new InventorySlot(x, y).</summary>
    public static object SlotOf(int x, int y) => Make("InventorySlot", x, y);

    /// <summary>new FavouriteSlots(slots) built from (X, Y) pairs in the given order, duplicates included.</summary>
    public static object FavouritesOf(params (int X, int Y)[] slots) =>
        Make("FavouriteSlots", ContractTypes.TypedArray(Resolve("InventorySlot"), slots.Select(s => SlotOf(s.X, s.Y)).ToArray()));

    /// <summary>new FavouriteSlots(list) where list is a List of InventorySlot holding the given slots.</summary>
    public static object FavouritesFromList(params object?[] slots) =>
        Make("FavouriteSlots", ContractTypes.TypedList(Resolve("InventorySlot"), slots));

    /// <summary>FavouriteSlots.Empty.</summary>
    public static object EmptyFavourites() => Static("FavouriteSlots", "Empty")!;

    /// <summary>The Slots property of a FavouriteSlots, as an array in its order.</summary>
    public static object?[] SlotsOf(object set) => ((IEnumerable)Get(set, "Slots")!).Cast<object?>().ToArray();

    /// <summary>FavouriteSlots.Contains(slot).</summary>
    public static bool Contains(object set, object slot) => (bool)CallInstance(set, "Contains", slot)!;

    private static object? Invoke(Type type, string method, BindingFlags flags, object? target, object?[] args)
    {
        MethodInfo? candidate = type.GetMethods(flags)
            .FirstOrDefault(m => m.Name == method && Accepts(m.GetParameters(), args));
        Assert.True(candidate is not null, $"{type.FullName} has no {method} taking {args.Length} matching argument(s).");
        return ContractTypes.Invoke(candidate!, target, args);
    }

    private static bool Accepts(ParameterInfo[] parameters, object?[] args) =>
        parameters.Length == args.Length
        && parameters.Select((p, i) => args[i] is null || p.ParameterType.IsInstanceOfType(args[i])).All(ok => ok);
}
