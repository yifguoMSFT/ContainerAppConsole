using Microsoft.ContainerApps.Common.Extensions;
using Microsoft.ContainerApps.ProxyApi.Helpers;
using Microsoft.ContainerApps.ProxyApi.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ClientWebSocket wsClient = null;
            bool isConnected = false;
            try
            {
                Console.Write("websocket url:");
                string url = Console.ReadLine();
                if (!url.StartsWith("ws://") && !url.StartsWith("wss://"))
                {
                    Console.WriteLine("invalid websocket url!");
                    Environment.Exit(-1);
                }
                wsClient = new ClientWebSocket();
                var wsOptions = wsClient.Options;
                var cert = GetCertificate("rdfetestsslclientcert.antares-test.windows-int.net");//new X509Certificate2(@"F:\Downloads\rdfe.cer");

                //wsOptions.ClientCertificates.Add(cert);
                //wsOptions.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
                await wsClient.ConnectAsync(new Uri(url), CancellationToken.None);
                if (!wsClient.EnsureConnected())
                {
                    throw new Exception("Failed to connect!");
                }
                Console.WriteLine("Connected!");
                isConnected = true;

                

                var wsHelper = new ClientWebSocketHelper();
                Task.Run(async () =>
                {
                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    while (isConnected)
                    {
                        var recvResult = await wsClient.ReceiveAsync(buffer, CancellationToken.None);
                        if (recvResult.MessageType == WebSocketMessageType.Close)
                        {
                            await wsClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                            isConnected = false;
                            break;
                        }
                        var recv = wsHelper.Disassemble(buffer.Slice(0, recvResult.Count));
                        var dataBytes = recv.DataBytes;
                        switch (recv.Type)
                        {
                            case MessageType.Info:
                                Console.WriteLine("INFO: " + Encoding.UTF8.GetString(dataBytes));
                                break;
                            case MessageType.Error:
                                Console.WriteLine("ERROR: " + Encoding.UTF8.GetString(dataBytes));
                                break;
                            case MessageType.Forward:
                                byte controlByte = dataBytes[0];
                                switch (controlByte)
                                {
                                    case (byte)ClusterExecChannel.stdout:
                                    case (byte)ClusterExecChannel.stderr:
                                        Console.Write(Encoding.UTF8.GetString(dataBytes.Slice(1)));
                                        break;
                                    default:
                                        throw new Exception("unexpected message received");
                                }
                                break;
                        }
                    }
                });

                Console.CancelKeyPress += async (object sender, ConsoleCancelEventArgs e) =>
                {
                    e.Cancel = true;
                    var message = new byte[] { (byte)ClusterExecChannel.stdin }.Concat(Encoding.UTF8.GetBytes(((char)3).ToString())).ToArray();
                    await wsHelper.SendMessageAsync(wsClient, new ExecMessage(MessageType.Forward, message));
                };

                while (isConnected)
                {
                    var key = Console.ReadKey(true);
                    var message = new byte[] { (byte)ClusterExecChannel.stdin }.Concat(Encoding.UTF8.GetBytes(key.KeyChar.ToString())).ToArray();
                    await wsHelper.SendMessageAsync(wsClient, new ExecMessage(MessageType.Forward, message));
                }

                Environment.Exit(0);
            }
            finally 
            {
                isConnected = false;
                await wsClient.EnsureClosedAsync(TimeSpan.FromSeconds(10));
                wsClient.Dispose();
            }
        }
        public static X509Certificate2 GetCertificate(string name)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var results = store.Certificates.Find(X509FindType.FindBySubjectName, name, false);
            if (results.Count == 0)
            {
                throw new Exception(string.Format("Certificate with thumbprint {0} not found", name));
            }

            store.Close();
            return results[0];
        }
    }
}
