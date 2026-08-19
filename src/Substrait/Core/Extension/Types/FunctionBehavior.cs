// <copyright file="FunctionBehavior.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Substrait.Core.Extension.Types;

/// <summary>
/// Function behavior.
/// </summary>
public enum FunctionBehavior
{
    /// <summary>
    /// System preferred variation implicitly also support this variation.
    /// </summary>
    INHERITS,

    /// <summary>
    /// This type variation must be resolved independently.
    /// </summary>
    SEPARATE,
}
