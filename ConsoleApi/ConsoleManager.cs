using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApi
{
    public class ConsoleManager : IDisposable
    {
        public bool Initialized = false;
        public bool Busy = false;
        public bool IsRunning => !process.HasExited;

        private Process process;

        private StringBuilder output;
        private StringBuilder error;
        private Action<string> outputReciever;
        private int? pid;
        private bool useNsEnter;

        public ConsoleManager(Action<string> outputReciever, int? pid = null, bool useNsEnter = false)
        {
            this.outputReciever = outputReciever;
            this.pid = pid;
            this.useNsEnter = useNsEnter;
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
            if (!process.HasExited)
            {
                SendAsync("exit");
            }
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
            string environ = "";
            if (pid != null)
            {
                environ = string.Join(" ", File.ReadAllText($"/proc/{pid}/environ").Split('\0').Where(s => !string.IsNullOrEmpty(s) && s.Contains("=")));
            }
            string fsRootCmd = useNsEnter ? $"env -i - {environ} nsenter --target {pid} --mount --pid --uts --ipc --net -- /bash-static" : $"env -i - {environ} chroot /proc/{pid}/root /bash-static";
            process.StartInfo = new ProcessStartInfo(pid == null ? "bash" : fsRootCmd.Split(' ', 2)[0])
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                Arguments = pid == null ? string.Empty : fsRootCmd.Split(' ', 2)[1]
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
