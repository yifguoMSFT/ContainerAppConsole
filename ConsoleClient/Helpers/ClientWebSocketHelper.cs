//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.Common.Extensions;
using Microsoft.ContainerApps.ProxyApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    public class ClientWebSocketHelper : IClientWebSocketHelper
    {
        public byte[] Assemble(IExecMessage message)
        {
            var bytes = new byte[message.DataBytes.Count + 1];
            bytes[0] = (byte)message.Type;
            message.DataBytes.CopyTo(bytes, 1);
            return bytes;
        }

        public IExecMessage Disassemble(ArraySegment<byte> bytes)
        {
            var messageType = (MessageType)bytes[0];
            var message = new ExecMessage(messageType, bytes.Slice(1));
            return message;
        }

        public async Task<IExecMessage> ReceiveMessageAsync(WebSocket ws, byte[] buffer, CancellationToken token)
        {
            ArraySegment<byte> bytes = buffer;
            var result = await ws.ReceiveAsync(bytes, token);
            var message = Disassemble(bytes.Slice(0, result.Count));
            return message;
        }

        public async Task SendMessageAsync(WebSocket ws, IExecMessage message)
        {
            var bytes = Assemble(message);
            await ws.SendAsync(bytes);
        }
    }
}
