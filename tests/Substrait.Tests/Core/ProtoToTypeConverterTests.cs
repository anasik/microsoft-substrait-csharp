using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Extension;
using Substrait.Core.Type;
using Substrait.Core.Type.Converters;
using ProtoType = Substrait.Protobuf.Type;

namespace Substrait.Tests.Core;

[TestClass]
public sealed class ProtoToTypeConverterTests
{
    [TestMethod]
    public void ConvertsPrimitiveAndParameterizedTypes()
    {
        ProtoToTypeConverter converter = new();

        IType boolean = converter.From(new ProtoType
        {
            Bool = new ProtoType.Types.Boolean { Nullability = ProtoType.Types.Nullability.Nullable },
        });
        IType decimalType = converter.From(new ProtoType
        {
            Decimal = new ProtoType.Types.Decimal
            {
                Nullability = ProtoType.Types.Nullability.Required,
                Precision = 10,
                Scale = 2,
            },
        });

        Assert.AreEqual(TypeFactory.NULLABLE.Boolean_(null), boolean);
        Assert.AreEqual(TypeFactory.REQUIRED.Decimal(10, 2), decimalType);
    }

    [TestMethod]
    public void ConvertsNestedStructWithoutRecursion()
    {
        ProtoType inner = new()
        {
            Struct = new ProtoType.Types.Struct
            {
                Nullability = ProtoType.Types.Nullability.Required,
                Types_ =
                {
                    new ProtoType { I64 = new ProtoType.Types.I64 { Nullability = ProtoType.Types.Nullability.Required } },
                },
            },
        };
        ProtoType outer = new()
        {
            Struct = new ProtoType.Types.Struct
            {
                Nullability = ProtoType.Types.Nullability.Nullable,
                Types_ = { inner },
            },
        };

        IType result = new ProtoToTypeConverter().From(outer);

        Assert.AreEqual(TypeFactory.NULLABLE.Struct([TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.I64])]), result);
    }

    [TestMethod]
    public void ConvertsDeeplyNestedStructAndPreservesFieldOrder()
    {
        ProtoType deepest = new()
        {
            Struct = new ProtoType.Types.Struct
            {
                Nullability = ProtoType.Types.Nullability.Required,
                Types_ =
                {
                    new ProtoType { Decimal = new ProtoType.Types.Decimal { Nullability = ProtoType.Types.Nullability.Required, Precision = 10, Scale = 2 } },
                    new ProtoType { I64 = new ProtoType.Types.I64 { Nullability = ProtoType.Types.Nullability.Nullable } },
                },
            },
        };
        ProtoType middle = new()
        {
            Struct = new ProtoType.Types.Struct
            {
                Nullability = ProtoType.Types.Nullability.Nullable,
                Types_ =
                {
                    new ProtoType { Bool = new ProtoType.Types.Boolean { Nullability = ProtoType.Types.Nullability.Required } },
                    deepest,
                },
            },
        };
        ProtoType outer = new()
        {
            Struct = new ProtoType.Types.Struct
            {
                Nullability = ProtoType.Types.Nullability.Required,
                Types_ =
                {
                    new ProtoType { I32 = new ProtoType.Types.I32 { Nullability = ProtoType.Types.Nullability.Required } },
                    middle,
                    new ProtoType { String = new ProtoType.Types.String { Nullability = ProtoType.Types.Nullability.Nullable } },
                },
            },
        };

        IType result = new ProtoToTypeConverter().From(outer);

        Assert.AreEqual(
            TypeFactory.REQUIRED.Struct(
            [
                TypeFactory.REQUIRED.I32,
                TypeFactory.NULLABLE.Struct(
                [
                    TypeFactory.REQUIRED.Boolean_(null),
                    TypeFactory.REQUIRED.Struct(
                    [
                        TypeFactory.REQUIRED.Decimal(10, 2),
                        TypeFactory.NULLABLE.I64,
                    ]),
                ]),
                TypeFactory.NULLABLE.String_(null),
            ]),
            result);
    }

    [TestMethod]
    public void NonStrictModeIgnoresUnknownTypeVariation()
    {
        ProtoType type = new()
        {
            I64 = new ProtoType.Types.I64
            {
                Nullability = ProtoType.Types.Nullability.Required,
                TypeVariationReference = 42,
            },
        };
        ProtoToTypeConverter converter = new(
            new ExtensionsDictionary.Builder().Build(),
            new ExtensionsCollection(),
            ExtensionsDictionary.StrictMode.OFF);

        Assert.AreEqual(TypeFactory.REQUIRED.I64, converter.From(type));
    }
}
