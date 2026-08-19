// <copyright file="INavigableNode.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Substrait.Tools.Visitor;

/// <summary>
/// Interface for a node in a navigable tree-like structure.
/// </summary>
/// <typeparam name="TNode">The type of the nodes in the structure.</typeparam>
public interface INavigableNode<TNode>
    where TNode : INavigableNode<TNode>
{
    /// <summary>
    /// Gets the input nodes of the current node.
    /// </summary>
    public IEnumerable<TNode> InputNodes { get; }
}
