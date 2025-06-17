using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Vas1L1uS_Packs.Networking.Tcp;

namespace MultiplayerGame.Client.Root
{
    public class RootNode : IDisposable, TcpClientManager.IOutput
    {
        private TcpClientManager _tcpClientManager;
        private string _ip;
        private int _port;
        private List<string> _responseList;

        private string _clientId;
        private GameObject _player;
        private Dictionary<string, GameObject> _otherPlayers;

        private GameObject _playerPrefab;
        private GameObject _otherPlayerPrefab;

        private Vector3 _zeroWorldPosition;
        private bool _isInputEnabled;

        public void Dispose()
        {
            _otherPlayers = null;
            _responseList = null;
            _tcpClientManager.Dispose();
            _tcpClientManager = null;
        }

        public void Init(Vector3 zeroWorldPosition, bool isInputEnabled, GameObject playerPrefab, GameObject otherPlayerPrefab, string ip, int port)
        {
            _isInputEnabled = isInputEnabled;
            _zeroWorldPosition = zeroWorldPosition;

            _playerPrefab = playerPrefab;
            _otherPlayerPrefab = otherPlayerPrefab;

            _ip = ip;
            _port = port;

            _otherPlayers = new();
            _responseList = new();
            _tcpClientManager = new();
            _tcpClientManager.Init(this, _ip, _port);
        }

        public void Tick(float deltaTime)
        {
            ProcessResponseList();

            if (!_isInputEnabled) return;

            Vector3 moveDirection = new();

            if (Input.GetKey(KeyCode.W))
            {
                moveDirection += Vector3.forward;
            }

            if (Input.GetKey(KeyCode.S))
            {
                moveDirection += Vector3.back;
            }

            if (Input.GetKey(KeyCode.A))
            {
                moveDirection += Vector3.left;
            }

            if (Input.GetKey(KeyCode.D))
            {
                moveDirection += Vector3.right;
            }

            if (moveDirection == Vector3.zero) return;

            Vec3 dir = new()
            {
                X = moveDirection.x,
                Y = moveDirection.y,
                Z = moveDirection.z
            };

            RequestData requestData = new()
            {
                Type = "move",
                Body = dir
            };

            string json = JsonConvert.SerializeObject(requestData);
            _tcpClientManager.MakeRequest(json);
        }

        #region TcpClientManager Interface
        public void NewLog(string message)
        {
            Debug.Log(message);
        }

        public void ProcessResponse(string data)
        {
            _responseList.Add(data);
        }
        #endregion

        private void ProcessResponseList()
        {
            for (byte i = 0; i < 16; i++)
            {
                if (_responseList.Count < 1) break;

                string data = _responseList[0];

                if (data == null) continue;

                JObject jObject = JObject.Parse(data);

                if (jObject.TryGetValue("Type", out JToken type))
                {
                    JToken body = jObject.GetValue("Body");

                    switch (type.Value<string>())
                    {
                        case "start":
                            _clientId = body.Value<string>();
                            _player = GameObject.Instantiate(_playerPrefab);
                            break;
                        case "clients":
                            List<ClientData> clients = body.ToObject<List<ClientData>>();

                            if (clients == null) break;

                            foreach (ClientData client in clients)
                            {
                                if (client.Id == _clientId)
                                {
                                    _player.transform.position = new Vector3(client.Position.X, client.Position.Y, client.Position.Z) + _zeroWorldPosition;
                                }
                                else
                                {
                                    if (_otherPlayers.ContainsKey(client.Id))
                                    {
                                        _otherPlayers[client.Id].transform.position = new Vector3(client.Position.X, client.Position.Y, client.Position.Z) + _zeroWorldPosition;
                                    }
                                    else
                                    {
                                        _otherPlayers.Add(client.Id, GameObject.Instantiate(_otherPlayerPrefab, new Vector3(client.Position.X, client.Position.Y, client.Position.Z) + _zeroWorldPosition, _playerPrefab.transform.rotation));
                                    }
                                }
                            }

                            break;
                        default:
                            break;
                    }
                }

                _responseList.RemoveAt(0);
            }
        }
    }
}