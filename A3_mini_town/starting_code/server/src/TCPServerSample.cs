using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Numerics;
using System.Reflection;

/**
 * This class implements a simple tcp echo server.
 * Read carefully through the comments below.
 * Note that the server does not contain any sort of error handling.
 */
class TCPServerSample {
  public static void Main(string[] args) {
    TCPServerSample server = new TCPServerSample();
    server.run();
  }

  private TcpListener _listener;
  private List<TcpClient> _clients = new List<TcpClient>();

  private List<TcpClient> disconnectedClients = new List<TcpClient>();
  private Dictionary<TcpClient, AvatarInfo> clientAvatars = new Dictionary<TcpClient, AvatarInfo>();

  private int idCount = 0;

  private void run() {
    Console.WriteLine("Server started on port 55555");

    _listener = new TcpListener(IPAddress.Any, 55555);
    _listener.Start();

    while (true) {
      processNewClients();
      processExistingClients();
      RemoveClients();

      //Although technically not required, now that we are no longer blocking, 
      //it is good to cut your CPU some slack
      Thread.Sleep(100);
    }
  }

  private void processNewClients() {
    while (_listener.Pending()) {
      try {
        _clients.Add(_listener.AcceptTcpClient());
        Console.WriteLine("Accepted new client.");
      }
      catch (Exception ex) {
        Console.WriteLine("error processing new clients " + ex.Message);
      }
    }
  }

  private void processExistingClients() {
    foreach (TcpClient client in _clients) {
      try {
        if (client.Available == 0) continue;

        byte[] inBytes = StreamUtil.Read(client.GetStream());
        Packet inPacket = new Packet(inBytes);
        ISerializable inObject = inPacket.ReadObject();
        //Console.WriteLine("Received object: " + inObject);

        if (inObject is JoinRequest joinRequest) { HandleJoinRequest(client, joinRequest); }
        else if (inObject is Message message) {
          HandleMessage(client, message);
        }
        else if (inObject is MoveRequest moveRequest) {
          HandleMoveRequest(client, moveRequest);
        }
      }
      catch (Exception ex) {
        Console.WriteLine("Error processing existing clients: " + ex.Message);
      }
    }
  }

  private void HandleJoinRequest(TcpClient pClient, JoinRequest pJoinRequest) {
    try {
      AvatarInfo newAvatar = new AvatarInfo();

      idCount += 1;
      newAvatar.id = idCount; //change to random later?
      Console.WriteLine("newAvatar id: " + newAvatar.id);

      SendClientID sendClientID = new SendClientID();
      sendClientID.id = idCount;
      sendObject(pClient, sendClientID);

      Random randomSkin = new Random();
      newAvatar.skin = (int)(randomSkin.Next(0, 1000));
      //Console.WriteLine("newAvatar skin: " + newAvatar.skin);

      Random randomPosition = new Random();
      Vector3 newAvatarPosition = new Vector3(randomPosition.Next(0, 10), 0.0f, randomPosition.Next(0, 10));
      newAvatar.posX = newAvatarPosition.X;
      newAvatar.posZ = newAvatarPosition.Z;
      //Console.WriteLine("newAvatar position: " + newAvatarPosition);

      clientAvatars.Add(pClient, newAvatar);
      //Console.WriteLine("newAvatar added to clientAvatars with id " + newAvatar.id);
    }
    catch (Exception ex) {
      Console.WriteLine("Error initializing AvatarInfo: " + ex.Message);
    }

    try {
      AvatarList avatarList = new AvatarList();
      foreach (AvatarInfo avatar in clientAvatars.Values) {
        avatarList.avatars.Add(avatar);
      }
      //Console.WriteLine("avatarList count: " + avatarList.avatars.Count);

      foreach (TcpClient otherClient in clientAvatars.Keys) {
        if (otherClient.Connected) {
          sendObject(otherClient, avatarList);
        }
      }
    }
    catch (Exception ex) {
      Console.WriteLine("Error sending AvatarList to client: " + ex.Message);
      disconnectedClients.Add(pClient);
    }
  }

  private void HandleMessage(TcpClient pClient, Message pMessage) {
    if (clientAvatars.ContainsKey(pClient)) {

      AvatarInfo sender = clientAvatars[pClient];
      pMessage.idSender = sender.id;

      //Console.WriteLine("pMessage is: " + pMessage.message);
      bool isWhisper = pMessage.message.StartsWith("/whisper");

      //Console.WriteLine("pMessage ID is " + pMessage.idSender + " and pClient ID is " + clientAvatars[pClient].id);
      if (!isWhisper) {
        //Console.WriteLine("message is not whisper command");
        foreach (TcpClient client in _clients) {
          try {
            sendObject(client, pMessage);
          }
          catch (Exception ex) {
            Console.WriteLine("Error sending message to client: " + ex.Message);
            disconnectedClients.Add(client);
          }
        }
      }
      else {
        //Console.WriteLine("message is a whisper command");
        string whisperText = pMessage.message.Substring("/whisper".Length).Trim();
        //Console.WriteLine("whisperText: " + whisperText);
        pMessage.message = whisperText;

        foreach (TcpClient client in _clients) {
          try {
            if (client.Connected && clientAvatars.ContainsKey(client)) {
              if (client == pClient) {
                //Console.WriteLine("client is equal to pClient");
                sendObject(client, pMessage);
              }
            }
          }
          catch (Exception ex) {
            Console.WriteLine("Couldn't echo whisper message back to sender client: " + ex.Message);
            disconnectedClients.Add(client);
          }

          try {
            AvatarInfo receiver = clientAvatars[client];

            float distanceX = sender.posX - receiver.posX;
            float distanceZ = sender.posZ - receiver.posZ;
            float distance = MathF.Sqrt(distanceX * distanceX + distanceZ * distanceZ);
            //Console.WriteLine("distance  between clients is " + distance);

            if (distance <= 2.0f) {
              //Console.WriteLine("client found in range");
              sendObject(client, pMessage);
            }
          }
          catch (Exception ex) {
            Console.WriteLine("Couldn't send whisper message to nearby client: " + ex.Message);
            disconnectedClients.Add(client);
          }
        }
      }
    }
  }

  private void HandleMoveRequest(TcpClient pClient, MoveRequest pMoveRequest) {
    float spawnRange = 20f;
    float distance = MathF.Sqrt(pMoveRequest.posX * pMoveRequest.posX + pMoveRequest.posZ * pMoveRequest.posZ);
    //Console.WriteLine("Move request distance: " + distance);

    if (distance <= spawnRange) {
      AvatarInfo avatarInfo = clientAvatars[pClient];
      avatarInfo.posX = pMoveRequest.posX;
      avatarInfo.posZ = pMoveRequest.posZ;
      Vector3 newAvatarPosition = new Vector3(avatarInfo.posX, avatarInfo.posY, avatarInfo.posZ);
      //Console.WriteLine("newAvatarInfo positionX is " + newAvatarPosition);

      MoveInfo moveInfo = new MoveInfo();
      moveInfo.id = avatarInfo.id;
      moveInfo.posX = pMoveRequest.posX;
      moveInfo.posZ = pMoveRequest.posZ;
      Vector3 newMoveInfo = new Vector3(moveInfo.posX, moveInfo.posY, moveInfo.posZ);
      //Console.WriteLine("moveInfo id is "+ moveInfo.id + " and position is " + newMoveInfo);

      foreach (TcpClient client in _clients) {
        try {
          if (client.Connected)
            sendObject(client, moveInfo);
        }
        catch (Exception ex) {
          Console.WriteLine("Error sending movement request to client: " + ex.Message);
          disconnectedClients.Add(client);
        }
      }
    }
    else {
      //Console.WriteLine("Move request distance too big");
    }
  }

  private void sendObject(TcpClient pClient, ISerializable pOutObject) {
    if (!pClient.Connected) return;

    try {
      //Console.WriteLine("Sending: " + pOutObject);
      Packet outPacket = new Packet();
      outPacket.Write(pOutObject);
      StreamUtil.Write(pClient.GetStream(), outPacket.GetBytes());
    }
    catch (Exception ex) {
      Console.WriteLine("Error sending object: " + ex.Message);
      disconnectedClients.Add(pClient);
    }
  }

  private void RemoveClients() {
    foreach (TcpClient client in disconnectedClients) {
      try {
        RemoveAvatar removeAvatar = new RemoveAvatar();
        removeAvatar.id = clientAvatars[client].id;
        Console.WriteLine("Removing disconnected client with id " + removeAvatar.id);
        foreach (TcpClient otherClient in clientAvatars.Keys) {
          if (otherClient.Connected) {
            sendObject(otherClient, removeAvatar);
          }
        }
        _clients.Remove(client);
        clientAvatars.Remove(client);
      }
      catch (Exception ex) {
        Console.WriteLine("Error removing client: " + ex.Message);
      }
    }
    disconnectedClients.Clear();
  }

}

