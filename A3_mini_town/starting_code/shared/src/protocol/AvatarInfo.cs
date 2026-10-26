namespace shared {

  public class AvatarInfo : ISerializable {

    public int id;
    public int skin;
    public float posX;
    public float posY;
    public float posZ;

    public void Serialize(Packet pPacket) {
      pPacket.Write(id);
      pPacket.Write(skin);
      pPacket.Write(posX);
      pPacket.Write(posY);
      pPacket.Write(posZ);
    }

    public void Deserialize(Packet pPacket) {
      id = pPacket.ReadInt();
      skin = pPacket.ReadInt();
      posX = pPacket.ReadFloat();
      posY = pPacket.ReadFloat();
      posZ = pPacket.ReadFloat();
    }

  }
}
