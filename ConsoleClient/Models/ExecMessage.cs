//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ContainerApps.ProxyApi.Models
{
    public class ExecMessage : IExecMessage
    {
        public MessageType Type { get; private set; }
        public ArraySegment<byte> DataBytes { get; private set; }

        public ExecMessage(MessageType messageType, object obj)
        {
            Type = messageType;
            DataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)); ;
        }

        public ExecMessage(MessageType messageType, string s)
        {
            Type = messageType;
            DataBytes = Encoding.UTF8.GetBytes(s); ;
        }

        public ExecMessage(MessageType messageType, ArraySegment<byte> data)
        {
            Type = messageType;
            DataBytes = data;
        }

    }
}
