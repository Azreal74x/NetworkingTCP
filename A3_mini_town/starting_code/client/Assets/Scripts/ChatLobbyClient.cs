using shared;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
public class ChatLobbyClient : MonoBehaviour {
  //reference to the helper class that hides all the pAvatarInfo management behind a blackbox
  private AvatarAreaManager _avatarAreaManager;
  //reference to the helper class that wraps the chat interface
  private PanelWrapper _panelWrapper;

  [SerializeField] private string _server = "localhost";
  [SerializeField] private int _port = 55555;

  private TcpClient _client;

  private int myID;

  private void Start() {
    connectToServer();

    //register for the important events
    _avatarAreaManager = FindFirstObjectByType<AvatarAreaManager>();
    _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

    _panelWrapper = FindFirstObjectByType<PanelWrapper>();
    _panelWrapper.OnChatTextEntered += onChatTextEntered;
  }

  private void connectToServer() {
    try {
      _client = new TcpClient();
      _client.Connect(_server, _port);
      JoinRequest newJoinRequest = new JoinRequest();
      SendObject(newJoinRequest);
      Debug.Log("Connected to server.");
    }
    catch (Exception e) {
      Debug.Log("Could not connect to server:");
      Debug.Log(e.Message);
    }
  }

  private void onAvatarAreaClicked(Vector3 pClickPosition) {
    try {
      //Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);
      MoveRequest moveRequest = new MoveRequest();
      moveRequest.posX = pClickPosition.x;
      moveRequest.posZ = pClickPosition.z;
      SendObject(moveRequest);
    }
    catch(Exception ex) {
      Console.WriteLine("Error sending moveRequest to server: " + ex.Message);
    }
  }

  private void onChatTextEntered(string pText) {
    try {
      _panelWrapper.ClearInput();

      Message newMessage = new Message();
      newMessage.message = pText;
      //Console.WriteLine("newMessage to send is " + newMessage.message);

      if (myID != 0) {
        newMessage.idSender = myID;
        //Console.WriteLine("myID is " + myID);
      }
      else {
        Console.WriteLine("myID is not set!");
      }

      if (!string.IsNullOrWhiteSpace(pText)) {
        SendObject(newMessage);
      }
      else {
        Console.WriteLine("Text is null or white space!");
      }
    }
    catch(Exception ex) {
      Console.WriteLine("Error sending message to server: " + ex.Message);
    }
  }

  private void SendObject(ISerializable pOutObject) {
    try {
      //Debug.Log("Sending:" + pOutObject);

      Packet outPacket = new Packet();
      outPacket.Write(pOutObject);

      StreamUtil.Write(_client.GetStream(), outPacket.GetBytes());
    }

    catch (Exception e) {
      //for quicker testing, we reconnect if something goes wrong.
      Debug.Log("Error sending object: " + e.Message);
      _client.Close();
      //connectToServer();
    }
  }

  // RECEIVING CODE

  private void Update() {
    ProcessData();
  }

  private void ProcessData() {
    try {
      if (_client.Available > 0) {
        try {
          byte[] inBytes = StreamUtil.Read(_client.GetStream());
          Packet inPacket = new Packet(inBytes);
          ISerializable inObject = inPacket.ReadObject();
          //Console.WriteLine("Received object is: " + inObject);

          if (inObject is AvatarList) { HandleAvatarList(inObject as AvatarList); }
          else if (inObject is RemoveAvatar) { HandleRemoveAvatar(inObject as RemoveAvatar); }
          else if (inObject is SendClientID) { HandleSendClientID(inObject as SendClientID); }
          else if (inObject is Message message) { HandleMessage(message); }
          if (inObject is MoveInfo moveInfo) { HandleMoveInfo(moveInfo); }
        }
        catch (Exception ex) {
          Console.WriteLine("Error processing data: " + ex.Message);
        }
      }
    }
    catch (Exception e) {
      //for quicker testing, we reconnect if something goes wrong.
      Debug.Log(e.Message);
      _client.Close();
      //connectToServer();
    }
  }

  private void HandleAvatar(AvatarInfo pAvatarInfo) {
    try {
      //Console.WriteLine("AvatarInfo id is " + pAvatarInfo.id);
      if (!_avatarAreaManager.HasAvatarView(pAvatarInfo.id)) {
        AvatarView avatarView = _avatarAreaManager.AddAvatarView(pAvatarInfo.id);
        avatarView.SetSkin(pAvatarInfo.skin);
        avatarView.transform.localPosition = new Vector3(pAvatarInfo.posX, pAvatarInfo.posY, pAvatarInfo.posZ);
        //Console.WriteLine("avatarView id, skin and position set");
      }
    }
    catch(Exception ex) {
      Console.WriteLine("Error with HandleAvatar: " + ex.Message);
    }
  }

  private void HandleAvatarList(AvatarList pAvatarList) {
    //Console.WriteLine("pAvatarList.avatars count is " + pAvatarList.avatars.Count);
    foreach (AvatarInfo pAvatar in pAvatarList.avatars) {
      try {
        HandleAvatar(pAvatar);
      }
      catch (Exception ex) {
        Console.WriteLine("Error with HandleAvatarList: " + ex.Message);
      }
    }
  }

  private void HandleRemoveAvatar(RemoveAvatar pRemoveAvatar) {
    try {
      //Console.WriteLine("RemoveAvatar id is " + pRemoveAvatar.id);
      if (_avatarAreaManager.HasAvatarView(pRemoveAvatar.id)) {
        _avatarAreaManager.RemoveAvatarView(pRemoveAvatar.id);
        Console.WriteLine("RemoveAvatar with id " + pRemoveAvatar.id + " removed from avatarView");
      }
    }
    catch (Exception ex) {
      Console.WriteLine("Error with HandleRemoveAvatar: " + ex.Message);
    }
  }

  private void HandleSendClientID(SendClientID pSendClientID) {
    if (pSendClientID != null) {
      myID = pSendClientID.id;
    }
    else {
      Console.WriteLine("pSendClientID is null"); 
    }
  }

  private void HandleMessage(Message pMessage) {
    try {
      AvatarView avatarView = _avatarAreaManager.GetAvatarView(pMessage.idSender);
      avatarView.Say(pMessage.message);
      //Console.WriteLine("Avatar with id " + myID + " received pMessage to say is " + pMessage.message);
    }
    catch (Exception ex) {
      Console.WriteLine("Error with HandleMessage: " + ex.Message);
    }
  }

  private void HandleMoveInfo(MoveInfo pMoveInfo) {
    try {
      AvatarView avatarView = _avatarAreaManager.GetAvatarView(pMoveInfo.id);
      Vector3 newPos = new Vector3(pMoveInfo.posX, 0, pMoveInfo.posZ);
      avatarView.Move(newPos);
      //Console.WriteLine("Avatar with id " + myID + " moves to newPos: " + newPos);
    }
    catch (Exception ex) {
      Console.WriteLine("Error with HandleMoveInfo: " + ex.Message);
    }
  }
}
