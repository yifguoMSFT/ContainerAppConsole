//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.ProxyApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    public interface IMessageProcessor
    {
        (ArraySegment<byte> message, bool shouldForward) Process(ArraySegment<byte> bytesMessage);
    }
}
