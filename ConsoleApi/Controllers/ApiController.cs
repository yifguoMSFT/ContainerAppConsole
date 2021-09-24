using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public async Task<IActionResult> GetContainers(string podName = null)
        {
            try
            {
                var procs = new DirectoryInfo("/proc").GetDirectories().Where(d => int.TryParse(d.Name, out int pid) && pid != 1).ToList();
                var containers = new List<ContainerInfo>();
                foreach (var proc in procs)
                {
                    var result = await GetContainerNameAsync(proc);
                    if (result != null)
                    {
                        containers.Add(result);
                    }
                }
                return Ok(JsonConvert.SerializeObject(containers));
            }
            catch (Exception e)
            {
                return Ok(JsonConvert.SerializeObject(e));
            }
        }

        [Route("pods")]
        public async Task<IActionResult> GetPods()
        {
            try
            {
                string result = await ProcessManager.RunAsync("nsenter", "--target 1 --pid --ipc --mount --uts --net -- crictl pods -o json");
                var json = JsonConvert.DeserializeObject<JToken>(result);
                var pods = json["items"].Select(t => new { name = t["metadata"]["name"], uid = t["metadata"]["uid"] });
                return Ok(JsonConvert.SerializeObject(pods));
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
