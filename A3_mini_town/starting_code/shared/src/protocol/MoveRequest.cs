namespace shared {
  public class MoveRequest : ISerializable {
    public float posX;
    public float posY;
    public float posZ;

    public void Serialize(Packet pPacket) {
      pPacket.Write(posX);
      pPacket.Write(posY);
      pPacket.Write(posZ);
    }

    public void Deserialize(Packet pPacket) {
      posX = pPacket.ReadFloat();
      posY = pPacket.ReadFloat();
      posZ = pPacket.ReadFloat();
    }
  }
}