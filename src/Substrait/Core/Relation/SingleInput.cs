// <copyright file="SingleInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Immutable;

namespace Substrait.Core.Relation;

/// <summary>
/// Relational operator with a single input.
/// </summary>
public abstract class SingleInput : Rel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleInput"/> class.
    /// </summary>
    protected SingleInput()
    {
    }

    /// <summary>
    /// Gets input.
    /// </summary>
    public abstract IRel Input { get; }

    /// <inheritdoc/>
    public override sealed IReadOnlyList<IRel> Inputs => ImmutableList.Create(this.Input);
}
