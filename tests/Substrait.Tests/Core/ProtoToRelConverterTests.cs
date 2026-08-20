using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Expression;
using Substrait.Core.Extension;
using Substrait.Core.Relation;
using Substrait.Core.Relation.Converters;
using Substrait.Core.Type;
using Substrait.Protobuf;
using ProtoExpression = Substrait.Protobuf.Expression;
using ProtoRel = Substrait.Protobuf.Rel;
using ProtoType = Substrait.Protobuf.Type;

namespace Substrait.Tests.Core;

[TestClass]
public sealed class ProtoToRelConverterTests
{
    private readonly ProtoToRelConverter converter = new(
        new ExtensionsDictionary.Builder().Build(),
        new ExtensionsCollection(),
        ExtensionsDictionary.StrictMode.OFF);

    [TestMethod]
    public void ConvertsReadFilterProjectChain()
    {
        ProtoRel read = CreateNamedRead();
        ProtoRel filter = new()
        {
            Filter = new FilterRel
            {
                Input = read,
                Condition = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { Boolean = true } },
                Common = new RelCommon(),
            },
        };
        ProtoRel project = new()
        {
            Project = new ProjectRel
            {
                Input = filter,
                Expressions = { new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = 42 } } },
                Common = new RelCommon(),
            },
        };

        Project result = (Project)this.converter.ToRel(project);

        Assert.IsInstanceOfType<Filter>(result.Input);
        Assert.IsInstanceOfType<NamedTableRead>(((Filter)result.Input).Input);
        Assert.AreEqual(2, result.RecordType.Fields.Count);
        Assert.AreEqual(42L, ((Literal.I64Literal)result.Expressions[0]).Value);
    }

    [TestMethod]
    public void ConvertsScalarSubqueryThroughRelationConverter()
    {
        ProtoRel relation = new()
        {
            Project = new ProjectRel
            {
                Input = CreateNamedRead(),
                Expressions =
                {
                    new ProtoExpression
                    {
                        Subquery = new ProtoExpression.Types.Subquery
                        {
                            Scalar = new ProtoExpression.Types.Subquery.Types.Scalar { Input = CreateNamedRead() },
                        },
                    },
                },
                Common = new RelCommon { Emit = new RelCommon.Types.Emit { OutputMapping = { 1 } } },
            },
        };

        Project result = (Project)this.converter.ToRel(relation);

        Assert.IsInstanceOfType<Substrait.Core.Expression.Expression.ScalarSubquery>(result.Expressions[0]);
    }

    [TestMethod]
    public void ConvertsSetComparisonSubqueryThroughRelationConverter()
    {
        ProtoRel relation = new()
        {
            Project = new ProjectRel
            {
                Input = CreateNamedRead(),
                Expressions =
                {
                    new ProtoExpression
                    {
                        Subquery = new ProtoExpression.Types.Subquery
                        {
                            SetComparison = new ProtoExpression.Types.Subquery.Types.SetComparison
                            {
                                Left = new ProtoExpression
                                {
                                    Literal = new ProtoExpression.Types.Literal { I64 = 2 },
                                },
                                ComparisonOp = ProtoExpression.Types.Subquery.Types.SetComparison.Types.ComparisonOp.Lt,
                                ReductionOp = ProtoExpression.Types.Subquery.Types.SetComparison.Types.ReductionOp.All,
                                Right = CreateNamedRead(),
                            },
                        },
                    },
                },
                Common = new RelCommon { Emit = new RelCommon.Types.Emit { OutputMapping = { 1 } } },
            },
        };

        Project result = (Project)this.converter.ToRel(relation);
        var comparison = (Substrait.Core.Expression.Expression.SetComparisonSubquery)result.Expressions[0];

        Assert.AreEqual(new Literal.I64Literal(2), comparison.Expression);
        Assert.AreEqual(Substrait.Core.Expression.Expression.SetComparisonSubquery.ComparisonOp.LessThan, comparison.Comparison);
        Assert.AreEqual(Substrait.Core.Expression.Expression.SetComparisonSubquery.ReductionOp.All, comparison.Reduction);
        Assert.IsInstanceOfType<NamedTableRead>(comparison.Subquery);
    }

    [DataTestMethod]
    [DataRow(3L, 0L)]
    [DataRow(-1L, 3L)]
    [DataRow(100L, 8L)]
    public void ConvertsFetch(long count, long offset)
    {
        ProtoRel relation = new()
        {
            Fetch = new FetchRel
            {
                Input = CreateNamedRead(),
                CountExpr = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = count } },
                OffsetExpr = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = offset } },
                Common = new RelCommon(),
            },
        };

        Fetch result = (Fetch)this.converter.ToRel(relation);

        Assert.AreEqual(new Literal.I64Literal(count), result.Count);
        Assert.AreEqual(new Literal.I64Literal(offset), result.Offset);
        Assert.AreEqual(1, result.RecordType.Fields.Count);
        Assert.AreEqual(TypeFactory.REQUIRED.I64, result.RecordType.Fields[0]);
    }

    [TestMethod]
    public void ConvertsVirtualTableReadRows()
    {
        var nullableI32 = new ProtoType.Types.I32 { Nullability = ProtoType.Types.Nullability.Nullable };
        ProtoRel relation = new()
        {
            Read = new ReadRel
            {
                BaseSchema = new Substrait.Protobuf.NamedStruct
                {
                    Names = { "value" },
                    Struct = new ProtoType.Types.Struct
                    {
                        Types_ = { new ProtoType { I32 = nullableI32 } },
                        Nullability = ProtoType.Types.Nullability.Required,
                    },
                },
                VirtualTable = new ReadRel.Types.VirtualTable
                {
                    Expressions =
                    {
                        new ProtoExpression.Types.Nested.Types.Struct
                        {
                            Fields = { new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I32 = 10 } } },
                        },
                        new ProtoExpression.Types.Nested.Types.Struct
                        {
                            Fields =
                            {
                                new ProtoExpression
                                {
                                    Literal = new ProtoExpression.Types.Literal
                                    {
                                        Null = new ProtoType { I32 = nullableI32 },
                                        Nullable = true,
                                    },
                                },
                            },
                        },
                    },
                },
                Common = new RelCommon(),
            },
        };

        VirtualTableRead result = (VirtualTableRead)this.converter.ToRel(relation);

        Assert.AreEqual("value", result.InitialSchema.Names[0]);
        Assert.AreEqual(TypeFactory.NULLABLE.I32, result.RecordType.Fields[0]);
        Assert.AreEqual(2, result.Rows.Count);
        Assert.AreEqual(new Literal.I32Literal(10), result.Rows[0].Fields[0]);
        Assert.IsInstanceOfType<Literal.NullLiteral>(result.Rows[1].Fields[0]);
        Assert.AreEqual(TypeFactory.NULLABLE.I32, result.Rows[1].Fields[0].Type);
    }

    [DataTestMethod]
    [DataRow(SetRel.Types.SetOp.MinusPrimary, Set.SetOp.MinusPrimary, ProtoType.Types.Nullability.Nullable, ProtoType.Types.Nullability.Required, IType.NullableType.Nullable)]
    [DataRow(SetRel.Types.SetOp.IntersectionMultiset, Set.SetOp.IntersectionMultiset, ProtoType.Types.Nullability.Nullable, ProtoType.Types.Nullability.Required, IType.NullableType.Required)]
    [DataRow(SetRel.Types.SetOp.UnionAll, Set.SetOp.UnionAll, ProtoType.Types.Nullability.Required, ProtoType.Types.Nullability.Nullable, IType.NullableType.Nullable)]
    public void ConvertsSetOperations(
        SetRel.Types.SetOp protoOperation,
        Set.SetOp expectedOperation,
        ProtoType.Types.Nullability leftNullability,
        ProtoType.Types.Nullability rightNullability,
        IType.NullableType expectedNullability)
    {
        ProtoRel relation = new()
        {
            Set = new SetRel
            {
                Op = protoOperation,
                Inputs = { CreateNamedRead(leftNullability), CreateNamedRead(rightNullability) },
                Common = new RelCommon(),
            },
        };

        Set result = (Set)this.converter.ToRel(relation);

        Assert.AreEqual(expectedOperation, result.SetOperation);
        Assert.AreEqual(2, result.Inputs.Count);
        Assert.AreEqual(expectedNullability, result.RecordType.Fields[0].Nullable);
    }

    [DataTestMethod]
    [DataRow(false, 0)]
    [DataRow(true, 1)]
    public void ConvertsScalarAndEmptyVectorAggregates(bool includeEmptyGrouping, int expectedGroupingCount)
    {
        AggregateRel aggregate = new()
        {
            Input = CreateNamedRead(),
            Measures = { CreateAggregateMeasure() },
            Common = new RelCommon(),
        };
        if (includeEmptyGrouping)
        {
            aggregate.Groupings.Add(new AggregateRel.Types.Grouping());
        }

        Aggregate result = (Aggregate)CreateAggregateConverter().ToRel(new ProtoRel { Aggregate = aggregate });

        Assert.AreEqual(expectedGroupingCount, result.Groupings.Count);
        Assert.IsTrue(result.Groupings.All(grouping => grouping.Expressions.Count == 0));
        Assert.AreEqual(0, result.GroupingExpressions.Count);
        Assert.AreEqual(1, result.Measures.Count);
        Assert.AreEqual(TypeFactory.REQUIRED.BOOL, result.RecordType.Fields[0]);
    }

    [TestMethod]
    public void ConvertsAggregateJoinHashJoinAndExchange()
    {
        ProtoRel aggregate = new()
        {
            Aggregate = new AggregateRel
            {
                Input = CreateNamedRead(),
                GroupingExpressions = { new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = 1 } } },
                Groupings = { new AggregateRel.Types.Grouping { ExpressionReferences = { 0 } } },
                Common = new RelCommon(),
            },
        };
        ProtoRel join = new()
        {
            Join = new JoinRel
            {
                Left = aggregate,
                Right = CreateNamedRead(),
                Type = JoinRel.Types.JoinType.Inner,
                Expression = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { Boolean = true } },
                Common = new RelCommon(),
            },
        };
        ProtoRel hashJoin = new()
        {
            HashJoin = new HashJoinRel
            {
                Left = join,
                Right = CreateNamedRead(),
                Type = HashJoinRel.Types.JoinType.Left,
                BuildInput = HashJoinRel.Types.BuildInput.Left,
                Keys =
                {
                    new ComparisonJoinKey
                    {
                        Left = CreateFieldReference(0),
                        Right = CreateFieldReference(1),
                        Comparison = new ComparisonJoinKey.Types.ComparisonType
                        {
                            Simple = ComparisonJoinKey.Types.SimpleComparisonType.Eq,
                        },
                    },
                },
                Common = new RelCommon(),
            },
        };
        ProtoRel exchange = new()
        {
            Exchange = new ExchangeRel
            {
                Input = hashJoin,
                PartitionCount = 4,
                SingleTarget = new ExchangeRel.Types.SingleBucketExpression
                {
                    Expression = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = 7 } },
                },
                Common = new RelCommon(),
            },
        };

        SingleBucketExchange result = (SingleBucketExchange)this.converter.ToRel(exchange);

        Assert.AreEqual(4, result.PartitionCount);
        HashJoin convertedHashJoin = (HashJoin)result.Input;
        Assert.IsTrue(convertedHashJoin.BuildLeft);
        Assert.AreEqual(1, convertedHashJoin.Keys.Count);
        Join convertedJoin = (Join)convertedHashJoin.Left;
        Assert.IsInstanceOfType<Aggregate>(convertedJoin.Left);

        ProtoRel serialized = new RelToProtoConverter().From(result);
        SingleBucketExchange roundTripped = (SingleBucketExchange)this.converter.ToRel(serialized);
        HashJoin roundTrippedHashJoin = (HashJoin)roundTripped.Input;

        Assert.AreEqual(result, roundTripped);
        Assert.IsTrue(roundTrippedHashJoin.BuildLeft);
    }

    [TestMethod]
    public void RejectsAggregateWithUnusedGroupingExpression()
    {
        ProtoRel relation = new()
        {
            Aggregate = new AggregateRel
            {
                Input = CreateNamedRead(),
                GroupingExpressions =
                {
                    new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = 1 } },
                    new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = 2 } },
                },
                Groupings = { new AggregateRel.Types.Grouping { ExpressionReferences = { 0, 0 } } },
                Common = new RelCommon(),
            },
        };

        Assert.ThrowsException<System.Runtime.Serialization.SerializationException>(() => this.converter.ToRel(relation));
    }

    [TestMethod]
    public void RoundTripsFoundationalRelationChain()
    {
        ProtoRel relation = new()
        {
            Project = new ProjectRel
            {
                Input = new ProtoRel
                {
                    Filter = new FilterRel
                    {
                        Input = CreateNamedRead(),
                        Condition = new ProtoExpression { Literal = new ProtoExpression.Types.Literal { Boolean = true } },
                        Common = new RelCommon(),
                    },
                },
                Expressions = { new ProtoExpression { Literal = new ProtoExpression.Types.Literal { I64 = 42 } } },
                Common = new RelCommon(),
            },
        };
        IRel original = this.converter.ToRel(relation);

        ProtoRel serialized = new RelToProtoConverter().From(original);
        IRel roundTripped = this.converter.ToRel(serialized);

        Assert.AreEqual(original, roundTripped);
    }

    [TestMethod]
    public void RoundTripsScalarSubquery()
    {
        ProtoRel relation = new()
        {
            Project = new ProjectRel
            {
                Input = CreateNamedRead(),
                Expressions =
                {
                    new ProtoExpression
                    {
                        Subquery = new ProtoExpression.Types.Subquery
                        {
                            Scalar = new ProtoExpression.Types.Subquery.Types.Scalar { Input = CreateNamedRead() },
                        },
                    },
                },
                Common = new RelCommon { Emit = new RelCommon.Types.Emit { OutputMapping = { 1 } } },
            },
        };
        IRel original = this.converter.ToRel(relation);

        ProtoRel serialized = new RelToProtoConverter().From(original);
        IRel roundTripped = this.converter.ToRel(serialized);

        Assert.AreEqual(original, roundTripped);
    }

    private static ProtoExpression.Types.FieldReference CreateFieldReference(int field)
    {
        return new ProtoExpression.Types.FieldReference
        {
            DirectReference = new ProtoExpression.Types.ReferenceSegment
            {
                StructField = new ProtoExpression.Types.ReferenceSegment.Types.StructField { Field = field },
            },
            RootReference = new ProtoExpression.Types.FieldReference.Types.RootReference(),
        };
    }

    private static AggregateRel.Types.Measure CreateAggregateMeasure()
    {
        return new AggregateRel.Types.Measure
        {
            Measure_ = new AggregateFunction
            {
                FunctionReference = 1,
                OutputType = new ProtoType
                {
                    Bool = new ProtoType.Types.Boolean { Nullability = ProtoType.Types.Nullability.Required },
                },
                Phase = AggregationPhase.InitialToResult,
                Invocation = AggregateFunction.Types.AggregationInvocation.All,
            },
        };
    }

    private static ProtoToRelConverter CreateAggregateConverter()
    {
        Substrait.Protobuf.Plan plan = new()
        {
            ExtensionUris =
            {
                new SimpleExtensionURI { ExtensionUriAnchor = 1, Uri = "/synthetic-aggregate.yaml" },
            },
            Extensions =
            {
                new SimpleExtensionDeclaration
                {
                    ExtensionFunction = new SimpleExtensionDeclaration.Types.ExtensionFunction
                    {
                        ExtensionUriReference = 1,
                        FunctionAnchor = 1,
                        Name = "synthetic_aggregate",
                    },
                },
            },
        };
        return new ProtoToRelConverter(
            new ExtensionsDictionary.Builder(plan).Build(),
            new ExtensionsCollection(),
            ExtensionsDictionary.StrictMode.OFF);
    }

    private static ProtoRel CreateNamedRead()
    {
        return CreateNamedRead(ProtoType.Types.Nullability.Required);
    }

    private static ProtoRel CreateNamedRead(ProtoType.Types.Nullability nullability)
    {
        return new ProtoRel
        {
            Read = new ReadRel
            {
                BaseSchema = new Substrait.Protobuf.NamedStruct
                {
                    Names = { "value" },
                    Struct = new ProtoType.Types.Struct
                    {
                        Types_ =
                        {
                            new ProtoType
                            {
                                I64 = new ProtoType.Types.I64 { Nullability = nullability },
                            },
                        },
                        Nullability = ProtoType.Types.Nullability.Required,
                    },
                },
                NamedTable = new ReadRel.Types.NamedTable { Names = { "orders" } },
                Common = new RelCommon(),
            },
        };
    }
}
