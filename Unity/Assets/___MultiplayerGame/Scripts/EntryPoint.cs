using System.Collections.Generic;
using MineServer.Networking.Tcp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Vas1L1uS_Packs.Networking.Tcp;

public class EntryPoint : MonoBehaviour, TcpServerManager.IOutput, TcpClientManager.IOutput
{

    [SerializeField] private GameObject _otherPlayerPrefab;
    [SerializeField] private GameObject _playerPrefab;

    [SerializeField] private bool _isServer;
    [SerializeField] private string _ip = "127.0.0.1";
    [SerializeField] private int _port = 1111;

    private TcpClientManager _tcpClientManager;
    private TcpServerManager _tcpServerManager;

    private Dictionary<string, Client> _clients;

    private List<string> _responseList;
    private List<(string, string)> _requestList;

    private string _clientId;
    private GameObject _player;
    private Dictionary<string, GameObject> _otherPlayers;

    private void OnDestroy()
    {
        if (_isServer)
        {
            _requestList = null;
            _clients = null;
            _tcpServerManager.Dispose();
            _tcpServerManager = null;

        }
        else
        {
            _otherPlayers = null;
            _responseList = null;
            _tcpClientManager.Dispose(); 
            _tcpClientManager = new();
        }
    }

    private void Awake()
    {
        if (_isServer)
        {
            _requestList = new();
            _clients = new();
            _tcpServerManager = new();
            _tcpServerManager.Init(this, _ip, _port);

        }
        else
        {
            _otherPlayers = new();
            _responseList = new();
            _tcpClientManager = new();
            _tcpClientManager.Init(this, _ip, _port);
        }
    }

    private void Update()
    {
        if (_isServer)
        {
            ProcessRequestList();

            foreach (Client client in _clients.Values)
            {
                client.Tick();
            }

            List<ClientData> clients = new();

            foreach (Client client in _clients.Values)
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
        else
        {
            ProcessResponseList();

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
    }

    private void ProcessResponseList()
    {
        for (int i = 0; i < _responseList.Count; i++)
        {
            string data = _responseList[i];

            if (data == null) continue;

            JObject jObject = JObject.Parse(data);

            if (jObject.TryGetValue("Type", out JToken type))
            {
                JToken body = jObject.GetValue("Body");

                switch (type.Value<string>())
                {
                    case "start":
                        _clientId = body.Value<string>();
                        _player = Instantiate(_playerPrefab);
                        break;
                    case "clients":
                        List<ClientData> clients = body.ToObject<List<ClientData>>();

                        //List<ClientData> clients = null;

                        if (clients == null) break;

                        foreach (ClientData client in clients)
                        {
                            if (client.Id == _clientId)
                            {
                                _player.transform.position = new(client.Position.X, client.Position.Y, client.Position.Z);
                            }
                            else
                            {
                                if (_otherPlayers.ContainsKey(client.Id))
                                {
                                    _otherPlayers[client.Id].transform.position = new(client.Position.X, client.Position.Y, client.Position.Z);
                                }
                                else
                                {
                                    _otherPlayers.Add(client.Id, Instantiate(_otherPlayerPrefab, new(client.Position.X, client.Position.Y, client.Position.Z), _playerPrefab.transform.rotation));
                                }
                            }
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        _responseList.Clear();
    }

    private void ProcessRequestList()
    {
        for (int i = 0; i < _requestList.Count; i++)
        {
            (string, string) data = _requestList[i];
            JObject jObject = JObject.Parse(data.Item2);

            if (jObject.TryGetValue("Type", out JToken type))
            {
                JToken body = jObject.GetValue("Body");

                switch (type.Value<string>())
                {
                    case "move":
                        Vec3 moveDirection = body.ToObject<Vec3>();

                        _clients[data.Item1].Obj.GetComponent<Rigidbody>().AddForce(new Vector3(moveDirection.X, moveDirection.Y, moveDirection.Z).normalized * 10f);
                        break;

                    default:
                        break;
                }
            }
     
        }

        _requestList.Clear();
    }

    void TcpServerManager.IOutput.NewLog(string message)
    {
        Debug.Log(message);
    }

    void TcpServerManager.IOutput.AddNewClient(string clientId)
    {
        Client client = new(clientId, new(), Instantiate(_otherPlayerPrefab));
        _clients.Add(clientId, client);

        ResponseData responseData = new()
        {
            Type = "start",
            Body = clientId
        };

        string json = JsonConvert.SerializeObject(responseData);
        _tcpServerManager.SendResponseToClient(clientId, json);
    }

    void TcpServerManager.IOutput.RemoveClient(string clientId)
    {
        throw new System.NotImplementedException();
    }

    void TcpServerManager.IOutput.ProcessClientRequest(string clientId, string data)
    {
        _requestList.Add((clientId, data));
    }

    void TcpClientManager.IOutput.NewLog(string message)
    {
        Debug.Log(message);
    }

    void TcpClientManager.IOutput.ProcessResponse(string data)
    {
        _responseList.Add(data);
    }
   
}