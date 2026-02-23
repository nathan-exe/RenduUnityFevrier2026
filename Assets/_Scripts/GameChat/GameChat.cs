using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace Chat
{
    /// <summary>
    /// reçoit et envoit les messages du chat
    /// </summary>
    public class GameChat : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private ChatMessageBox _messageBox;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private CanvasGroup _backGroundCanvasGroup;
        
        //singleton
        public static GameChat Instance { get; private set; }

        private bool _justSubmittedText = false;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            Instance = this; //singleton
            _inputField.onSubmit.AddListener(OnSubmit);
            _inputField.onEndEdit.AddListener((_) =>
            {
                print("End Edit");
                if (_justSubmittedText)
                {
                    _inputField.ActivateInputField();
                    _justSubmittedText = false;
                }
            });
        }

        private void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback+=(ulong clientID)=>
            {
                if(NetworkManager.Singleton.IsServer && clientID!=NetworkManager.Singleton.LocalClientId)
                    BroadcastChatMessage("Client connected : " + clientID);
            };
            
            Hide();
        }

        public void Show()
        {
            _canvasGroup.alpha = 1;
            _backGroundCanvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _inputField.interactable = true;
            _inputField.ActivateInputField();
            EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
            _justSubmittedText = false;
        }

        public void Hide()
        {
            _canvasGroup.alpha = .5f;
            _backGroundCanvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _justSubmittedText = false;
            _inputField.interactable = false;
            _inputField.DeactivateInputField();
        }
        
        
        //appelé quand le joueur appuie sur entrer.
        void OnSubmit(string message)
        {
            print("Submit");
            if (_inputField.text != "")
            {
                //todo : récupérer le nom du joueur à partir d'un systeme d'identification
                BroadcastChatMessage(message, "TestPlayer_"+NetworkManager.Singleton.LocalClient.ClientId, ChatMessageType.Player);
                _inputField.text = "";
                _justSubmittedText = true;
            }
            EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
        }
        
        /// <summary>
        /// Envoie un message aux autres joueurs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="playerName"></param>
        /// <param name="chatMessageType"></param>
        void BroadcastChatMessage(string message,string playerName = "System",ChatMessageType chatMessageType = ChatMessageType.System)
        {
            byte[] bytes = SerializePacketDataToByteArray(new(
                message,
                playerName,
                chatMessageType));
            
            LowLevelNetworkManager.Instance.BroadcastMessage(
                LowLevelNetworkManager.GameNetworkMessageType.GameChatMessage,
                bytes);
        }
        
        /// <summary>
        /// Appelé quand un message a été reçu
        /// </summary>
        /// <param name="data"></param>
        public void ReceiveMessage(DataStreamReader data)
        {
            Packet decodedPacket = DeserializePacketDataFromByteArray(data);
            _messageBox.PrintMessage(
                decodedPacket.message,
                decodedPacket.playerName,
                decodedPacket.chatMessageType);
        }

        private struct Packet
        {
            public string message;
            public string playerName;
            public ChatMessageType chatMessageType;

            public Packet(string message, string playerName, ChatMessageType chatMessageType)
            {
                this.message = message;
                this.playerName = playerName;
                this.chatMessageType = chatMessageType;
            }
        }
        
#region Serialization
        
        byte[] SerializePacketDataToByteArray(Packet packet)
        {
            //byte structure :
            //messageType - 1 byte
            //message length in bytes - 4 bytes
            //player name length in bytes - 4 bytes
            //message string data - n bytes
            //player name string data - n' bytes

            byte[] messageData = Encoding.UTF8.GetBytes(packet.message);
            byte[] playerNameData = Encoding.UTF8.GetBytes(packet.playerName);
            
            IEnumerable<byte> bytes = Array.Empty<byte>()
               
                //header
                .Append((byte)packet.chatMessageType)
                .Concat(BitConverter.GetBytes(messageData.Length))
                .Concat(BitConverter.GetBytes(playerNameData.Length))
                //data
                .Concat(messageData)
                .Concat(playerNameData);

            return bytes.ToArray();
        }
        
        Packet DeserializePacketDataFromByteArray(DataStreamReader data)
        {
            //byte structure :
            //messageType - 1 byte
            //message length in bytes - 4 bytes
            //player name length in bytes - 4 bytes
            //message string data - n bytes
            //player name string data - n' bytes
            
            Packet output = new();
            
            //header
            output.chatMessageType = (ChatMessageType)data.ReadByte();
            uint messageLength = data.ReadUInt();
            uint nameLength = data.ReadUInt();
            
            //data
            byte[] buffer = new byte[messageLength];
            data.ReadBytes(buffer);
            output.message = Encoding.UTF8.GetString(buffer);
            
            buffer = new byte[nameLength];
            data.ReadBytes(buffer);
            output.playerName = Encoding.UTF8.GetString(buffer);
            
            return output;
        }
        
        Packet DeserializePacketDataFromByteArray(NativeArray<Byte> data)
        {
            return DeserializePacketDataFromByteArray(new DataStreamReader(data));
        }
#endregion

    }
    
    public enum ChatMessageType
    {
        System,
        Player
    }
}