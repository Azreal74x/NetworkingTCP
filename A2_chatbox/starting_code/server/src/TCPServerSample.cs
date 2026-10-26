using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.ComponentModel.Design;

class TCPServerSample {
  /**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */

  //send message and its byte length with .Write 
  //(get byte length with .Read)

  public static void Main(string[] args) {
    TCPServerSample sample = new TCPServerSample();
    sample.RunServer();
  }

  TcpListener listener;

  List<TcpClient> clients = new List<TcpClient>();
  Dictionary<TcpClient, string> namedClients = new Dictionary<TcpClient, string>();
  List<TcpClient> clientsToRemove = new List<TcpClient>();
  List<string> rooms = new List<string>();
  Dictionary<TcpClient, string> clientRooms = new Dictionary<TcpClient, string>();

  int clientCount = 1;
  byte[] byteStream = new byte[0];
  bool isCommand = false;

  void RunServer() {
    Console.WriteLine("Server started on port 55555");
    listener = new TcpListener(IPAddress.Any, 55555);
    listener.Start();



    while (true) {

      CreateClients();
      ProcessMessages();
      RemoveClients();

      //Although technically not required, now that we are no longer blocking, 
      //it is good to cut your CPU some slack
      Thread.Sleep(100);
    }
  }


  void CreateClients() {
    try {
      while (listener.Pending()) { //check if there are clients

        //accepting clients
        TcpClient newClient = listener.AcceptTcpClient();
        clients.Add(newClient); //if there are waiting clients add them to list of unaccepted clients
        Console.WriteLine("accepted new client.");

        //naming clients
        string clientName = " ";
        foreach (TcpClient client in clients) { //unnamed client
          if (!namedClients.ContainsKey(client)) {
            clientName = "guest" + clientCount;
            namedClients.Add(client, clientName);
            clientCount += 1;
            //Console.WriteLine("client count is " + clientCount);
          }
          if (client == newClient) {
            SendServerMessages(client, true, "You joined the server as " + clientName);
          }

          //add new clients to general room
          if (!clientRooms.ContainsKey(client)) {
            clientRooms.Add(client, "general");
          }
          if (!rooms.Contains("general")) {
            rooms.Add("general");
          }
        }

        foreach (TcpClient existingClient in clients) {
          if (existingClient == newClient) continue;
          SendServerMessages(existingClient, true, clientName + " joined the server");
        }
      }
    }
    catch (Exception ex) {
      Console.WriteLine("error creating new clients: " + ex.Message);
    }
  }

  void ProcessMessages() {
    byteStream = new byte[0];
    string stringStream = " ";
    string namedStream = " ";

    foreach (TcpClient client in clients) {
      try {
        if (client.Available == 0) continue; //check for unaccepted clients in list
        NetworkStream stream = client.GetStream(); //get message from said client

        byte[] readStream = StreamUtil.Read(stream); //stream == message
        stringStream = Encoding.UTF8.GetString(readStream);

        string clientName = namedClients[client];
        Console.WriteLine("client name is " + clientName);

        namedStream = clientName + ": " + stringStream;
        Console.WriteLine("full message is " + namedStream);
      }
      catch (Exception ex) {
        Console.WriteLine("error processing messages: " + ex.Message);
      }

      try {
        //check for commands
        string[] wordsInMessage = stringStream.Split(' ');

        if (wordsInMessage.Length > 0) {
          if (wordsInMessage[0].ToLower() == "/setname" && !string.IsNullOrWhiteSpace(wordsInMessage[1])) {
            isCommand = true;
            CommandSetName(client, wordsInMessage);
          }
          else if (wordsInMessage[0].ToLower() == "/list") {
            isCommand = true;
            CommandList(client);
          }
          else if (wordsInMessage[0].ToLower() == "/help") {
            isCommand = true;
            CommandHelp(client, wordsInMessage);
          }
          else if (wordsInMessage[0].ToLower() == "/whisper" && !string.IsNullOrWhiteSpace(wordsInMessage[1])) {
            isCommand = true;
            CommandWhisper(client, wordsInMessage);
          }
          else if (wordsInMessage[0].ToLower() == "/join" && !string.IsNullOrWhiteSpace(wordsInMessage[1])) {
            isCommand = true;
            CommandJoin(client, wordsInMessage);
          }
          else if (wordsInMessage[0].ToLower() == "/listrooms") {
            isCommand = true;
            CommandListRooms(client);
          }
          else if (wordsInMessage[0].ToLower() == "/listroom") {
            isCommand = true;
            CommandListRoom(client);
          }
          else {
            isCommand = false;
          }
        }
      }
      catch (Exception ex) {
        Console.WriteLine("error with checking commands: " + (ex.Message));
      }

      if (clientRooms.ContainsKey(client)) {
        ShareMessages(client, namedStream);

        foreach (TcpClient otherClient in clients) {
          if (otherClient != null && clientRooms.ContainsKey(otherClient)) {
            if (otherClient != client && clientRooms[otherClient] == clientRooms[client]) {
              Console.WriteLine("currentClient " + client + " is in the same room as other client " + otherClient);
              ShareMessages(otherClient, namedStream);
            }
          }
          else Console.WriteLine("we're the only client in the room");
        }

      }
      else Console.WriteLine("client is not in the room");

    }
  }
  void ShareMessages(TcpClient pClient, string message) {
    try {
      if (message.Length > 0 && isCommand == false) {
        NetworkStream stream = pClient.GetStream();
        byteStream = Encoding.UTF8.GetBytes(message);
        StreamUtil.Write(stream, byteStream);
        Console.WriteLine(namedClients[pClient] + "sent a message to all clients in that room");
      }
    }
    catch (Exception ex) {
      clientsToRemove.Add(pClient);
      Console.WriteLine("error sharing messages: " + ex.Message);
    }
  }

  void CommandSetName(TcpClient pClient, string[] pWordsInMessage) {
    Console.WriteLine("/setname command used by " + namedClients[pClient]);
    string newName = pWordsInMessage[1].ToLower();
    if (!namedClients.ContainsValue(newName)) {
      Console.WriteLine(namedClients[pClient] + " changed their name to " + newName);
      SendServerMessages(pClient, true, "You changed your name to " + newName);
      SendServerMessages(pClient, false, namedClients[pClient] + "changed their name to " + newName);
      namedClients[pClient] = newName;
    }
    else {
      SendServerMessages(pClient, true, "command failed, name already in use");
    }
  }

  void CommandList(TcpClient pClient) {
    string nameList = "List:\n" + string.Join("\n", namedClients.Values);
    SendServerMessages(pClient, true, nameList);
  }

  void CommandHelp(TcpClient pClient, string[] pWordsInMessage) {
    string commandList = "Commands:\n/setname -> change your nickname\n/list -> see list of online people\n/whisper nickname -> send private message\n/listrooms -> see list of rooms\n/listroom -> see list of people in current room";
    SendServerMessages(pClient, true, commandList);
  }

  void CommandWhisper(TcpClient pClient, string[] pWordsInMessage) {
    string receiver = pWordsInMessage[1].ToLower();
    string message = "";

    foreach (string word in pWordsInMessage) {
      if (word != "/whisper" && word != receiver) {
        message += " " + word;
      }
    }

    if (namedClients.ContainsValue(receiver) && receiver != namedClients[pClient] && !string.IsNullOrWhiteSpace(message)) {
      foreach (var namedClient in namedClients) {
        if (namedClient.Value == receiver) {
          SendServerMessages(namedClient.Key, true, namedClients[pClient] + " whispers:" + message);
          SendServerMessages(pClient, true, "You wispered to " + receiver + ":" + message);
        }
      }
    }
    else if (receiver == namedClients[pClient]) {
      SendServerMessages(pClient, true, "You can't whisper to yourself");
    }
    else if (!string.IsNullOrWhiteSpace(message)) {
      SendServerMessages(pClient, true, "You can't send an empty message");
    }
    else {
      SendServerMessages(pClient, true, "Target " + receiver + " does not exist");
    }
  }

  void CommandJoin(TcpClient pClient, string[] pWordsInMessage) {
    string roomName = pWordsInMessage[1].ToLower();
    if (!clientRooms.ContainsKey(pClient)) {
      clientRooms.Add(pClient, roomName);
      Console.WriteLine("new client added to clientRooms dictionary: " + clientRooms[pClient]);
    }
    else {
      clientRooms[pClient] = roomName;
      //Console.WriteLine("client exists and got a new value in clientRooms dictionary: " + clientRooms);
      foreach (var clientRoom in clientRooms) {
        Console.WriteLine("clientRoom in clientRooms list " + clientRooms[pClient]);
      }
    }
    SendServerMessages(pClient, true, "You joined " + roomName);
    Console.WriteLine("/join used with the roomName " + roomName);

    if (!rooms.Contains(roomName)) {
      rooms.Add(roomName);
      Console.WriteLine(roomName + " created");
      foreach (string room in rooms) {
        Console.WriteLine("room in rooms list " + room);
      }
    }
    else {
      Console.WriteLine(roomName + " exists");
    }
  }

  void CommandListRooms(TcpClient pClient) {
    string roomList = "";
    foreach (string room in rooms) {
      roomList += room + "\n";
      //Console.WriteLine("roomList is " + roomList);
    }
    SendServerMessages(pClient, true, "Rooms list:\n" + roomList);
  }

  void CommandListRoom(TcpClient pClient) {
    string clientRoom = clientRooms[pClient];
    string clientsInRoomList = "";
    foreach (var clientInRoom in clientRooms) {
      if (clientInRoom.Value.Contains(clientRoom)) {
        string clientName = namedClients[clientInRoom.Key];
        clientsInRoomList += clientName + "\n";
      }
    }
    SendServerMessages(pClient, true, clientRoom + " list:\n" + clientsInRoomList);
  }

  void RemoveClients() {
    try {
      foreach (TcpClient client in clientsToRemove) {
        Console.WriteLine("removed " + namedClients[client]);
        SendServerMessages(client, false,"server couldn't reach " + namedClients[client] + ", they are disconnected from the server"); 
        clients.Remove(client);
        namedClients.Remove(client);
        clientRooms.Remove(client);
      }
      clientsToRemove.Clear();
    }
    catch (Exception ex) {
      Console.WriteLine("error removing clients: " + ex.Message);
    }
  }

  void SendServerMessages(TcpClient currentClient, bool clientOnly, string message) {
    try {
      if (clientOnly) {
        Console.WriteLine("sent server message to client only");
        NetworkStream stream = currentClient.GetStream();
        StreamUtil.Write(stream, Encoding.UTF8.GetBytes(message));
      }
      else {
        foreach (TcpClient client in clients) {
          if (client == currentClient) continue; //skip current client

          NetworkStream stream = client.GetStream();
          StreamUtil.Write(stream, Encoding.UTF8.GetBytes(message));
          Console.WriteLine("sent server message to all clients in this room except current client");
        }
      }
    }
    catch (Exception ex) {
      Console.WriteLine("error sending server messages: " + ex.Message);
    }
  }
}



