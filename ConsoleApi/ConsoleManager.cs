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
        public bool Initialized = false;
        public bool Busy = false;
        public bool IsRunning => !process.HasExited;

        private Process process;

        private StringBuilder output;
        private StringBuilder error;
        private Action<string> outputReciever;
        private int? pid;

        public ConsoleManager(Action<string> outputReciever, int? pid = null)
        {
            this.outputReciever = outputReciever;
            this.pid = pid;
            Reset();
        }

        public async Task SendAsync(string msg)
        {
            if (!process.HasExited)
            {
                await process.StandardInput.WriteLineAsync(msg);
            }
            else
            {
                outputReciever.Invoke("terminated!");
            }
        }

        public void Dispose()
        {
            process.Kill();
            process.Dispose();
        }

        public void Reset()
        {
            if (process != null)
            {
                Dispose();
            }
            process = new Process();
            process.StartInfo = new ProcessStartInfo(pid == null ? "bash": $"chroot /proc/{pid}/root /bash-static")
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
    }
}
