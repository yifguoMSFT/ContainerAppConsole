//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.ProxyApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{

    public interface IClientWebSocketHelper
    {
        byte[] Assemble(IExecMessage message);

        IExecMessage Disassemble(ArraySegment<byte> message);

        Task SendMessageAsync(WebSocket ws, IExecMessage message);

        Task<IExecMessage> ReceiveMessageAsync(WebSocket ws, byte[] buffer, CancellationToken token);
    }
}
