using System.Diagnostics;
using System.Text.RegularExpressions;
using CX.Engine.Common.Tracing;

namespace CX.Engine.Common.Python;

public class PythonProcess
{
    private readonly PythonProcessOptions _options;
    private readonly SemaphoreSlim _semaphoreOut = new(1, 1);

    public PythonProcess(PythonProcessOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    public async Task<byte[]> StreamToByteArrayViaFilesAsync(string scriptPath, Stream stream, string inputExtension = null,
        string outputExtension = null, bool ignoreErrors = false)
    {
        return await CXTrace.Current.SpanFor(CXTrace.Section_RunPython, null)
            .ExecuteAsync(async span =>
            {
                // Define the path to the Python executable

                var tempFileIn = Path.GetTempFileName();

                if (inputExtension != null)
                    tempFileIn = Path.ChangeExtension(tempFileIn, inputExtension);

                var tempFileOut = Path.GetTempFileName();

                if (outputExtension != null)
                    tempFileOut = Path.ChangeExtension(tempFileOut, outputExtension);

                try
                {
                    await CreateFileFromStreamAsync(tempFileIn, stream);

                    // Create a new process start info
                    var start = new ProcessStartInfo
                    {
                        FileName = _options.PythonInterpreterPath, // Set the executable to run (Python)
                        Arguments = PyEscapeAndQuoteArgs(scriptPath, tempFileIn, tempFileOut),
                        UseShellExecute = false, // Do not use the OS shell to start the process
                        RedirectStandardOutput = true, // Redirect output so it can be read from C#
                        RedirectStandardError = true, // Redirect errors
                        CreateNoWindow = true, // Do not create a new window
                        WorkingDirectory = _options.WorkingDir
                    };

                    // Start the process with the info specified
                    using (var process = Process.Start(start))
                    {
                        if (process == null)
                            throw new InvalidOperationException((string)"Python interpreter process not found");

                        // Read the standard output of the process
                        //_ = await process.StandardOutput.ReadToEndAsync();  // Log contents
                        var errors = await process.StandardError.ReadToEndAsync();

                        // Wait for the process to finish
                        await process.WaitForExitAsync();

                        ignoreErrors &= File.Exists(tempFileOut);

                        // Throw if we encountered any errors
                        if (!string.IsNullOrEmpty(errors) && !ignoreErrors)
                            throw new InvalidOperationException(errors);
                    }

                    var content = await File.ReadAllBytesAsync(tempFileOut);
                    span.Output = new { ContentLength = content.Length };

                    return content;
                }
                finally
                {
                    TryDeleteFile(tempFileOut);
                    TryDeleteFile(tempFileIn);
                }
            });
    }


    public async Task<string> StreamToStringViaFilesAsync(string scriptPath, Stream stream, Action<string> parseStdOutOutput = null)
    {
        return await CXTrace.Current.SpanFor(CXTrace.Section_RunPython, null)
            .ExecuteAsync(async span =>
            {
                // Define the path to the Python executable

                var tempFileIn = Path.GetTempFileName();
                var tempFileOut = Path.GetTempFileName();
                try
                {
                    await CreateFileFromStreamAsync(tempFileIn, stream);

                    // Create a new process start info
                    var start = new ProcessStartInfo
                    {
                        FileName = _options.PythonInterpreterPath, // Set the executable to run (Python)
                        Arguments = PyEscapeAndQuoteArgs(scriptPath, tempFileIn, tempFileOut),
                        UseShellExecute = false, // Do not use the OS shell to start the process
                        RedirectStandardOutput = true, // Redirect output so it can be read from C#
                        RedirectStandardError = true, // Redirect errors
                        CreateNoWindow = true, // Do not create a new window
                        WorkingDirectory = _options.WorkingDir
                    };

                    // Start the process with the info specified
                    using (var process = Process.Start(start))
                    {
                        if (process == null)
                            throw new InvalidOperationException((string)"Python interpreter process not found");

                        parseStdOutOutput?.Invoke(await process.StandardOutput.ReadToEndAsync());
                        var errors = await process.StandardError.ReadToEndAsync();

                        // Wait for the process to finish
                        await process.WaitForExitAsync();

                        // Throw if we encountered any errors
                        if (!string.IsNullOrEmpty(errors))
                            throw new InvalidOperationException(errors);
                    }

                    var content = await File.ReadAllTextAsync(tempFileOut);
                    span.Output = new { Content = content.Preview(2 * 1024 * 1024) };

                    return content;
                }
                finally
                {
                    TryDeleteFile(tempFileOut);
                    TryDeleteFile(tempFileIn);
                }
            });
    }

    public Task StreamToFilesAsync(string scriptPath, Stream stream, Func<string, int, int, Task> processFile, params string[] moreArgs)
    {
        return CXTrace.Current.SpanFor(CXTrace.Section_RunPython, null)
            .ExecuteAsync(async span =>
            {
                using var _ = await _semaphoreOut.UseAsync();

                // Define the path to the Python executable
                var tempFileIn = Path.GetTempFileName();
                var outDirectory = Path.Combine(Path.GetTempPath(), "python_binary_out");
                //Empty the output directory
                if (Directory.Exists(outDirectory))
                {
                    Directory.Delete(outDirectory, true);
                    Directory.CreateDirectory(outDirectory);
                }

                try
                {
                    await CreateFileFromStreamAsync(tempFileIn, stream);

                    // Create a new process start info
                    var start = new ProcessStartInfo
                    {
                        FileName = _options.PythonInterpreterPath, // Set the executable to run (Python)
                        Arguments = PyEscapeAndQuoteArgs(new List<string> { scriptPath, tempFileIn, outDirectory }.Concat(moreArgs)
                            .ToArray()),
                        UseShellExecute = false, // Do not use the OS shell to start the process
                        RedirectStandardOutput = false, // Redirect output so it can be read from C#
                        RedirectStandardError = true, // Redirect errors
                        CreateNoWindow = true, // Do not create a new window
                        WorkingDirectory = _options.WorkingDir
                    };

                    // Start the process with the info specified
                    using (var process = Process.Start(start))
                    {
                        if (process == null)
                            throw new InvalidOperationException("Python interpreter process not found");

                        // Read the standard output of the process
                        //_ = await process.StandardOutput.ReadToEndAsync();  // Log contents
                        var errors = await process.StandardError.ReadToEndAsync();

                        // Wait for the process to finish
                        await process.WaitForExitAsync();

                        // Throw if we encountered any errors
                        if (!string.IsNullOrEmpty(errors))
                            throw new InvalidOperationException(errors);
                    }

                    var files = Directory.GetFiles(outDirectory, "*.*");
                    foreach (var file in files)
                    {
                        //Regex extract from page X.jpg
                        var pageNo = int.Parse(Regex.Match(file, @"page \d+.jpg").Value[5..^4]);

                        await processFile(file, pageNo, files.Length);
                    }
                }
                finally
                {
                    TryDeleteFile(tempFileIn);
                    if (Directory.Exists(outDirectory))
                        Directory.Delete(outDirectory, true);
                }
            });
    }
}