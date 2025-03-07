﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode.Plugins;

public interface IBuildParameters
{
    IReadOnlyDictionary<string, JArray> TagParameters { get; }
}
