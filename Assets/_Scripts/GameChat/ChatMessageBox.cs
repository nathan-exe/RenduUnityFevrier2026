
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace Chat
{
    
    
/// <summary>
/// Affiche des messages dans l'UI du jeu
/// </summary>
public class ChatMessageBox : MonoBehaviour
{
    private const byte maxMessageCount = 16;
    private int _currentMessageCount = 0;

    //temp
    //public List<string> testMessages;
    
    private static readonly Dictionary<ChatMessageType,string> colors = new ()
    {
        {ChatMessageType.System ,"#ffee50ff"},
        {ChatMessageType.Player ,"#70ccffff"},
    };
    
    /// <summary>
    /// Affiche un message dans le chat
    /// </summary>
    /// <param name="message"></param>
    /// <param name="playerName"></param>
    /// <param name="chatMessageType"></param>
    public void PrintMessage(string message, string playerName = "System",ChatMessageType chatMessageType = ChatMessageType.System)
    {
        _currentMessageCount++;
        
        GameObject textObject = new GameObject("messageText_" + (_currentMessageCount)); //todo : pool ou queue
        textObject.transform.parent = transform;
        transform.SetAsLastSibling();
        textObject.transform.localScale = Vector3.one;

        //setup text
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 15;
        text.lineSpacing = 17;
        
        //write text message
        text.text = "<color="+colors[chatMessageType]+"><b>" + playerName + " : </color></b>" + message;
        
        //destroy oldest message
        if (_currentMessageCount > maxMessageCount)
        {
            Destroy(transform.GetChild(0).gameObject);//todo : pool ou queue
        }
    }

    
    
    //temp
    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PrintMessage(testMessages.PickRandom());
        }
    }

    void Start()
    {
        foreach (string testMessage in testMessages)
        {
            PrintMessage(testMessage);
        }
    }*/
    
}

}