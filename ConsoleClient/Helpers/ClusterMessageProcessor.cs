//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.ApiResources;
using Microsoft.ContainerApps.ProxyApi.Controllers;
using Microsoft.ContainerApps.ProxyApi.Extensions;
using Microsoft.ContainerApps.ProxyApi.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    public class ClusterMessageProcessor : IMessageProcessor
    {
        private readonly IClientWebSocketHelper _clientWebSocketHelper;
        private readonly ILogger<ExecApiController> _logger;
        private readonly ExecRequestEnvelope _envelope;
        public ClusterMessageProcessor(IClientWebSocketHelper clientWebSocketHelper, ILogger<ExecApiController> logger, ExecRequestEnvelope envelope)
        {
            _clientWebSocketHelper = clientWebSocketHelper;
            _logger = logger;
            _envelope = envelope;
        }

        public (ArraySegment<byte> message, bool shouldForward) Process(ArraySegment<byte> bytesMessage)
        {
            (ArraySegment<byte> message, bool shouldForward) result;
            if (bytesMessage[0] == (byte)ClusterExecChannel.clusterEvent)
            {
                
                result.shouldForward = true;
                string json = Encoding.UTF8.GetString(bytesMessage.Slice(1));
                try
                {
                    var clusterStatus = JsonConvert.DeserializeObject<ClusterExecStatus>(json);
                    
                    if (clusterStatus.Status == "Success")
                    {
                        string msg = clusterStatus.Message ?? "received success status from cluster";
                        _logger.LogExecClusterStatusInfo(msg, json, _envelope);
                        result.message = _clientWebSocketHelper.Assemble(new ExecMessage(MessageType.Info, msg));
                    }
                    else
                    {
                        var errorEntity = ErrorMap.ClusterExecFailure.CreateErrorEntity(clusterStatus.Message, clusterStatus.Code);
                        _logger.LogExecClusterStatusError(clusterStatus.Message, json, _envelope);
                        result.message = _clientWebSocketHelper.Assemble(new ExecMessage(MessageType.Error, errorEntity));
                    }
                }
                catch (Exception e)
                {
                    _logger.LogExecClusterStatusError($"failed to parse cluster exec status, error: {e.Message}", json, _envelope);
                    var errorEntity = ErrorMap.UnknownClusterExecStatus.CreateErrorEntity(json);
                    result.message = _clientWebSocketHelper.Assemble(new ExecMessage(MessageType.Error, errorEntity));
                }
            }
            else
            {
                result = (_clientWebSocketHelper.Assemble(new ExecMessage(MessageType.Forward, bytesMessage)), true);
            }
            return result;
        }
    }
}
