using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MineServer.Networking.Tcp;

namespace Vas1L1uS_Packs.Networking.Tcp
{
    public class TcpClientManager : IDisposable
    {
        private string _ip;
        private int _port;
        private TcpClient _client;
        private CancellationTokenSource _cts;
        private bool _isRun;
        private IOutput _output;

        public void Dispose()
        {
            DisconnectFromServer();
            _client?.Dispose();
            _isRun = false;
            _output = null;
        }

        public void Init(IOutput output, string ip, int port)
        {
            _ip = ip;
            _port = port;
            _output = output;
            _ = ConnectToServerAsync();
        }

        public void MakeRequest(string message)
        {
            _ = MakeRequestAsync(message);
        }

        private async Task ConnectToServerAsync()
        {
            // Отменяем предыдущее подключение, если было
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _client = new TcpClient();

            try
            {
                _output.NewLog($"[{nameof(TcpClientManager)}] Попытка подключения к серверу...");

                // Подключаемся с таймаутом 5 секунд
                Task connectTask = _client.ConnectAsync(_ip, _port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new OperationCanceledException($"[{nameof(TcpClientManager)}] Превышено время ожидания подключения");
                }

                await connectTask; // Проверяем возможные ошибки подключения

                _isRun = true;
                _output.NewLog($"[{nameof(TcpClientManager)}] Подключение установлено. Запуск получения данных...");

                // Запускаем фоновый прием данных
                _ = Task.Run(() => ReceiveResponseAsync(_cts.Token), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                _output.NewLog($"[{nameof(TcpClientManager)}] Подключение отменено");
                Cleanup();
            }
            catch (Exception e)
            {
                _output.NewLog($"[{nameof(TcpClientManager)}] Ошибка подключения: {e.Message}");
                Cleanup();
                throw; // Пробрасываем исключение выше, если нужно
            }
        }

        private void DisconnectFromServer()
        {
            try
            {
                _cts?.Cancel();
                _client?.Dispose();
                _output.NewLog("Отключение от сервера завершено");
            }
            catch (Exception ex)
            {
                _output.NewLog($"[{nameof(TcpClientManager)}] Ошибка при отключении: {ex.Message}");
            }
        }

        private async Task MakeRequestAsync(string message)
        {
            if (_client == null || !_client.Connected) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                NetworkStream stream = _client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                _output.NewLog($"[{nameof(TcpClientManager)}] Send error: {e.Message}");
                _isRun = false;
            }
        }

        private async Task ReceiveResponseAsync(CancellationToken cancellationToken)
        {
            try
            {
                var processor = new TcpMessageProcessor(
                    _client.GetStream(),
                    json => _output.ProcessResponse(json),
                    nameof(TcpClientManager)
                );

                await processor.ProcessMessagesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _output.NewLog($"[{nameof(TcpClientManager)}] Receive error: {ex.Message}");
                _isRun = false;
            }
        }

        private int FindDelimiter(List<byte> data, byte[] delimiter)
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

        private void Cleanup()
        {
            _isRun = false;
            _client?.Dispose();
            _cts?.Cancel();
        }

        public interface IOutput
        {
            void ProcessResponse(string data);
            void NewLog(string message);
        }
    }
}