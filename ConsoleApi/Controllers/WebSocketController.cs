using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
               
                using (var webSocket = new WebSocketWrapper(4096, await HttpContext.WebSockets.AcceptWebSocketAsync()))
                {
                    try
                    {
                        if (!webSocket.IsConnected)
                        {
                            throw new Exception("webSocket connection failed");
                        }
                        
                        (string pod, int pid) = await TryHandShakeAsync(webSocket);
                        await TryPrepareStaticBashAsync(pid);

                        ConsoleManager consoleManager = null;
                        using (consoleManager = new ConsoleManager(async s =>
                        {
                            if (s.StartsWith("end of child process"))
                            {
                                consoleManager.Busy = false;
                                var splitted = s.Split('|');
                                await webSocket.SendAsync(JsonSerializer.Serialize(new { prefix = splitted[1] }));
                            }
                            else
                            {
                                await webSocket.SendAsync(JsonSerializer.Serialize(new { text = s }));
                            }
                        }))
                        {
                            await consoleManager.SendAsync(@"cd /");
                            await consoleManager.SendAsync(@"echo end of child process\|$HOSTNAME:$(pwd)");
                            string input = null;
                            while (webSocket.IsConnected && consoleManager.IsRunning)
                            {
                                input = await webSocket.RecvAsync();
                                input = RemoveStartAndEndQutation(input);
                                var splitted = input.Split(' ', 2);
                                string cmd = splitted[0];

                                if (cmd == "run" && splitted.Length == 2)
                                {
                                    await consoleManager.SendAsync(splitted[1]);
                                    if (!consoleManager.Busy)
                                    {
                                        consoleManager.Busy = true;
                                        await consoleManager.SendAsync(@"echo end of child process\|$HOSTNAME:$(pwd)");
                                    }
                                }
                                else if (cmd == "reset")
                                {
                                    await ResetAsync(consoleManager, pid);
                                }
                                else
                                {
                                    throw new Exception($"invalid input: {input}");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (webSocket.IsConnected)
                        {
                            await webSocket.SendAsync(JsonSerializer.Serialize(new { error = e.Message }));
                        }
                    }
                }
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        private async Task ResetAsync(ConsoleManager consoleManager, int pid)
        {
            consoleManager.Reset();
            await consoleManager.SendAsync(@"cd /");
            await consoleManager.SendAsync(@"echo end of child process\|$HOSTNAME:$(pwd)");
        }

       

        private async Task<(string pod, int pid)> TryHandShakeAsync(WebSocketWrapper webSocket)
        {
            try
            {
                var input = await webSocket.RecvAsync();
                input = RemoveStartAndEndQutation(input);
                if (!input.StartsWith("set-pod"))
                {
                    throw new Exception("the first operation must be set-pod [pod name]");
                }
                string pod = input.Split(' ', 2)[1];

                input = await webSocket.RecvAsync();
                input = RemoveStartAndEndQutation(input);
                if (!input.StartsWith("set-container"))
                {
                    throw new Exception("the second operation must be set-container [container pid]");
                }
                string pid = input.Split(' ', 2)[1];
                return (pod, int.Parse(pid));
            }
            catch (Exception e)
            {
                throw new Exception($"Handshake failed with exception: {e.Message}");
            }
        }

        private async Task TryPrepareStaticBashAsync(int pid)
        {
            ConsoleManager consoleManager = null;
            using (consoleManager = new ConsoleManager(s => { }))
            {
                if (!Directory.Exists($"/proc/{pid}"))
                {
                    throw new Exception($"pid {pid} doesn't exist on {Environment.MachineName}");
                }

                try
                {
                    System.IO.File.Copy("bash-static", $"/proc/{pid}/root/bash-static", false);
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("exists"))
                    {
                        throw;
                    }
                }
                await consoleManager.SendAsync($"chmod 544 /proc/{pid}/root/bash-static");
            }
        }

        private string RemoveStartAndEndQutation(string input)
        {
            if (input.StartsWith('"') && input.EndsWith('"'))
            {
                input = input.Substring(1, input.Length - 2);
            }
            return input;
        }
    }
}
