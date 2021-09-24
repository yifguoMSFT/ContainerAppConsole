using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public static class ProcessManager
    {
        public static async Task<string> Run(string cmd, string args)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(cmd)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                Arguments = args
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"error code {process.ExitCode}:{error}");
            }

            return output;
        }
    }
}
