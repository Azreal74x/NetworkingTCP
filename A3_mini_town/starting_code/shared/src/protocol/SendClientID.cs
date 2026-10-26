using shared;

namespace shared {
  public class SendClientID : ISerializable {

    public int id;

    public void Serialize(Packet pPacket) {
      pPacket.Write(id);
    }

    public void Deserialize(Packet pPacket) {
      id = pPacket.ReadInt();
    }
  }
}

