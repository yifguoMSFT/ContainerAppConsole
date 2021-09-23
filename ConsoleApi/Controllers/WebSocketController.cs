using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public class WebSocketController : ControllerBase
    {
        [HttpGet("/echo")]
        public async Task Echo()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using (WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync())
                {
                    var buffer = new byte[1024 * 4];
                    bool closed = false;
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    while (!result.CloseStatus.HasValue && !closed)
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    }
                    if (!closed)
                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        [HttpGet("/string-echo")]
        public async Task StringEcho()
        {
            using (var webSocket = new WebSocketWrapper(4096, await HttpContext.WebSockets.AcceptWebSocketAsync()))
            {
                string s = null;
                while (webSocket.IsConnected)
                {
                    s = await webSocket.RecvAsync();
                    await webSocket.SendAsync(s);
                }
            }
        }

        [HttpGet("/console")]
        public async Task Console()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                bool busy = false;
                using (var webSocket = new WebSocketWrapper(4096, await HttpContext.WebSockets.AcceptWebSocketAsync()))
                using (var consoleManager = new ConsoleManager(async s => 
                {
                    if (s.StartsWith("end of child process"))
                    {
                        busy = false;
                        var splitted = s.Split('|');
                        await webSocket.SendAsync(JsonSerializer.Serialize(new { prefix = splitted[1] }));
                    }
                    else
                    {
                        await webSocket.SendAsync(JsonSerializer.Serialize(new { text = s }));
                    }
                    
                }))
                {

                    string input = null;
                    if (!webSocket.IsConnected)
                    {
                        throw new Exception("webSocket connection failed");
                    }
                    await consoleManager.SendAsync(@"echo end of child process\|$(hostname):$(pwd)");
                    while (webSocket.IsConnected)
                    {
                        input = await webSocket.RecvAsync();
                        if(input.StartsWith('"') && input.EndsWith('"'))
                        {
                            input = input.Substring(1, input.Length - 2);
                        }
                        var splitted = input.Split(' ', 2);
                        string cmd = splitted[0];
                        
                        if (cmd == "run" && splitted.Length == 2)
                        {
                            await consoleManager.SendAsync(splitted[1]);
                            if (!busy)
                            {
                                busy = true;
                                await consoleManager.SendAsync(@"echo end of child process\|$(hostname):$(pwd)");
                            }
                        }
                        else
                        {
                            await webSocket.SendAsync(JsonSerializer.Serialize(new { error = $"invalid input: {input}" }));
                        }
                    }
                }
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
