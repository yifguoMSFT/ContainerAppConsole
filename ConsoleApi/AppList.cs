using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public class AppList
    {

        public string ApiVersion { get; set; }

        public string Kind { get; set; }

        public V1ObjectMeta Metadata { get; set; }

        public IEnumerable<App> Items { get; set; }
    }
}
