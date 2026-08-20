using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Extension;
using Substrait.Core.Plan;
using Substrait.Core.Plan.Converters;
using Substrait.Core.Relation;
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
