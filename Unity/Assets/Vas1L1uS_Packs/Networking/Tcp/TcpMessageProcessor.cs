using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MineServer.Networking.Tcp
{
    public class TcpMessageProcessor
    {
        private readonly byte[] _delimiter = Encoding.UTF8.GetBytes("\n");
        private readonly List<byte> _buffer = new List<byte>();
        private readonly NetworkStream _stream;
        private readonly Action<string> _messageHandler;
        private readonly string _contextName;

        public TcpMessageProcessor(NetworkStream stream, Action<string> messageHandler, string contextName)
        {
            _stream = stream;
            _messageHandler = messageHandler;
            _contextName = contextName;
        }

        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            var readBuffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                    if (bytesRead == 0)
                    {
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }

                    _buffer.AddRange(readBuffer.Take(bytesRead));

                    ProcessBuffer();
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException)
            {
                Console.WriteLine($"[{_contextName}] Connection interrupted: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_contextName}] Message processing error: {ex}");
            }
        }

        private void ProcessBuffer()
        {
            while (true)
            {
                int delimiterIndex = FindDelimiter(_buffer, _delimiter);
                if (delimiterIndex == -1) break;

                byte[] messageBytes = _buffer.Take(delimiterIndex).ToArray();
                _buffer.RemoveRange(0, delimiterIndex + _delimiter.Length);

                string message = Encoding.UTF8.GetString(messageBytes);
                _messageHandler(message);
            }
        }

        private static int FindDelimiter(List<byte> data, byte[] delimiter)
        {
            for (int i = 0; i <= data.Count - delimiter.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < delimiter.Length; j++)
                {
                    if (data[i + j] != delimiter[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }
}