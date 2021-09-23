using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

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
        public IActionResult GetContainers(string podName = null)
        {
            try
            {
                var procs = new DirectoryInfo("/proc").GetDirectories().Where(d => int.TryParse(d.Name, out int pid) && pid != 1).ToList();
                return Ok(procs.Select(p => p.Name));
            }
            catch (Exception e)
            {
                return Ok(JsonConvert.SerializeObject(e));
            }
        }
    }
}
