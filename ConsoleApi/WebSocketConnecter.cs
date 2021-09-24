using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public class WebSocketConnecter:IDisposable
    {
        private byte[] bufferA, bufferB;
        private WebSocket a, b;
        private bool disconnected = false;
        private CancellationTokenSource cancellationTokenSource;

        public WebSocketConnecter(WebSocket a, WebSocket b, byte[] bufferA, byte[] bufferB)
        {
            this.a = a;
            this.b = b;
            this.bufferA = bufferA;
            this.bufferB = bufferB;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ConnectAsync()
        {
            Task<WebSocketReceiveResult> taskA = null, taskB = null;
            while (!disconnected)
            {
                if (taskA?.IsCompleted != false)
                {
                    taskA = a.ReceiveAsync(new ArraySegment<byte>(bufferA), cancellationTokenSource.Token);
                }

                if (taskB?.IsCompleted != false)
                {
                    taskB = b.ReceiveAsync(new ArraySegment<byte>(bufferB), cancellationTokenSource.Token);
                }

                await Task<WebSocketReceiveResult>.WhenAny(taskA, taskB);

                if (taskA.IsCompleted)
                {
                    await CopyAsync(b, taskA.Result, bufferA);
                }
                else
                {
                    await CopyAsync(a, taskB.Result, bufferB);
                }
            }
        }


        private async Task CopyAsync(WebSocket ws, WebSocketReceiveResult result, byte[] buffer)
        {
            await ws.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
            if (result.CloseStatus.HasValue)
            {
                disconnected = true;
                cancellationTokenSource.Cancel();
            }
        }

        public void Dispose()
        {
            disconnected = true;
            cancellationTokenSource.Dispose();
        }
        
    }
}
