//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ContainerApps.ProxyApi.Models
{
    public class ExecRequestEnvelope
    {
        public string SubscriptionId;
        public string ResourceGroupName;
        public string ContainerAppName;
        public string RevisionName;
        public string ReplicaName;
        public string ContainerName;
    }
}
