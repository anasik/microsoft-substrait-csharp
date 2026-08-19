using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Substrait.Core.Expression;
using Substrait.Core.Relation;
using Substrait.Core.Type;
using Substrait.Tools.Visitor;
using static Substrait.Core.Expression.Literal;

namespace Substrait.Tests.Core;

[TestClass]
public sealed class RelationVisitorTests
{
    [TestMethod]
    public void RelVisitorContainsAllSealedRelationTypes()
    {
        List<System.Type> relationTypes = Assembly.GetAssembly(typeof(IRel))!
            .GetTypes()
            .Where(type => typeof(IRel).IsAssignableFrom(type)
                && type.IsClass
                && type.IsSealed
                && type.Namespace == "Substrait.Core.Relation")
            .ToList();

        List<System.Type> visitedTypes = typeof(RelVisitor<,>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == "Visit" && method.GetParameters().Length == 2)
            .Select(method => method.GetParameters()[0].ParameterType)
            .ToList();

        foreach (System.Type relationType in relationTypes)
        {
            Assert.IsTrue(
                visitedTypes.Contains(relationType),
                $"RelVisitor does not contain a Visit method for {relationType.Name}");
        }
    }

    [TestMethod]
    public void DispatchersTraverseSyntheticRelationTree()
    {
        IRel root = CreateRelationTree(true);
        StringBuilderContext topDownContext = new();
        StringBuilderContext bottomUpContext = new();

        new RelTopDownDispatcher<StringBuilderContext, VoidOutput>(new RelationPrinter())
            .Dispatch(root, topDownContext);
        new RelBottomUpDispatcher<StringBuilderContext, VoidOutput>(new RelationPrinter())
            .Dispatch(root, bottomUpContext);

        Assert.AreEqual("Project|Filter|NamedTableRead|", topDownContext.ToString());
        Assert.AreEqual("NamedTableRead|Filter|Project|", bottomUpContext.ToString());
    }

    [TestMethod]
    public void RelationEqualityIncludesNestedInputs()
    {
        Project first = CreateRelationTree(true);
        Project equivalent = CreateRelationTree(true);
        Project different = CreateRelationTree(false);

        Assert.AreEqual(first, equivalent);
        Assert.AreEqual(first.GetHashCode(), equivalent.GetHashCode());
        Assert.AreNotEqual(first, different);
    }

    [TestMethod]
    public void DispatchersBailOutWhenProjectIsFound()
    {
        IRel root = CreateRelationTree(true);
        NoOpContext<IRel, bool> context = new();

        Assert.IsTrue(new ProjectFindingTopDownDispatcher().Dispatch(root, context));
        Assert.IsTrue(new ProjectFindingBottomUpDispatcher().Dispatch(root, context));
    }

    private static Project CreateRelationTree(bool condition)
    {
        NamedStruct schema = new(["value"], TypeFactory.REQUIRED.Struct([TypeFactory.REQUIRED.I64]));
        NamedTableRead read = new(schema, ["orders"], filter: null);
        Filter filter = new(read, new BoolLiteral(condition));
        return new Project(filter, [new FieldReference(TypeFactory.REQUIRED.I64, 0)]);
    }

    private sealed class StringBuilderContext : NoOpContext<IRel, VoidOutput>
    {
        private readonly StringBuilder builder = new();

        public void Append(string value) => this.builder.Append(value).Append('|');

        public override string ToString() => this.builder.ToString();
    }

    private sealed class RelationPrinter : DefaultRelVisitor<StringBuilderContext, VoidOutput>
    {
        public override VoidOutput Visit(IRel other, StringBuilderContext context)
        {
            throw new NotSupportedException($"Unable to print relation {other.GetType().Name}");
        }

        protected override VoidOutput DefaultVisit(IRel relation, StringBuilderContext context)
        {
            context.Append(relation.GetType().Name);
            return VoidOutput.Instance;
        }
    }

    private sealed class ProjectFindingTopDownDispatcher
        : RelTopDownDispatcher<NoOpContext<IRel, bool>, bool>
    {
        public ProjectFindingTopDownDispatcher()
            : base(new ProjectFinder())
        {
        }

        protected override bool ShouldBailOut(bool result, NoOpContext<IRel, bool> context) => result;
    }

    private sealed class ProjectFindingBottomUpDispatcher
        : RelBottomUpDispatcher<NoOpContext<IRel, bool>, bool>
    {
        public ProjectFindingBottomUpDispatcher()
            : base(new ProjectFinder())
        {
        }

        protected override bool ShouldBailOut(bool result, NoOpContext<IRel, bool> context) => result;
    }

    private sealed class ProjectFinder : DefaultRelVisitor<NoOpContext<IRel, bool>, bool>
    {
        public override bool Visit(Project project, NoOpContext<IRel, bool> context) => true;

        protected override bool DefaultVisit(IRel relation, NoOpContext<IRel, bool> context) => false;
    }
}
