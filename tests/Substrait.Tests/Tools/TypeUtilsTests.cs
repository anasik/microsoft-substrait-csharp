using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Type;
using Substrait.Tools;

namespace Substrait.Tests.Tools;

[TestClass]
public sealed class TypeUtilsTests
{
    [TestMethod]
    public void ConcatCombinesNamedStructs()
    {
        var first = new NamedStruct(
            ["a", "b", "c"],
            new ParameterizedType.Struct(
                [TypeFactory.REQUIRED.BOOL, TypeFactory.REQUIRED.STR, TypeFactory.REQUIRED.BINARY],
                IType.NullableType.Nullable));
        var second = new NamedStruct(
            ["x", "y"],
            new ParameterizedType.Struct(
                [TypeFactory.REQUIRED.FP64, TypeFactory.REQUIRED.FP32],
                IType.NullableType.Required));

        NamedStruct result = first.Concat(second);

        AssertSequenceEqual(["a", "b", "c", "x", "y"], result.Names);
        AssertSequenceEqual(
            [TypeFactory.REQUIRED.BOOL, TypeFactory.REQUIRED.STR, TypeFactory.REQUIRED.BINARY, TypeFactory.REQUIRED.FP64, TypeFactory.REQUIRED.FP32],
            result.Struct.Fields);
        Assert.AreEqual(IType.NullableType.Required, result.Struct.Nullable);
    }

    [TestMethod]
    public void ConcatCombinesNamedStructSequence()
    {
        var first = new NamedStruct(["i"], TypeFactory.NULLABLE.Struct([TypeFactory.REQUIRED.BOOL]));
        var second = new NamedStruct(["j"], TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.FP64]));
        var third = new NamedStruct(["k"], TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.STR]));

        NamedStruct result = TypeUtils.Concat([first, second, third]);

        AssertSequenceEqual(["i", "j", "k"], result.Names);
        AssertSequenceEqual(
            [TypeFactory.REQUIRED.BOOL, TypeFactory.REQUIRED.FP64, TypeFactory.REQUIRED.STR],
            result.Struct.Fields);
        Assert.AreEqual(IType.NullableType.Required, result.Struct.Nullable);
    }

    [TestMethod]
    public void RenameReplacesNamesAndPreservesStruct()
    {
        var namedStruct = new NamedStruct(
            ["i", "j"],
            TypeFactory.NULLABLE.Struct([TypeFactory.REQUIRED.BINARY, TypeFactory.REQUIRED.STR]));

        NamedStruct result = namedStruct.Rename(["x", "y"]);

        AssertSequenceEqual(["x", "y"], result.Names);
        Assert.AreSame(namedStruct.Struct, result.Struct);
    }

    private static void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        Assert.IsTrue(expected.SequenceEqual(actual), $"Expected [{string.Join(", ", expected)}], but found [{string.Join(", ", actual)}].");
    }
}