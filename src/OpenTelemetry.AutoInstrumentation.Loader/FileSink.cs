//------------------------------------------------------------------------------
// <auto-generated />
// This comment is here to prevent StyleCop from analyzing a file originally from Serilog.
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace OpenTelemetry.AutoInstrumentation.Loader
{
    internal sealed class FileSink : IDisposable
    {
        readonly TextWriter _output;
        readonly FileStream _underlyingStream;
        readonly object _syncRoot = new object();

        public FileSink(string path, Encoding encoding = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Stream outputStream = _underlyingStream = System.IO.File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);

            _output = new StreamWriter(outputStream, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public void Info(string message, params object[] args)
        {
            _output.Write(message, args);
            FlushToDisk();
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _output.Dispose();
            }
        }

        private void FlushToDisk()
        {
            lock (_syncRoot)
            {
                _output.Flush();
                _underlyingStream.Flush(true);
            }
        }
    }
}
