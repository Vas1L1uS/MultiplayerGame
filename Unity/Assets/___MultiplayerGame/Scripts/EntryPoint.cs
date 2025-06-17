using UnityEngine;
using UnityEngine.EventSystems;

public class EntryPoint : MonoBehaviour
{
    [SerializeField] private bool _isServer;
    [SerializeField] private string _ip;
    [SerializeField] private int _port;
    [Space]
    [SerializeField] private Camera _camera;
    [SerializeField] private bool _isInputEnabled;
    [SerializeField] private Vector3 _zeroPosition;
    [SerializeField] private GameObject _otherPlayerPrefab;
    [SerializeField] private GameObject _playerPrefab;
    [Space]
    [SerializeField] private CustomButton _moveUpButton;
    [SerializeField] private CustomButton _moveBackButton;
    [SerializeField] private CustomButton _moveLeftButton;
    [SerializeField] private CustomButton _moveRightButton;


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
            _clientRootNode.Init(_camera, _zeroPosition, _playerPrefab, _otherPlayerPrefab, _ip, _port);
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
            Vector3 moveDirection = Vector3.zero;

            if (_isInputEnabled)
            {
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
            }

            if (_moveUpButton.IsPressed)
            {
                moveDirection += Vector3.forward;
            }

            if (_moveBackButton.IsPressed)
            {
                moveDirection += Vector3.back;
            }

            if (_moveLeftButton.IsPressed)
            {
                moveDirection += Vector3.left;
            }

            if (_moveRightButton.IsPressed)
            {
                moveDirection += Vector3.right;
            }

            _clientRootNode.Tick(Time.deltaTime, moveDirection.normalized);
        }
    }
}