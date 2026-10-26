namespace shared {
  public class MoveInfo : ISerializable {
    public int id;
    public float posX;
    public float posY;
    public float posZ;

    public void Serialize(Packet pPacket) {
      pPacket.Write(id);
      pPacket.Write(posX);
      pPacket.Write(posY);
      pPacket.Write(posZ);
    }

    public void Deserialize(Packet pPacket) {
      id = pPacket.ReadInt();
      posX = pPacket.ReadFloat();
      posY = pPacket.ReadFloat();
      posZ = pPacket.ReadFloat();
    }
  }
}