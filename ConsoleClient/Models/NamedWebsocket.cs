//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Models
{
    /// <summary>
    /// Just a simple wrapper to "extends" a name property for WebSocket class
    /// </summary>
    public class NamedWebsocket : WebSocket
    {
        private readonly WebSocket ws;
        public readonly string Name;

        public NamedWebsocket(WebSocket ws, string name)
        {
            this.ws = ws;
            Name = name;
        }

        #region auto generated WebSocket abstract methods implementation based on this.ws
        public override WebSocketCloseStatus? CloseStatus => ws.CloseStatus;

        public override string CloseStatusDescription => ws.CloseStatusDescription;

        public override WebSocketState State => ws.State;

        public override string SubProtocol => ws.SubProtocol;

        public override void Abort()
        {
            ws.Abort();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return ws.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return ws.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override void Dispose()
        {
            ws.Dispose();
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return ws.ReceiveAsync(buffer, cancellationToken);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return ws.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
        #endregion
    }
}
