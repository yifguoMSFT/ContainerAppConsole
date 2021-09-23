using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public class WebSocketWrapper:IDisposable
    {
        private byte[] buffer;
        private WebSocket webSocket;

        private bool isConnected = false;
        public bool IsConnected => isConnected;
        public WebSocketWrapper(int bufferSize, WebSocket webSocket)
        {
            this.webSocket = webSocket;
            this.buffer = new byte[bufferSize];
            if (webSocket.State == WebSocketState.Open)
            {
                isConnected = true;
            }
            else
            {
                throw new Exception($"unexpected webSocket state: {webSocket.State}");
            }
        }

        public void Dispose()
        {
            webSocket.Dispose();
        }

        public Task SendAsync(string msg)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            return webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None); ;
        }

        public async Task<string> RecvAsync()
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string s = null;
            if (result.EndOfMessage)
            {
                s = Encoding.UTF8.GetString(buffer, 0, result.Count);
            }
            else
            { 
                //TODO: keep recieving until end of message
            }
            if (result.CloseStatus.HasValue)
            {
                await CloseAsync();
            }
            return s;
        }

        public Task CloseAsync()
        {
            isConnected = false;
            return webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        
    }
}
