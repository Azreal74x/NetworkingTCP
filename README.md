# Thanks for checking out my TCP networking projects! 

I worked on two networking projects using C# and Unity, focusing on TCP client-server communication, handling multiple clients, and multiplayer synchronization.

# ChatBox

**A TCP chat system where multiple clients can communicate through a shared server.** <br>

**Client Management:** <br>
Used dictionaries to manage connected clients, unique usernames, and chat rooms. <br>

**Message Handling:** <br>
Implemented server-side message routing using TCP network streams. <br>

**Chat Commands:** <br>
Added commands for changing usernames, listing users, and sending private messages. <br>

**Chat Rooms:** <br>
Implemented separate chat rooms where messages are only shared between clients in the same room. <br>

https://github.com/user-attachments/assets/60ea6d41-70ec-47fc-bc9b-60a2beb6c8d4

# Mini Town

**A 3D multiplayer chat lobby in Unity where players can move around and communicate with each other.**

**Object Serialization:** <br>
Used ISerializable and packets to send different types of data between clients and the server. <br>

**Player Management:** <br>
Handled player IDs, spawn positions, skins, and disconnected clients on the server. <br>

**Player Synchronization:** <br>
Implemented server-side movement validation and synchronized player positions across clients. <br>

**Proximity Chat:** <br>
Added a whisper system that calculates the distance between players to determine who receives messages. <br>

**Multiplayer Communication:** <br>
Synchronized player avatars and chat messages between multiple Unity clients. <br>

https://github.com/user-attachments/assets/5ab33129-96e1-4ee5-876b-8b8d22622453
