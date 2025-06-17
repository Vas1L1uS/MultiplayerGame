using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MineServer.Networking.Tcp
{
    public class TcpServerManager : IDisposable
    {
        private string _ip;
        private int _port;
        private ConcurrentDictionary<string, TcpClient> _clients;
        private TcpListener _server;
        private bool _isRun;
        private CancellationTokenSource _serverCts;
        private IOutput _output;

        public void Dispose()
        {
            if (!_isRun) return;

            _isRun = false;

            // Отменяем все операции
            _serverCts?.Cancel();
            _serverCts?.Dispose();
            _serverCts = null;

            // Останавливаем сервер
            try
            {
                _server?.Stop();
            }
            catch (Exception ex)
            {
                _output.NewLog($"[{nameof(TcpServerManager)}] Ошибка при остановке сервера: {ex.Message}");
            }

            // Закрываем все клиентские подключения
            foreach (var client in _clients.Values)
            {
                try
                {
                    client?.Dispose();
                }
                catch (Exception ex)
                {
                    _output.NewLog($"[{nameof(TcpServerManager)}] Ошибка при закрытии клиента: {ex.Message}");
                }
            }

            _clients?.Clear();
            _clients = null;

            _output.NewLog($"[{nameof(TcpServerManager)}] Сервер полностью остановлен и освобожден");
        }

        public void Init(IOutput output, string ip, int port)
        {
            _ip = ip;
            _port = port;
            _output = output;
            _clients = new ConcurrentDictionary<string, TcpClient>();
            _isRun = true;
            _serverCts = new CancellationTokenSource();
            _ = StartServer(_serverCts.Token);
        }

        public void SendResponseToAllClients(string data)
        {
            _ = BroadcastAllClients(data);
        }

        public void SendResponseToClient(string clientId, string data)
        {
            _ = BroadcastClient(clientId, data);
        }

        private async Task BroadcastAllClients(string data)
        {
            if (_clients == null || _clients.IsEmpty) return;

            byte[] byteData = Encoding.UTF8.GetBytes(data + "\n");
            List<Task> tasks = new List<Task>();

            foreach (string clientId in _clients.Keys)
            {
                tasks.Add(BroadcastClient(clientId, data));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                _output.NewLog($"[{nameof(TcpServerManager)}] Ошибка при массовой отправке: {e.Message}");
            }
        }

        private async Task BroadcastClient(string clientId, string data)
        {
            if (_clients == null || !_clients.TryGetValue(clientId, out TcpClient client))
            {
                return;
            }

            byte[] byteData = Encoding.UTF8.GetBytes(data + "\n");

            if (client?.Connected == true)
            {
                try
                {
                    await client.GetStream().WriteAsync(byteData, 0, byteData.Length);
                }
                catch (Exception e)
                {
                    _output.NewLog($"[{nameof(TcpServerManager)}] Ошибка отправки клиенту {clientId}: {e.Message}");
                    _clients.TryRemove(clientId, out _);
                    client?.Dispose();
                }
            }
        }

        private async Task StartServer(CancellationToken cancellationToken)
        {
            var ip = IPAddress.Parse(_ip);
            var port = _port;

            try
            {
                _server = new TcpListener(IPAddress.Any, port);
                _server.Start();

                _output.NewLog($"[{nameof(TcpServerManager)}] Сервер запущен. Ожидание подключений...");

                while (_isRun && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        TcpClient client = await _server.AcceptTcpClientAsync().WithCancellation(cancellationToken);
                        string clientId = Guid.NewGuid().ToString();
                        _output.NewLog($"[{nameof(TcpServerManager)}] Клиент подключен. ID: {clientId}");

                        if (_clients.TryAdd(clientId, client))
                        {
                            _output.AddNewClient(clientId);

                            _ = HandleClientAsync(clientId, client, cancellationToken)
                                .ContinueWith(t =>
                                {
                                    if (t.IsFaulted)
                                    {
                                        _output.NewLog($"[{nameof(TcpServerManager)}] Ошибка обработки клиента {clientId}: {t.Exception?.InnerException?.Message}");
                                    }
                                    client.Dispose();
                                    _clients.TryRemove(clientId, out _);
                                }, TaskContinuationOptions.ExecuteSynchronously);
                        }
                        else
                        {
                            client.Dispose();
                            _output.NewLog($"[{nameof(TcpServerManager)}] Конфликт ID клиента: {clientId}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _output.NewLog($"[{nameof(TcpServerManager)}] Сервер получил запрос на остановку");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _output.NewLog($"[{nameof(TcpServerManager)}] Ошибка при принятии подключения: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.NewLog($"[{nameof(TcpServerManager)}] Критическая ошибка сервера: {ex.Message}");
            }
            finally
            {
                Dispose(); // Гарантируем освобождение ресурсов
            }
        }

        private async Task HandleClientAsync(string clientId, TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    TcpMessageProcessor processor = new(
                        stream,
                        data => _output.ProcessRequest(clientId, data),
                        $"{nameof(TcpServerManager)}:{clientId}"
                    );

                    await processor.ProcessMessagesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _output.NewLog($"[{nameof(TcpServerManager)}] Ошибка обработки клиента {clientId}: {ex.Message}");
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client?.Dispose();
                _output.RemoveClient(clientId);
                _output.NewLog($"[{nameof(TcpServerManager)}] Клиент отключен. ID: {clientId}");
            }
        }

        public interface IOutput
        {
            void ProcessRequest(string clientId, string data);
            void AddNewClient(string clientId);
            void RemoveClient(string clientId);
            void NewLog(string message);
        }
    }

    public static class TaskExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            return await task;
        }
    }
}