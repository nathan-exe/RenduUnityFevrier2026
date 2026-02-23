
using System;
using System.Collections.Generic;
using Chat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using UnityEngine.Assertions;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;
using Random = UnityEngine.Random;

public class LowLevelNetworkManager : NetworkBehaviour
{
    
    
    NetworkDriver _driver;
    private NativeList<NetworkConnection> _connections;
    private NetworkConnection _connectionToServer;
    
    private bool _initialized = false;

    [SerializeField]
    private ushort _port = 7777;
    public static LowLevelNetworkManager Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        NetworkManager.Singleton.OnServerStarted+=
            InitialiseNetworkDriverAndConnections;
        NetworkManager.Singleton.OnClientStarted += () =>
        {
            print("Client started");
            InitialiseNetworkDriverAndConnections();
        };
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        //release resources
        if (_driver.IsCreated)
        {
            _driver.Dispose();
            _connections.Dispose();
        }
    }

    void Update()
    {
        if (!_initialized) return;
        
        _driver.ScheduleUpdate().Complete();
        
        CleanUpDeadConnectionsAndAcceptNewConnections();
        
        //fetch events
        for (int i = 0; i < _connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type eventType;
            while ((eventType = _driver.PopEventForConnection(_connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                //=handle events=

                //data event
                if (eventType == NetworkEvent.Type.Data)
                {
                    //peek stream reader
                    using NativeArray<Byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp,NativeArrayOptions.UninitializedMemory);
                    stream.ReadBytes(bytes);
                    stream = new DataStreamReader(bytes);

                    //MessageSender sender = (MessageSender)stream.ReadByte();
                    
                    //the server broadcasts the received message to every client
                    //if (NetworkManager.Singleton.IsServer && sender != MessageSender.Server)
                    if (NetworkManager.Singleton.IsServer)
                    {
                        //broadcast message back to other clients
                        foreach (NetworkConnection networkConnection in _connections)
                        {
                            Debug.Log("broadcasted message back to connection :"+networkConnection);
                            
                            //filter connection to server and dead connections and sender
                            if (!networkConnection.IsCreated || networkConnection == _connectionToServer)
                                continue;

                            
                            StatusCode statusCode = (StatusCode)_driver.BeginSend(NetworkPipeline.Null, networkConnection, out DataStreamWriter writer);
                            if (statusCode != StatusCode.Success) { Debug.LogError("   Couldn't send message. Status code : " + statusCode,this); continue; }
                            
                            writer.WriteBytes(bytes);
                            
                            _driver.EndSend(writer);
                        }
                    }
                    //======
                    
                    //handle the message locally
                    HandleMessage(stream);
                }
                //disconnection event
                else if (eventType == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from the server.");
                    _connections[i] = default;
                    break;
                }
            }
        }
    }
    
    private void InitialiseNetworkDriverAndConnections()
    {
        print("ahhh");
        if (_initialized) return;
        
        //instantiate network driver
#if UNITY_WEBGL && !UNITY_EDITOR
        m_Driver = NetworkDriver.Create(new WebSocketNetworkInterface());
#else
        _driver = NetworkDriver.Create(new UDPNetworkInterface());
#endif
        
        _connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        
        print("Identity : \n" +
              "is client : "+NetworkManager.Singleton.IsClient+"\n" +
              "is host : "+NetworkManager.Singleton.IsHost+"\n" +
              "is server : "+NetworkManager.Singleton.IsServer);
        
        if (NetworkManager.Singleton.IsServer) //host
        {
            StatusCode statusCode;
            if ((statusCode = (StatusCode)_driver.Bind(NetworkEndpoint.AnyIpv4.WithPort(_port))) != StatusCode.Success)
            {
                Debug.LogError("Failed to bind to port : "+_port + ". Status Code : "+statusCode.ToString());
                return;
            }
            
            //listen
            statusCode = (StatusCode)_driver.Listen();//todo : fetch error code
            print("driver listen status code : "+statusCode.ToString());
        }
        
        if (NetworkManager.Singleton.IsClient)//host et client
        {
            NetworkConnection connection = _driver.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(_port));
            _connectionToServer = connection;
            _connections.Add(connection);
            print("Connected to "+NetworkEndpoint.LoopbackIpv4.WithPort(_port)+" as client. connection is created : "+connection.IsCreated);
        }
        
        print("initialized");
        _initialized = true;
    }
    private void CleanUpDeadConnectionsAndAcceptNewConnections()
    {
        print("CleanUpDeadConnectionsAndAcceptNewConnections()");
        
        // Clean up dead connections.
        for (int i = 0; i < _connections.Length; i++)
        {
            if (!_connections[i].IsCreated)
            {
                _connections.RemoveAtSwapBack(i);
                i--;
            }
        }
        
        // Accept new connections.
        NetworkConnection c;
        while ((c = _driver.Accept()) != default)
        {
            _connections.Add(c);
            Debug.Log("Accepted a connection !!!!");
        }
    }
    
    /// <summary>
    /// Envoie un message au server. Le server redistribuera ce message à tous les clients après l'avoir reçu.
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="bytes"></param>
    public void BroadcastMessage(GameNetworkMessageType messageType, byte[] bytes)
    {
        if (!_initialized) return;
        
        Assert.IsTrue(_connectionToServer.IsCreated,"not connected to server.");
        StatusCode statusCode = (StatusCode)_driver.BeginSend(NetworkPipeline.Null, _connectionToServer, out DataStreamWriter writer);
        Assert.IsTrue(statusCode == StatusCode.Success,"Couldn't send message. Status code : " + statusCode);
        
        //header :
        //message type -> 1 byte
        //data length -> 4 bytes
        //writer.WriteByte((byte)(IsServer?MessageSender.Server:MessageSender.Client));
        writer.WriteByte((byte)messageType);
        writer.WriteUInt((uint)bytes.Length);
        //data -> n bytes
        writer.WriteBytes(bytes);
        
        _driver.EndSend(writer);
        print("   sentMessage : "+messageType);

    }
    
    private void HandleMessage(DataStreamReader stream)
    {
        //header
        GameNetworkMessageType messageType = (GameNetworkMessageType)stream.ReadByte();
        uint dataLength = stream.ReadUInt();
        
        //data
        switch (messageType)
        {
            case GameNetworkMessageType.Unknown :
                break;
            case GameNetworkMessageType.GameChatMessage :
                Chat.GameChat.Instance.ReceiveMessage(stream);
                break;
        }
    }

    public enum GameNetworkMessageType
    {
        Unknown,
        GameChatMessage
    }
    
    //notes : 
    //ip : identifiant d'un appareil
    //port : pour filtrer/identifier une application, attribuable de manière aléatoire. 80-> port web, port 22 -> SSH
    //IPV4 -> pas unique, identifiant local, combiné à l'IPV4 du routeur ?
    //IP différentes par interface : wifi, ethernet, 5g... pour pas que le routeur n'envoie trois fois un meme paquet au meme appareil
    //IPV6 -> successeur de l'IPV4, avec plus de possibilités. plusieurs IPV6 possibles par appareil, identifiant global.
    //routeur : 1 interface(IP) locale et publique
    //NAT / Network Adress Translation : le routeur transforme une IP locale en IP publique (entre autres) (couche réseau)
    //PAT / Port Adress Translation : pareil mais pour les Ports (couche transport)
    //bitmasking -> pour subdiviser un réseau local en différents sous réseaux (ne marche pas à l'échelle mondiale)
    //255 255 255 255 -> broadcast
    //MAN -> metropolitan area network
}
