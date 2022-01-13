using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SimpleServer.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        [Route("ok")]
        [HttpGet]
        public IActionResult Ok()
        {
            return Ok(Environment.MachineName);
        }

        [Route("get")]
        [HttpGet]
        public async Task<IActionResult> Get(string url)
        {
            var client = new HttpClient();
            try
            {
                var result = await client.GetAsync(url);
                return Ok("ok");
            }
            catch (Exception e)
            {
                return Ok(JsonConvert.SerializeObject(e));
            }
        }

        [Route("nameresolver")]
        [HttpGet]
        public async Task<IActionResult> Nameresolver(string hostname, bool includeIpV6 = false)
        {
            NameResolverResult result = null;
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(hostname);
                var ips = hostEntry.AddressList.Where(addr => includeIpV6 || addr.AddressFamily != AddressFamily.InterNetworkV6).Select(addr => addr.ToString()).Distinct().ToList();
                result = new NameResolverResult { Status = "success", IpAddresses = ips };

            }
            catch (Exception e)
            {
                if (e.Message == "No such host is known")
                {
                    result = new NameResolverResult { Status = "host not found" };
                }
                else
                {
                    result = new NameResolverResult { Status = "unknown error", Exception = e };
                }
            }

            return Ok(JsonConvert.SerializeObject(result));
        }

        private class NameResolverResult
        {
            public string Status;
            public List<string> IpAddresses;
            public Exception Exception;
        }
    }

   
}
