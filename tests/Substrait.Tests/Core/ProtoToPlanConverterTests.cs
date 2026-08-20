using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Expression;
using Substrait.Core.Extension;
using Substrait.Core.Extension.Functions;
using Substrait.Core.Extension.Types;
using Substrait.Core.Plan;
using Substrait.Core.Plan.Converters;
using Substrait.Core.Relation;
using Substrait.Core.Type;
using Substrait.Protobuf;
using ProtoPlan = Substrait.Protobuf.Plan;
using ProtoRel = Substrait.Protobuf.Rel;
using ProtoVersion = Substrait.Protobuf.Version;

namespace Substrait.Tests.Core;

[TestClass]
public sealed class ProtoToPlanConverterTests
{
    private static readonly string[] ExpectedRootNames = ["output"];
    private readonly ProtoToPlanConverter converter = new(new ExtensionsCollection());

    [TestMethod]
    public void ConvertsRootNamesAndVersion()
    {
        ProtoPlan plan = CreatePlan();

        IPlan result = this.converter.From(plan, ExtensionsDictionary.StrictMode.OFF);

        Assert.AreEqual(1, result.Roots.Count);
        Assert.IsInstanceOfType<NamedTableRead>(result.Roots[0].Input);
        CollectionAssert.AreEqual(ExpectedRootNames, result.Roots[0].Names.ToArray());
        Assert.AreEqual(1U, result.Version.MajorNumber);
        Assert.AreEqual(2U, result.Version.MinorNumber);
        Assert.AreEqual(3U, result.Version.PatchNumber);
        Assert.AreEqual("abc", result.Version.GitHash);
        Assert.AreEqual("tests", result.Version.Producer);
    }

    [TestMethod]
    public void RejectsMultiplePlanRelations()
    {
        ProtoPlan plan = CreatePlan();
        plan.Relations.Add(CreatePlan().Relations[0]);

        Assert.ThrowsException<NotImplementedException>(() =>
            this.converter.From(plan, ExtensionsDictionary.StrictMode.OFF));
    }

    [TestMethod]
    public void RejectsNonRootPlanRelation()
    {
        ProtoPlan plan = CreatePlan();
        plan.Relations[0] = new PlanRel { Rel = CreateRead() };

        Assert.ThrowsException<System.Runtime.Serialization.SerializationException>(() =>
            this.converter.From(plan, ExtensionsDictionary.StrictMode.OFF));
    }

    [TestMethod]
    public void RoundTripsPlanSemantics()
    {
        IPlan original = this.converter.From(CreatePlan(), ExtensionsDictionary.StrictMode.OFF);

        ProtoPlan serialized = new PlanToProtoConverter().From(original);
        IPlan roundTripped = this.converter.From(serialized, ExtensionsDictionary.StrictMode.OFF);

        Assert.AreEqual(original, roundTripped);
    }

    [TestMethod]
    public void NumbersFunctionAndTypeVariationAnchorsIndependently()
    {
        var variation = new TypeVariationImpl("/types.yaml", "i64", "custom", string.Empty, FunctionBehavior.INHERITS);
        var schema = new Substrait.Core.Type.NamedStruct(
            ["value"],
            TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.I64_(variation)]));
        var read = new NamedTableRead(schema, ["orders"], null);
        var function = new Substrait.Core.Expression.Expression.ScalarFunctionInvocation(
            "/functions.yaml",
            "identity:i64",
            [new Literal.I64Literal(1)],
            TypeFactory.REQUIRED.I64,
            null);
        var project = new Project(read, [function]);
        IPlan plan = new Substrait.Core.Plan.Plan(
            [new Substrait.Core.Plan.Plan.Root(project, ["value", "result"])],
            Substrait.Core.Plan.Version.Current);

        ProtoPlan result = new PlanToProtoConverter().From(plan);

        Assert.AreEqual(1U, result.Relations[0].Root.Input.Project.Input.Read.BaseSchema.Struct.Types_[0].I64.TypeVariationReference);
        Assert.AreEqual(0U, result.Relations[0].Root.Input.Project.Expressions[0].ScalarFunction.FunctionReference);
        Assert.AreEqual(1U, result.Extensions.Single(extension => extension.ExtensionTypeVariation is not null).ExtensionTypeVariation.TypeVariationAnchor);
        Assert.AreEqual(0U, result.Extensions.Single(extension => extension.ExtensionFunction is not null).ExtensionFunction.FunctionAnchor);
    }

    private static ProtoPlan CreatePlan()
    {
        return new ProtoPlan
        {
            Version = new ProtoVersion
            {
                MajorNumber = 1,
                MinorNumber = 2,
                PatchNumber = 3,
                GitHash = "abc",
                Producer = "tests",
            },
            Relations =
            {
                new PlanRel
                {
                    Root = new RelRoot
                    {
                        Input = CreateRead(),
                        Names = { "output" },
                    },
                },
            },
        };
    }

    private static ProtoRel CreateRead()
    {
        return new ProtoRel
        {
            Read = new ReadRel
            {
                BaseSchema = new Substrait.Protobuf.NamedStruct
                {
                    Names = { "value" },
                    Struct = new Substrait.Protobuf.Type.Types.Struct
                    {
                        Types_ =
                        {
                            new Substrait.Protobuf.Type
                            {
                                I64 = new Substrait.Protobuf.Type.Types.I64
                                {
                                    Nullability = Substrait.Protobuf.Type.Types.Nullability.Required,
                                },
                            },
                        },
                        Nullability = Substrait.Protobuf.Type.Types.Nullability.Required,
                    },
                },
                NamedTable = new ReadRel.Types.NamedTable { Names = { "orders" } },
                Common = new RelCommon(),
            },
        };
    }
}
