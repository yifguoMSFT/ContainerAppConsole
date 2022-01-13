using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using k8s;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Net.Security;

namespace ConsoleApi
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        [Route("ok")]
        [HttpGet]
        public IActionResult Ok()
        {
            return Ok("ok");
        }

        [Route("containers")]
        public async Task<IActionResult> GetContainers(string id, string ip = null)
        {
            if (ip == null)
            {
                return await GetLocalContainers(id);
            }
            else
            {
                try
                {
                    var resp = await httpClient.GetAsync($"http://{ip}:10080/api/containers?id={id}");
                    var conent = await resp.Content.ReadAsStringAsync();
                    return Ok(conent);
                }
                catch (Exception e)
                {
                    return StatusCode(500, JsonConvert.SerializeObject(e));
                }
            }
        }

        [Route("local-containers")]
        public async Task<IActionResult> GetLocalContainers(string id = null)
        {
            try
            {
                string result = await ProcessManager.RunAsync("nsenter", $"--target 1 --pid --ipc --mount --uts --net -- crictl ps" + (id == null ? "":$" --pod {id}") + " -o json");
                var json = JsonConvert.DeserializeObject<JToken>(result);
                var containers = json["containers"].Select(t => new { name = t["metadata"]["name"], id = t["id"] }).OrderBy(t => t.name);
                return Ok(JsonConvert.SerializeObject(containers));
            }
            catch (Exception e)
            {
                return StatusCode(500, JsonConvert.SerializeObject(e));
            }
        }

        private HttpClient httpClient = new HttpClient();

        [Route("containerApps/{cappName}/consoleWebsocketUrl")]
        public async Task<IActionResult> GetConsoleWebsocketUrl(string cappName, string podName)
        {
            try
            {
                var config = KubernetesClientConfiguration.InClusterConfig();
                var client = new Kubernetes(config);
                var podList = client.ListNamespacedPod("k8se-apps");
                var pod = podList.Items.Where(p => p.Metadata.Labels["containerapps.io/app-name"] == cappName && p.Metadata.Name == podName).FirstOrDefault();
                if (pod == null)
                {
                    return StatusCode(404, $"Pod {podName} not found");
                }

                string url = $"ws://console-api.eastus.cloudapp.azure.com/console?podName={pod.Metadata.Name}";
                return Ok(url);

            }
            catch (Exception e)
            {
                return StatusCode(500, JsonConvert.SerializeObject(e));
            }
        }


        [Route("containerApps/{cappName}/pods")]
        public async Task<IActionResult> GetPods(string cappName)
        {
            try
            {
                var config = KubernetesClientConfiguration.InClusterConfig();
                var client = new Kubernetes(config);
                var podList = client.ListNamespacedPod("k8se-apps");
                var pods = podList.Items.Where(p => p.Metadata.Labels["containerapps.io/app-name"] == cappName).ToList();
                var podNames = pods.Select(p => p.Metadata.Name).ToList();
                return Ok(JsonConvert.SerializeObject(podNames));

            }
            catch (Exception e)
            {
                return StatusCode(500, JsonConvert.SerializeObject(e));
            }
        }


        [Route("containerApps")]
        public async Task<IActionResult> GetContainerApps() 
        {
            try
            {
                var config = KubernetesClientConfiguration.InClusterConfig();
                var client = new Kubernetes(config);
                var apps = ((JObject)client.ListNamespacedCustomObject("k8se.microsoft.com", "v1alpha1", "k8se-apps", "containerapps")).ToObject<AppList>();
                var appNames = apps.Items.Select(a => a.Metadata.Name).ToList();
                return Ok(JsonConvert.SerializeObject(appNames));
            }
            catch (Exception e)
            {

                return Ok(JsonConvert.SerializeObject(e));
            }
        }

        [Route("test")]
        public async Task<IActionResult> Test(string args = null)
        {
            k8s.Models.V1Pod pod = null;
            var list = new List<string>();
            try
            {
                var config = KubernetesClientConfiguration.InClusterConfig();
                return Ok(JsonConvert.SerializeObject(config));
                //config.AccessToken = System.IO.File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token");
                var client = new Kubernetes(config);
                //var apps = ((JObject)client.ListClusterCustomObject("k8se.microsoft.com", "v1alpha1", "apps")).ToObject<AppList>();
                var pods = client.ListNamespacedPod("default");
                pod = pods.Items.Where(p => p.Metadata.Name.StartsWith("jeff-aci-helloworld")).First();

                /*using (var wsClient = new ClientWebSocket())
                {
                    /*var wsOptions = wsClient.Options;
                    wsOptions.SetRequestHeader("Authorization", $"Bearer { System.IO.File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token")}");
                    wsOptions.ClientCertificates.Add(new X509Certificate2("/var/run/secrets/kubernetes.io/serviceaccount/ca.crt"));
                    wsOptions.AddSubProtocol("v4.channel.k8s.io");
                    wsOptions.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
                    await wsClient.ConnectAsync(new Uri($"wss://kubernetes.default.svc/api/v1/namespaces/default/pods/{pod.Metadata.Name}/exec?command=ls&stdin=true&stdout=true&tty=false"), CancellationToken.None);
                    //*/
                //using (var webSocket = await client.WebSocketNamespacedPodExecAsync(pod.Metadata.Name, "default", new[] { "ls"}))
                using (var wsClient = await client.WebSocketNamespacedPodExecAsync(pod.Metadata.Name, "default", new[] { "ls" }, pod.Spec.Containers[0].Name, tty: false, webSocketSubProtol: "v4.channel.k8s.io"))
                {//*/
                    /*var wrapper = new WebSocketWrapper(wsClient, 4096);
                    var start = DateTime.Now;

                    string str = null;
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => "timeout");
                    try
                    {
                        while ((str = await Task.WhenAny(wrapper.RecvAsync(), timeoutTask).Unwrap()) != null)
                        {
                            list.Add(str);
                            if (timeoutTask.IsCompleted)
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    { 
                    }//*/
                    var demux = new StreamDemuxer(wsClient);
                    demux.Start();

                    var buff = new byte[4096];
                    var stream = demux.GetStream(1, 1);
                    var streamReader = new StreamReader(stream);
                    string str = streamReader.ReadToEnd();
                    return Ok(str);
                }
            }
            catch (Exception e)
            {

                return Ok(JsonConvert.SerializeObject(new { e, pod, list }));
            }

        }
    }
}
