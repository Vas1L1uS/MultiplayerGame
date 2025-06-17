using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    [SerializeField] private bool _isServer;
    [SerializeField] private bool _isInputEnabled;
    [SerializeField] private string _ip;
    [SerializeField] private int _port;
    [SerializeField] private Vector3 _zeroPosition;
    [SerializeField] private GameObject _otherPlayerPrefab;
    [SerializeField] private GameObject _playerPrefab;

    private MultiplayerGame.Server.Root.RootNode _serverRootNode;
    private MultiplayerGame.Client.Root.RootNode _clientRootNode;

    private void OnDestroy()
    {
        if (_isServer)
        {
            _serverRootNode.Dispose();
            _serverRootNode = null;

        }
        else
        {
            _clientRootNode.Dispose();
            _clientRootNode = null;
        }
    }

    private void Awake()
    {
        if (_isServer)
        {
            _serverRootNode = new();
            _serverRootNode.Init(_otherPlayerPrefab, _ip, _port);
        }
        else
        {
            _clientRootNode = new();
            _clientRootNode.Init(_zeroPosition, _isInputEnabled, _playerPrefab, _otherPlayerPrefab, _ip, _port);
        }
    }

    private void Update()
    {
        if (_isServer)
        {
            _serverRootNode.Tick(Time.deltaTime);
        }
        else
        {
            _clientRootNode.Tick(Time.deltaTime);
        }
    }
}