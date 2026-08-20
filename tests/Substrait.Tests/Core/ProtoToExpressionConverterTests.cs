using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Expression;
using Substrait.Core.Expression.Converters;
using Substrait.Core.Extension;
using Substrait.Core.Type;
using Substrait.Core.Type.Converters;
using ProtoExpression = Substrait.Protobuf.Expression;

namespace Substrait.Tests.Core;

[TestClass]
public sealed class ProtoToExpressionConverterTests
{
    private readonly ProtoToExpressionConverter converter = new(
        new ExtensionsDictionary.Builder().Build(),
        new ExtensionsCollection(),
        new ProtoToTypeConverter());

    [TestMethod]
    public void ConvertsNestedStructLiteralAndPreservesOrder()
    {
        ProtoExpression.Types.Literal proto = new()
        {
            Struct = new ProtoExpression.Types.Literal.Types.Struct
            {
                Fields =
                {
                    new ProtoExpression.Types.Literal { I32 = 1 },
                    new ProtoExpression.Types.Literal
                    {
                        Struct = new ProtoExpression.Types.Literal.Types.Struct
                        {
                            Fields = { new ProtoExpression.Types.Literal { String = "nested" } },
                        },
                    },
                    new ProtoExpression.Types.Literal { Boolean = true },
                },
            },
        };

        Literal.StructLiteral result = (Literal.StructLiteral)this.converter.CreateLiteral(proto);

        Assert.AreEqual(3, result.Fields.Count);
        Assert.AreEqual(1, ((Literal.I32Literal)result.Fields[0]).Value);
        Assert.AreEqual("nested", ((Literal.StrLiteral)((Literal.StructLiteral)result.Fields[1]).Fields[0]).Value);
        Assert.IsTrue(((Literal.BoolLiteral)result.Fields[2]).Value);
    }

    [TestMethod]
    public void ConvertsIfThenIteratively()
    {
        ProtoExpression proto = new()
        {
            IfThen = new ProtoExpression.Types.IfThen
            {
                Ifs =
                {
                    new ProtoExpression.Types.IfThen.Types.IfClause
                    {
                        If = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { Boolean = true } },
                        Then = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I32 = 7 } },
                    },
                },
                Else = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I32 = 9 } },
            },
        };

        Substrait.Core.Expression.Expression.IfThen result =
            (Substrait.Core.Expression.Expression.IfThen)this.converter.From(proto);

        Assert.AreEqual(7, ((Literal.I32Literal)result.IfClauses[0].Then).Value);
        Assert.AreEqual(9, ((Literal.I32Literal)result.ElseClause).Value);
    }

    [TestMethod]
    public void ConvertsRootFieldReferenceAgainstInputSchema()
    {
        ParameterizedType.Struct schema = TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.I32, TypeFactory.NULLABLE.STR]);
        ProtoExpression proto = new()
        {
            Selection = new ProtoExpression.Types.FieldReference
            {
                DirectReference = new ProtoExpression.Types.ReferenceSegment
                {
                    StructField = new ProtoExpression.Types.ReferenceSegment.Types.StructField { Field = 1 },
                },
                RootReference = new ProtoExpression.Types.FieldReference.Types.RootReference(),
            },
        };

        FieldReference result = (FieldReference)this.converter.From(proto, schema, ImmutableList<ParameterizedType.Struct>.Empty);

        Assert.AreEqual(1, result.FieldIndex);
        Assert.AreEqual(TypeFactory.NULLABLE.STR, result.Type);
    }

    [TestMethod]
    public void RejectsRootFieldReferenceOutsideInputSchema()
    {
        ParameterizedType.Struct schema = TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.I32]);
        ProtoExpression proto = CreateFieldReference(1);

        SerializationException exception = Assert.ThrowsException<SerializationException>(() =>
            this.converter.From(proto, schema, ImmutableList<ParameterizedType.Struct>.Empty));

        StringAssert.Contains(exception.Message, "field index 1");
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(2U)]
    public void RejectsOuterReferenceOutsideEnclosingSchemas(uint stepsOut)
    {
        ParameterizedType.Struct schema = TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.I32]);
        ProtoExpression proto = CreateFieldReference(0, stepsOut);

        SerializationException exception = Assert.ThrowsException<SerializationException>(() =>
            this.converter.From(proto, schema, [schema]));

        StringAssert.Contains(exception.Message, $"outer reference steps {stepsOut}");
    }

    [TestMethod]
    public void RejectsFieldReferenceOutsideOuterSchema()
    {
        ParameterizedType.Struct schema = TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.I32]);
        ProtoExpression proto = CreateFieldReference(1, 1);

        SerializationException exception = Assert.ThrowsException<SerializationException>(() =>
            this.converter.From(proto, schema, [schema]));

        StringAssert.Contains(exception.Message, "field index 1");
    }

    private static ProtoExpression CreateFieldReference(int fieldIndex, uint? stepsOut = null)
    {
        var reference = new ProtoExpression.Types.FieldReference
        {
            DirectReference = new ProtoExpression.Types.ReferenceSegment
            {
                StructField = new ProtoExpression.Types.ReferenceSegment.Types.StructField { Field = fieldIndex },
            },
        };

        if (stepsOut.HasValue)
        {
            reference.OuterReference = new ProtoExpression.Types.FieldReference.Types.OuterReference { StepsOut = stepsOut.Value };
        }
        else
        {
            reference.RootReference = new ProtoExpression.Types.FieldReference.Types.RootReference();
        }

        return new ProtoExpression { Selection = reference };
    }
}
