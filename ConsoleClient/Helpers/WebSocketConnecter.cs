//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.ContainerApps.Configuration;
using Microsoft.ContainerApps.ProxyApi.Exceptions;
using Microsoft.ContainerApps.ProxyApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.ContainerApps.ProxyApi.Helpers.IWebSocketConnector;

namespace Microsoft.ContainerApps.ProxyApi.Helpers
{
    using SocketDisconnectionEnvelope = Nullable<(NamedWebsocket ws, string message, WebSocketCloseStatus? closeStatus)>;
    public class WebSocketConnecter: IWebSocketConnector
    {
        private ArraySegment<byte> buffer1, buffer2;
        private NamedWebsocket ws1, ws2;
        private DevExAPIConfig config;
        private bool disconnected = false;
        private CancellationTokenSource cancellationTokenSource;
        private Task InactiveSessionTimer;
        private DateTime lastEventTimestamp;
        private string disconnectionReason;

        public WebSocketConnecter(WebSocket ws1, string name1, WebSocket ws2, string name2, DevExAPIConfig config)
        {
            this.ws1 = new NamedWebsocket(ws1, name1);
            this.ws2 = new NamedWebsocket(ws2, name2);
            this.config = config;
            buffer1 = new ArraySegment<byte>(new byte[config.WebSocketBufferSize]);
            buffer2 = new ArraySegment<byte>(new byte[config.WebSocketBufferSize]);
            cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// <param name="processor1"/> processes the message received from <see cref="ws1"/> which result will be written to <see cref="ws2"/> and vice versa
        /// <paramref name="sender1"/> and <paramref name="sender2"/> will be executed with a disconnection message before disconnecting so that they can let
        /// the other side of websocket connection knows why connection is terminated
        /// </summary>
        public async Task<string> ConnectAsync(IMessageProcessor processor1, DisconnectionMessageSender sender1, IMessageProcessor processor2, DisconnectionMessageSender sender2)
        {
            Task task1 = null, task2 = null;
            InactiveSessionTimer = Task.Run(async () =>
            {
                var interval = TimeSpan.FromSeconds(Math.Max(config.ExecSessionInactiveTimeoutInSeconds/10, 10));
                while (!disconnected)
                {
                    await Task.Delay(interval);
                    var elapsed = DateTime.UtcNow - lastEventTimestamp;
                    if (elapsed.TotalSeconds > config.ExecSessionInactiveTimeoutInSeconds)
                    {
                        string message = $"Connection terminated because of session is inactive for {config.ExecSessionInactiveTimeoutInSeconds} seconds.";
                        var task1 = sender1?.Invoke(message);
                        var task2 = sender2?.Invoke(message);
                        await Task.WhenAll(new[] { task1, task2 }.Where(t => t != null));
                        await DisconnectAsync((ws1, message, null), (ws2, message, null), message);
                        break;
                    }
                }
            });
            while (!disconnected)
            {
                if (task1?.IsCompleted != false)
                {
                    task1 = ws1
                        .ReceiveAsync(buffer1, cancellationTokenSource.Token)
                        .ContinueWith(t => ProcessCompletedTask(t, ws1, ws2, buffer1, processor1))
                        .Unwrap();
                }

                if (task2?.IsCompleted != false)
                {
                    task2 = ws2
                        .ReceiveAsync(buffer2, cancellationTokenSource.Token)
                        .ContinueWith(t => ProcessCompletedTask(t, ws2, ws1, buffer2, processor2))
                        .Unwrap();
                }

                await Task.WhenAny(task1, task2).Unwrap();
                lastEventTimestamp = DateTime.UtcNow;
            }

            return disconnectionReason;
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }

        private async Task ProcessCompletedTask(Task<WebSocketReceiveResult> task, NamedWebsocket recvFrom, NamedWebsocket sendTo, ArraySegment<byte> recvBuffer, IMessageProcessor processor)
        {
            if (task.IsCompleted)
            {
                if (!task.IsFaulted)
                {
                    //received from recvFrom
                    try
                    {
                        await ProcessWsRecvResultAsync(recvFrom, sendTo, task.Result, recvBuffer, processor);
                    }
                    catch (Exception e)
                    {
                        await DisconnectAsync(
                            (recvFrom, $"failed when sending message to {sendTo.Name}", null),
                            (sendTo, $"failed to send message received from {recvFrom.Name}", null));
                        throw new WebSocketConnectorException($"Failed to copy result received from {recvFrom.Name} to {sendTo.Name}", e);
                    }
                }
                else
                {
                    //faulted;
                    var exception = task.Exception.InnerException;
                    if (exception is WebSocketException)
                    {
                        if (exception.InnerException is AspNetCore.Connections.ConnectionResetException)
                        {
                            // recvFrom connection other side dropped connection without sending a close
                            await DisconnectAsync(
                                (recvFrom, $"connection dropped by {recvFrom.Name}", null),
                                (sendTo, $"connection with {recvFrom.Name} was dropped", null));
                            throw new WebSocketConnectorException($"connection dropped by {recvFrom.Name}", exception);
                        }
                        else if (recvFrom.State != WebSocketState.Open)
                        {
                            await DisconnectAsync(
                                (recvFrom, $"websocket is in an unexpected state {recvFrom.State}", null),
                                (sendTo, $"connection with {recvFrom.Name} is in an unexpected state {recvFrom.State}", null));
                            throw new WebSocketConnectorException($"{recvFrom.Name} websocket is in an unexpected state {recvFrom.State}", exception);
                        }
                    }
                    else if (disconnected == true || exception is OperationCanceledException)
                    {
                        // connection is already terminated, abandon the result
                    }
                    else
                    {
                        // unknown exception
                        string errorMessage = $"unknown error from the connection with {recvFrom.Name}";
                        await DisconnectAsync((recvFrom, errorMessage, null), (sendTo, errorMessage, null));
                        throw new WebSocketConnectorException($"unknown error from the connection with {recvFrom.Name}", exception);
                    }
                }
            }
        }

        private async Task DisconnectAsync(SocketDisconnectionEnvelope envelope1, SocketDisconnectionEnvelope envelope2, string disconnectionReason = "normal closure")
        {
            if (disconnected)
            {
                throw new WebSocketConnectorException("Connection is already disconnected");
            }

            try
            {
                var timeout = TimeSpan.FromSeconds(config.WebSocketMessageTimeoutInSeconds);
                var exceptionPairs = new List<(NamedWebsocket ws, Exception e)>();
                var tasks = new[] { envelope1.Value, envelope2.Value }
                    .Select(async p =>
                    {
                        if (p.ws.State == WebSocketState.Open || p.ws.State == WebSocketState.CloseReceived)
                        {
                            try
                            {
                                var closeStatus = p.closeStatus ?? WebSocketCloseStatus.NormalClosure;
                                await CloseWebSocketAsync(p.ws, closeStatus, p.message, timeout);
                            }
                            catch (Exception e)
                            {
                                exceptionPairs.Add((p.ws, e));
                            }
                        }
                    });
                await Task.WhenAll(tasks);
                if (exceptionPairs.Any())
                {
                    string errorMessage = $"WebSocket {string.Join(" and ", exceptionPairs.Select(p => p.ws.Name))} not closed successfully, error: {string.Join(";", exceptionPairs.Select(p => p.e.Message))}";
                    throw new WebSocketConnectorException(errorMessage, new AggregateException(exceptionPairs.Select(p => p.e)));
                }
            }
            finally
            {
                this.disconnectionReason = disconnectionReason;
                disconnected = true;
                cancellationTokenSource.Cancel();
            }
        }

        private async Task CloseWebSocketAsync(WebSocket ws, WebSocketCloseStatus closeStatus, string closeMessage, TimeSpan timeout)
        {
            var tokenSource = new CancellationTokenSource(timeout);
            try
            {
                await ws.CloseOutputAsync(closeStatus, closeMessage, tokenSource.Token);
            }
            catch (OperationCanceledException e)
            {
                throw new OperationCanceledException($"Failed to close the WebSocket connection after {timeout.TotalSeconds} seconds timeout", e);
            }
        }

        private async Task ProcessWsRecvResultAsync(NamedWebsocket recvFrom, NamedWebsocket sendTo, WebSocketReceiveResult recvResult, ArraySegment<byte> recvBuffer, IMessageProcessor processor)
        {
            if (disconnected)
            {
                // already disconnected, abandon the result;
                return;
            }
            else
            {
                if (recvResult.MessageType == WebSocketMessageType.Close)
                {
                    // recvFrom ws connection's other side initialized a close
                    await DisconnectAsync(
                        (recvFrom, "CLOSE received", null),
                        (sendTo, recvResult.CloseStatusDescription, recvResult.CloseStatus),
                        $"CLOSE received from {recvFrom.Name}.");
                }
                else
                {
                    var processedMessagePair = processor.Process(recvBuffer.Slice(0, recvResult.Count));
                    if (processedMessagePair.shouldForward)
                    {
                        await sendTo.SendAsync(processedMessagePair.message, recvResult.MessageType, recvResult.EndOfMessage, cancellationTokenSource.Token);
                    }
                }
            }
        }
    }
}
