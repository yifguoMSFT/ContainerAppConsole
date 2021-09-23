using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public class ConsoleManager:IDisposable
    {
        private Process process;

        private StringBuilder output;
        private StringBuilder error;
        public ConsoleManager(Action<string> outputReciever)
        {
            process = new Process();
            process.StartInfo = new ProcessStartInfo("bash")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                Arguments = string.Empty
            };

            output = new StringBuilder();
            error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputReciever.Invoke(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputReciever.Invoke(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public Task SendAsync(string msg)
        {
            return process.StandardInput.WriteLineAsync(msg);
        }

        public void Dispose()
        {
            process.Dispose();
        }

        public enum State
        {
            Ready,
            Busy
        }
    }
}
