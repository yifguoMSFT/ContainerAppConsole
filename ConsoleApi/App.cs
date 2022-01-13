using k8s.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public class App
    {
        public string ApiVersion { get; set; }

        public string Kind { get; set; }

        public V1ObjectMeta Metadata { get; set; }

        public JToken Spec { get; set; }

        public JToken Status { get; set; }
    }
}
