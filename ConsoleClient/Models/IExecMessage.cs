//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Microsoft.ContainerApps.ProxyApi.Models
{
    public interface IExecMessage
    {
        MessageType Type { get; }

        ArraySegment<byte> DataBytes { get; }
    }
}
