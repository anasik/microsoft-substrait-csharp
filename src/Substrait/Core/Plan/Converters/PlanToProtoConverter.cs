using Substrait.Core.Expression;
using Substrait.Core.Extension;
using Substrait.Core.Relation;
using Substrait.Core.Type;
using Substrait.Tools.Visitor;
using ProtoExpression = Substrait.Protobuf.Expression;
using ProtoRel = Substrait.Protobuf.Rel;
using ProtoType = Substrait.Protobuf.Type;

namespace Substrait.Core.Plan.Converters;

/// <summary>
/// Converts internal plans to protobuf plans.
/// </summary>
public class PlanToProtoConverter
{
    /// <summary>
    /// Stores intermediate converter outputs and collected extensions.
    /// </summary>
    public class ConverterContext : IContext<IRel, ProtoRel>, IContext<IExpression, ProtoExpression>, IContext<IType, ProtoType>
    {
        private readonly ExtensionsCollector.Builder extensions = new();
        private readonly Context<IExpression, ProtoExpression> expressions = new();
        private readonly Context<IRel, ProtoRel> relations = new();
        private readonly Context<IType, ProtoType> types = new();

        /// <summary>Gets the collected extension declarations.</summary>
        public ExtensionsCollector ExtensionsCollector => this.extensions.Build();

        /// <summary>Collects an extension and returns its anchor.</summary>
        public int AddExtension(ExtensionsCollector.ExtensionType type, string uri, string name) =>
            this.extensions.Collect(type, uri, name);

        /// <inheritdoc/>
        public ProtoRel GetOutput(IRel node) => this.relations.GetOutput(node);

        /// <inheritdoc/>
        public void AddOutput(IRel node, ProtoRel output) => this.relations.AddOutput(node, output);

        /// <inheritdoc/>
        public void RemoveOutput(IRel node) => this.relations.RemoveOutput(node);

        /// <inheritdoc/>
        public ProtoExpression GetOutput(IExpression node) => this.expressions.GetOutput(node);

        /// <inheritdoc/>
        public void AddOutput(IExpression node, ProtoExpression output) => this.expressions.AddOutput(node, output);

        /// <inheritdoc/>
        public void RemoveOutput(IExpression node) => this.expressions.RemoveOutput(node);

        /// <inheritdoc/>
        public ProtoType GetOutput(IType node) => this.types.GetOutput(node);

        /// <inheritdoc/>
        public void AddOutput(IType node, ProtoType output) => this.types.AddOutput(node, output);

        /// <inheritdoc/>
        public void RemoveOutput(IType node) => this.types.RemoveOutput(node);
    }
}
