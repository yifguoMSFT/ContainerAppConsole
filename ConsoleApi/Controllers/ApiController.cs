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

        [Route("pods")]
        public async Task<IActionResult> GetPods(bool local = false)
        {
            try
            {
                if (local)
                {
                    string result = await ProcessManager.RunAsync("nsenter", "--target 1 --pid --ipc --mount --uts --net -- crictl pods -o json");
                    var json = JsonConvert.DeserializeObject<JToken>(result);
                    var pods = json["items"].Where(t => t["metadata"]["namespace"].ToString() == "default").Select(t => new { name = t["metadata"]["name"], id = t["id"] });
                    return Ok(JsonConvert.SerializeObject(pods));
                }
                else
                {
                    var hostEntry = await Dns.GetHostEntryAsync("console-api-v2-headless");
                    var ips = hostEntry.AddressList.Select(addr => addr.ToString()).ToList();

                    var tasks = ips.Select(async ip =>
                    {
                        var resp = await httpClient.GetAsync($"http://{ip}:10080/api/pods?local=true");
                        var conent = await resp.Content.ReadAsStringAsync();
                        var json = JsonConvert.DeserializeObject<JToken>(conent);
                        foreach (var item in json)
                        {
                            item["ip"] = ip;
                        }
                        return json;
                    }).ToList();
                    await Task.WhenAll(tasks);
                    var result = tasks.SelectMany(t => t.Result).OrderBy(t => t["name"]);
                    return Ok(JsonConvert.SerializeObject(result));
                }
                
            }
            catch (Exception e)
            {
                return StatusCode(500, JsonConvert.SerializeObject(e));
            }
        }

        private class ContainerInfo
        {
            public string Name;
            public int Pid;
        }

        private async Task<ContainerInfo> GetContainerNameAsync(DirectoryInfo proc)
        {
            try
            {
                using (var environ = proc.GetFiles("environ")[0].OpenText())
                {
                    var envs = await environ.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(envs))
                    {
                        var envDict = envs.Split('\0').Where(s => !string.IsNullOrEmpty(s)).Select(env => env.Split('=')).ToDictionary(splitted => splitted[0], splitted => splitted[1]);
                        if (envDict.ContainsKey("CONTAINER_NAME"))
                        {
                            return new ContainerInfo { Pid = int.Parse(proc.Name), Name = envDict["CONTAINER_NAME"] };
                        }
                    }
                }
            }
            catch (Exception e)
            { 
            }
            return null;
        }
    }
}
