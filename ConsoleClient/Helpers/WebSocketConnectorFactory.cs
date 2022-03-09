//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    public class WebSocketConnectorFactory : IWebSocketConnectorFactory
    {
        public IWebSocketConnector CreateWebSocketConnector(WebSocket ws1, string name1, WebSocket ws2, string name2, DevExAPIConfig config)
        {
            return new WebSocketConnecter(ws1, name1, ws2, name2, config);
        }
    }
}
