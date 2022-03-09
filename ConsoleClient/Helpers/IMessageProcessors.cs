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
    public interface IMessageProcessors
    {
        IMessageProcessor GenerateClientMessageProcessor(ExecRequestEnvelope requestEnvelope);

        IMessageProcessor GenerateClusterMessageProcessor(ExecRequestEnvelope requestEnvelope);
    }
}
