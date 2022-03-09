//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.ProxyApi.Models;
using System;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    public class ClientMessageProcessor : IMessageProcessor
    {
        private readonly IClientWebSocketHelper _clientWebSocketHelper;
        public ClientMessageProcessor(IClientWebSocketHelper clientWebSocketHelper)
        {
            _clientWebSocketHelper = clientWebSocketHelper;
        }

        public (ArraySegment<byte> message, bool shouldForward) Process(ArraySegment<byte> bytesMessage)
        {
            var message = _clientWebSocketHelper.Disassemble(bytesMessage);
            if (message.Type == MessageType.Forward)
            {
                return (message.DataBytes, true);
            }
            else
            {
                // TODO: handle the info and error message from client
                return (null, false);
            }
        }
    }
}
