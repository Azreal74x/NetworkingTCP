namespace shared {
  public class RemoveAvatar : ISerializable {

    public int id;

    public void Serialize(Packet pPacket) {
      pPacket.Write(id);
    }

    public void Deserialize(Packet pPacket) {
      id = pPacket.ReadInt();
    }

  }
}
