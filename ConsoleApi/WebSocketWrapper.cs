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
        public WebSocket WebSocket;
        public bool IsConnected => WebSocket.State == WebSocketState.Open;
        public WebSocketWrapper(WebSocket webSocket, int? bufferSize = 4096, byte[] buffer = null)
        {
            this.WebSocket = webSocket;
            this.buffer = buffer ?? new byte[bufferSize.Value];
            if(!IsConnected)
            {
                throw new Exception($"unexpected webSocket state: {webSocket.State}");
            }
        }

        public void Dispose()
        {
            try
            {
                WebSocket.Dispose();
            }
            catch (Exception e)
            { 
            }
        }

        public Task SendAsync(string msg)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            return WebSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None); ;
        }

        public Task SendAsync(byte[] b)
        {
            return WebSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None); ;
        }

        public async Task<string> RecvAsync()
        {
            var result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
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

            if (s.StartsWith('"') && s.EndsWith('"'))
            {
                s = s.Substring(1, s.Length - 2);
            }
            return s;
        }

        public Task CloseAsync()
        {
            return WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        
    }
}
