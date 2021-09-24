using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
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
            using (var webSocket = new WebSocketWrapper(await HttpContext.WebSockets.AcceptWebSocketAsync(), 4096))
            {
                string s = null;
                while (webSocket.IsConnected)
                {
                    s = await webSocket.RecvAsync();
                    await webSocket.SendAsync(s);
                }
            }
        }

        [HttpGet("/inter-console")]
        public async Task InterConsole()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using (var webSocket = new WebSocketWrapper(await HttpContext.WebSockets.AcceptWebSocketAsync(), 4096))
                {
                    try
                    {
                        if (!webSocket.IsConnected)
                        {
                            throw new Exception("webSocket connection failed");
                        }

                        string containerId = await TrySecondHandShakeAsync(webSocket);
                        string containerPid = await ProcessManager.RunAsync("nsenter", @"--target 1 --pid --ipc --mount --uts --net -- crictl inspect --output go-template --template {{.info.pid}} " + containerId);
                        int pid;
                        try
                        {
                            containerPid = containerPid.Trim();
                            pid = int.Parse(containerPid);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"failed to parse int {containerPid}");
                        }
                        await TryPrepareStaticBashAsync(pid);

                        ConsoleManager consoleManager = null;
                        using (consoleManager = new ConsoleManager(async s =>
                        {
                            if (s.StartsWith("end of child process"))
                            {
                                consoleManager.Busy = false;
                                var splitted = s.Split('|');
                                await webSocket.SendAsync(JsonConvert.SerializeObject(new { prefix = splitted[1] }));
                            }
                            else
                            {
                                await webSocket.SendAsync(JsonConvert.SerializeObject(new { text = s }));
                            }
                        }, pid, Environment.GetEnvironmentVariable("CONSOLE_API_PRIVILEGED_MODE") == "true"))
                        {
                            await consoleManager.SendAsync(@"cd /");
                            await consoleManager.SendAsync(@"echo end of child process\|$HOSTNAME:$(pwd)");
                            string input = null;
                            while (webSocket.IsConnected && consoleManager.IsRunning)
                            {
                                input = await webSocket.RecvAsync();
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
                            await webSocket.SendAsync(JsonConvert.SerializeObject(new { error = e }));
                        }
                    }
                }
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }



        [HttpGet("/console")]
        public async Task Console()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var buffer = new byte[4096];
                using (var webSocket = new WebSocketWrapper(await HttpContext.WebSockets.AcceptWebSocketAsync(), buffer: buffer))
                {
                    try
                    {
                        if (!webSocket.IsConnected)
                        {
                            throw new Exception("webSocket connection failed");
                        }
                        
                        string nodeIp = await TryFirstHandShakeAsync(webSocket);
                        using (var interWebSocket = new ClientWebSocket())
                        {
                            await interWebSocket.ConnectAsync(new Uri($"ws://{nodeIp}:10080/inter-console"), CancellationToken.None);
                            if (interWebSocket.State != WebSocketState.Open)
                            {
                                throw new Exception("Failed to connect to inter websocket");
                            }

                            using (var connector = new WebSocketConnecter(webSocket.WebSocket, interWebSocket, buffer, new byte[4096]))
                            {
                                await connector.ConnectAsync();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (webSocket.IsConnected)
                        {
                            await webSocket.SendAsync(JsonConvert.SerializeObject(new { error = e }));
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

       

        private async Task<string> TryFirstHandShakeAsync(WebSocketWrapper webSocket)
        {
            try
            {
                var input = await webSocket.RecvAsync();
                if (!input.StartsWith("set-node"))
                {
                    throw new Exception("the first handshake must be set-node [node ip]");
                }
                string ip = input.Split(' ', 2)[1];

                return ip;
            }
            catch (Exception e)
            {
                throw new Exception($"First handshake failed with exception: {e.Message}");
            }
        }

        private async Task<string> TrySecondHandShakeAsync(WebSocketWrapper webSocket)
        {
            try
            {
                string input = await webSocket.RecvAsync();
                if (!input.StartsWith("set-container"))
                {
                    throw new Exception("the second handshake must be set-container [container id]");
                }
                string id = input.Split(' ', 2)[1];
                return id;
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
    }
}
