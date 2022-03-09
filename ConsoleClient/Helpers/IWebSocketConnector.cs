//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    public interface IWebSocketConnector : IDisposable
    {
        public delegate Task DisconnectionMessageSender(string message);
        public Task<string> ConnectAsync(IMessageProcessor processor1, DisconnectionMessageSender sender1, IMessageProcessor processor2, DisconnectionMessageSender sender2);
    }
}
