using System.Collections.Generic;

namespace shared {

  public class AvatarList : ISerializable {

    public List<AvatarInfo> avatars = new List<AvatarInfo>();

    public void Serialize(Packet pPacket) {
      pPacket.Write(avatars.Count);

      foreach (AvatarInfo avatar in avatars) {
        avatar.Serialize(pPacket);
      }
    }

    public void Deserialize(Packet pPacket) {
      int avatarCount = pPacket.ReadInt();
      avatars.Clear();

      for (int i = 0; i < avatarCount; i++) {
        AvatarInfo avatar = new AvatarInfo();
        avatar.Deserialize(pPacket);
        avatars.Add(avatar);
      }
    }
  }

}
