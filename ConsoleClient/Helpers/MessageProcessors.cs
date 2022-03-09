//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.ProxyApi.Controllers;
using Microsoft.ContainerApps.ProxyApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    public class MessageProcessors : IMessageProcessors
    {
        private readonly IClientWebSocketHelper _clientWebSocketHelper;
        private readonly ILogger<ExecApiController> _logger;
        public MessageProcessors(IClientWebSocketHelper clientWebSocketHelper, ILogger<ExecApiController> logger)
        {
            _clientWebSocketHelper = clientWebSocketHelper;
            _logger = logger;
        }

        public IMessageProcessor ClientMessageProcessor => new ClientMessageProcessor(_clientWebSocketHelper);


        public IMessageProcessor GenerateClientMessageProcessor(ExecRequestEnvelope requestEnvelope)
        {
            return new ClientMessageProcessor(_clientWebSocketHelper);
        }

        public IMessageProcessor GenerateClusterMessageProcessor(ExecRequestEnvelope requestEnvelope)
        {
            return new ClusterMessageProcessor(_clientWebSocketHelper, _logger, requestEnvelope);
        }
    }
}
