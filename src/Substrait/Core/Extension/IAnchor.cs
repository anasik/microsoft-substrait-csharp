// <copyright file="IAnchor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Substrait.Core.Extension;

/// <summary>
/// Anchor to identify a specific declaration in an extension.
/// </summary>
public interface IAnchor
{
    /// <summary>
    /// Gets namespace of the anchor.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Gets key of the anchor.
    /// </summary>
    string Key { get; }
}
