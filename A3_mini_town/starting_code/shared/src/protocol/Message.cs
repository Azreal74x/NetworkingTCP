namespace shared {
  public class Message : ISerializable {

    public int idSender;
    public string message;
    public void Serialize(Packet pPacket) {
      pPacket.Write(idSender);
      pPacket.Write(message);
    }

    public void Deserialize(Packet pPacket) {
      idSender = pPacket.ReadInt();
      message = pPacket.ReadString();
    }

  }
}