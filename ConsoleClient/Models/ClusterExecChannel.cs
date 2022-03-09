//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ContainerApps.ProxyApi.Models
{
    /// <summary>
    /// see https://github.com/kubernetes/kubernetes/pull/13885
    /// </summary>
    public enum ClusterExecChannel
    {
        stdin,
        stdout,
        stderr,
        clusterEvent,
        terminalResize
    }
}
