using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Extension.Functions;
using Substrait.Core.Type;

namespace Substrait.Tests.Core;

[TestClass]
public sealed class ExtensionFunctionTests
{
    [TestMethod]
    public void TypeExpressionParserCreatesScalarTypes()
    {
        ITypeExpression parsed = TypeExpressionParser.Parse("i64");

        Assert.AreEqual(TypeFactory.REQUIRED.I64, parsed);
    }

    [TestMethod]
    public void FunctionKeyUsesParsedArgumentSignatures()
    {
        IArgument[] arguments =
        [
            new ValueArgument("i64", "left", "Left operand.", required: true),
            new ValueArgument("string", "right", "Right operand.", required: true),
        ];

        ScalarFunctionImpl function = new(
            "https://example.test/functions",
            "compare",
            "Compares two values.",
            FunctionImpl.NullabilityMode.Mirror,
            arguments,
            ImmutableDictionary<string, IOption>.Empty,
            ordered: null,
            variadic: null,
            returnType: "boolean");

        Assert.AreEqual("compare:i64_str", function.Key);
        Assert.AreEqual(function.Uri, function.Anchor.Namespace);
        Assert.AreEqual(function.Key, function.Anchor.Key);
    }
}
