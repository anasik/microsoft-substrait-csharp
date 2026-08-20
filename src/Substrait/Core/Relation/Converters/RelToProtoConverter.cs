using Substrait.Core.Expression.Converters;
using Substrait.Core.Plan.Converters;
using Substrait.Core.Type.Converters;
using Substrait.Protobuf;
using Substrait.Tools;
using ProtoExpression = Substrait.Protobuf.Expression;
using ProtoRel = Substrait.Protobuf.Rel;
using ProtoType = Substrait.Protobuf.Type;

namespace Substrait.Core.Relation.Converters;

/// <summary>
/// Converts internal relations to protobuf relations.
/// </summary>
public class RelToProtoConverter
{
    private readonly RelBottomUpDispatcher<PlanToProtoConverter.ConverterContext, ProtoRel> dispatcher;

    /// <summary>Initializes a relation converter.</summary>
    public RelToProtoConverter()
    {
        var typeConverter = new TypeToProtoConverter();
        var expressionConverter = new ExpressionToProtoConverter(typeConverter, this);
        this.dispatcher = new(new RelToProtoVisitor(expressionConverter, typeConverter));
    }

    /// <summary>Converts a relation using a new context.</summary>
    public ProtoRel From(IRel relation) => this.From(relation, new PlanToProtoConverter.ConverterContext());

    /// <summary>Converts a relation using a shared context.</summary>
    public ProtoRel From(IRel relation, PlanToProtoConverter.ConverterContext context) =>
        this.dispatcher.Dispatch(relation, context);

    private sealed class RelToProtoVisitor : RelVisitor<PlanToProtoConverter.ConverterContext, ProtoRel>
    {
        private readonly ExpressionToProtoConverter expressionConverter;
        private readonly TypeToProtoConverter typeConverter;

        public RelToProtoVisitor(ExpressionToProtoConverter expressionConverter, TypeToProtoConverter typeConverter)
        {
            this.expressionConverter = expressionConverter;
            this.typeConverter = typeConverter;
        }

        public override ProtoRel Visit(Filter relation, PlanToProtoConverter.ConverterContext context) =>
            new() { Filter = new() { Input = context.GetOutput(relation.Input), Condition = this.expressionConverter.From(relation.Condition, context), Common = Common(relation.Transmute) } };

        public override ProtoRel Visit(Cross relation, PlanToProtoConverter.ConverterContext context) =>
            new() { Cross = new() { Left = context.GetOutput(relation.Left), Right = context.GetOutput(relation.Right), Common = Common(relation.Transmute) } };

        public override ProtoRel Visit(Project relation, PlanToProtoConverter.ConverterContext context)
        {
            var project = new ProjectRel { Input = context.GetOutput(relation.Input), Common = Common(relation.Transmute) };
            project.Expressions.AddRange(relation.Expressions.Select(expression => this.expressionConverter.From(expression, context)));
            return new ProtoRel { Project = project };
        }

        public override ProtoRel Visit(NamedTableRead relation, PlanToProtoConverter.ConverterContext context)
        {
            ReadRel read = this.CreateRead(relation, context);
            read.NamedTable = new() { Names = { relation.Names } };
            return new ProtoRel { Read = read };
        }

        public override ProtoRel Visit(VirtualTableRead relation, PlanToProtoConverter.ConverterContext context)
        {
            ReadRel read = this.CreateRead(relation, context);
            read.VirtualTable = new();
            read.VirtualTable.Expressions.AddRange(relation.Rows.Select(row =>
            {
                var structure = new ProtoExpression.Types.Nested.Types.Struct();
                structure.Fields.AddRange(row.Fields.Select(field => this.expressionConverter.From(field, context)));
                return structure;
            }));
            return new ProtoRel { Read = read };
        }

        public override ProtoRel Visit(EmptyRead relation, PlanToProtoConverter.ConverterContext context) =>
            new() { Read = this.CreateRead(relation, context) };

        public override ProtoRel Visit(Sort relation, PlanToProtoConverter.ConverterContext context)
        {
            var sort = new SortRel { Input = context.GetOutput(relation.Input), Common = Common(relation.Transmute) };
            sort.Sorts.AddRange(relation.SortFields.Select(field => new SortField
            {
                Expr = this.expressionConverter.From(field.Expr, context),
                Direction = field.Direction.ToProto(),
            }));
            return new ProtoRel { Sort = sort };
        }

        public override ProtoRel Visit(Fetch relation, PlanToProtoConverter.ConverterContext context) =>
            new()
            {
                Fetch = new()
                {
                    Input = context.GetOutput(relation.Input),
                    CountExpr = this.expressionConverter.From(relation.Count, context),
                    OffsetExpr = this.expressionConverter.From(relation.Offset, context),
                    Common = Common(relation.Transmute),
                },
            };

        public override ProtoRel Visit(Set relation, PlanToProtoConverter.ConverterContext context)
        {
            var set = new SetRel { Op = relation.SetOperation.ToProto(), Common = Common(relation.Transmute) };
            set.Inputs.AddRange(relation.Inputs.Select(context.GetOutput));
            return new ProtoRel { Set = set };
        }

        public override ProtoRel Visit(Aggregate relation, PlanToProtoConverter.ConverterContext context) => Unsupported(relation);
        public override ProtoRel Visit(Join relation, PlanToProtoConverter.ConverterContext context) => Unsupported(relation);
        public override ProtoRel Visit(HashJoin relation, PlanToProtoConverter.ConverterContext context) => Unsupported(relation);
        public override ProtoRel Visit(ScatterExchange relation, PlanToProtoConverter.ConverterContext context) => Unsupported(relation);
        public override ProtoRel Visit(SingleBucketExchange relation, PlanToProtoConverter.ConverterContext context) => Unsupported(relation);
        public override ProtoRel Visit(IRel other, PlanToProtoConverter.ConverterContext context) => Unsupported(other);

        private static ProtoRel Unsupported(IRel relation) =>
            throw new NotImplementedException($"Conversion for {relation.GetType().Name} is not implemented.");

        private static RelCommon Common(Remap? remap) => remap is null
            ? new RelCommon { Direct = new() }
            : new RelCommon { Emit = new() { OutputMapping = { remap.Indices } } };

        private ReadRel CreateRead(Read relation, PlanToProtoConverter.ConverterContext context)
        {
            var read = new ReadRel
            {
                BaseSchema = this.CreateNamedStruct(relation.InitialSchema, context),
                Common = Common(relation.Transmute),
            };
            if (relation.Filter is not null)
            {
                read.Filter = this.expressionConverter.From(relation.Filter, context);
            }

            return read;
        }

        private NamedStruct CreateNamedStruct(Core.Type.NamedStruct schema, PlanToProtoConverter.ConverterContext context)
        {
            var structure = new ProtoType.Types.Struct { Nullability = schema.Struct.Nullable.ToProto() };
            structure.Types_.AddRange(schema.Struct.Fields.Select(field => this.typeConverter.From(field, context)));
            return new NamedStruct { Names = { schema.Names }, Struct = structure };
        }
    }
}
