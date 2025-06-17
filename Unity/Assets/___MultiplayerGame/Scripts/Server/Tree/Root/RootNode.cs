using System;
using System.Collections.Generic;
using MineServer.Networking.Tcp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MultiplayerGame.Server.Root
{
    public class RootNode : IDisposable, TcpServerManager.IOutput
    {
        private TcpServerManager _tcpServerManager;
        private string _ip;
        private int _port;

        private Dictionary<string, SClient> _clients;

        private List<(string, string)> _requestList;

        private GameObject _otherPlayerPrefab;

        public void Dispose()
        {
            _requestList = null;
            _clients = null;
            _tcpServerManager.Dispose();
            _tcpServerManager = null;
        }

        public void Init(GameObject otherPlayerPrefab, string ip, int port)
        {
            _otherPlayerPrefab = otherPlayerPrefab;

            _ip = ip;
            _port = port;

            _requestList = new();
            _clients = new();
            _tcpServerManager = new();
            _tcpServerManager.Init(this, _ip, _port);
        }

        public void Tick(float deltaTime)
        {
            ProcessRequestList();

            foreach (SClient client in _clients.Values)
            {
                client.Tick();
            }

            List<ClientData> clients = new();

            foreach (SClient client in _clients.Values)
            {
                clients.Add(client.Data);
            }

            ResponseData responseData = new()
            {
                Type = "clients",
                Body = clients
            };

            string json = JsonConvert.SerializeObject(responseData);
            _tcpServerManager.SendResponseToAllClients(json);
        }

        private void ProcessRequestList()
        {
            for (byte i = 0; i < 16; i++)
            {
                if (_requestList.Count < 1) break;

                string clientId = _requestList[0].Item1;
                string data = _requestList[0].Item2;

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(data)) continue;

                JObject jObject = JObject.Parse(data);

                if (jObject.TryGetValue("Type", out JToken type))
                {
                    JToken body = jObject.GetValue("Body");

                    switch (type.Value<string>())
                    {
                        case "move":
                            Vec3 moveDirection = body.ToObject<Vec3>();

                            _clients[clientId].Obj.GetComponent<Rigidbody>().AddForce(new Vector3(moveDirection.X, moveDirection.Y, moveDirection.Z).normalized * 500f * Time.deltaTime);
                            break;

                        default:
                            break;
                    }
                }

                _requestList.RemoveAt(0);
            }
        }

        #region TcpClientManager Interface
        public void ProcessRequest(string clientId, string data)
        {
            _requestList.Add((clientId, data));
        }

        public void AddNewClient(string clientId)
        {
            SClient client = new(clientId, new(), GameObject.Instantiate(_otherPlayerPrefab, new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f)), _otherPlayerPrefab.transform.rotation));
            _clients.Add(clientId, client);

            ResponseData responseData = new()
            {
                Type = "start",
                Body = clientId
            };

            string json = JsonConvert.SerializeObject(responseData);
            _tcpServerManager.SendResponseToClient(clientId, json);
        }

        public void RemoveClient(string clientId)
        {
            throw new NotImplementedException();
        }

        public void NewLog(string message)
        {
            Debug.Log(message);
        }
        #endregion
    }
}