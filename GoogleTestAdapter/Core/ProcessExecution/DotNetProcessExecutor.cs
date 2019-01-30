﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.ProcessExecution
{

    public class DotNetProcessExecutor : IProcessExecutor
    {
        private readonly bool _printTestOutput;
        private readonly ILogger _logger;
        
        private Process _process;

        public DotNetProcessExecutor(bool printTestOutput, ILogger logger)
        {
            _printTestOutput = printTestOutput;
            _logger = logger;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension,
            Action<string> reportOutputLine)
        {
            // output reading after https://stackoverflow.com/a/7608823/1276129
            var processStartInfo = new ProcessStartInfo(command, parameters)
            {
                StandardOutputEncoding = Encoding.Default,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            if (!string.IsNullOrEmpty(pathExtension))
                processStartInfo.EnvironmentVariables["PATH"] = Utils.GetExtendedPath(pathExtension);

            _process = new Process {StartInfo = processStartInfo};
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            using (_process)
            {
                void HandleEvent(string line, AutoResetEvent autoResetEvent)
                {
                    if (line == null)
                    {
                        autoResetEvent.Set();
                    }
                    else
                    {
                        reportOutputLine(line);
                        if (_printTestOutput)
                        {
                            _logger.LogInfo(line);
                        }
                    }
                }

                // ReSharper disable AccessToDisposedClosure
                _process.OutputDataReceived += (sender, e) => HandleEvent(e.Data, outputWaitHandle);
                _process.ErrorDataReceived += (sender, e) => HandleEvent(e.Data, errorWaitHandle);
                // ReSharper restore AccessToDisposedClosure

                if (_printTestOutput)
                {
                    _logger.LogInfo(
                        ">>>>>>>>>>>>>>> Output of command '" + command + " " + parameters + "'");
                }

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                if (_process.WaitForExit(int.MaxValue) &&
                    outputWaitHandle.WaitOne(int.MaxValue) &&
                    errorWaitHandle.WaitOne(int.MaxValue))
                {
                    if (_printTestOutput)
                    {
                        _logger.LogInfo("<<<<<<<<<<<<<<< End of Output");
                    }
                    return _process.ExitCode;
                }

                return int.MaxValue;
            }
        }

        public void Cancel()
        {
            if (_process != null)
            {
                ProcessUtils.KillProcess(_process.Id, _logger);
            }
        }

    }

}