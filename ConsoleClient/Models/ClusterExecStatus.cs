//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ContainerApps.ProxyApi.Models
{
    /// <summary>
    /// Data structure that k8s cluster exec API returns before closing the connection, see 
    /// https://github.com/kubernetes/kubernetes/blob/master/pkg/kubelet/cri/streaming/remotecommand/exec.go
    /// </summary>
    public class ClusterExecStatus
    {
        public object Metadata;
        public string Status;
        public string Message;
        public string Reason;
        public object Details;
        public int Code;
    }
}
