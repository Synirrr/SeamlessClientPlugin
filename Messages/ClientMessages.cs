using ProtoBuf;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SeamlessClientPlugin.SeamlessTransfer;
using SeamlessClientPlugin.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClientPlugin.ClientMessages
{
    public enum ClientMessageType
    {
        FirstJoin,
        TransferServer
    }


    [ProtoContract]
    public class ClientMessage
    {
        [ProtoMember(1)]
        public ClientMessageType MessageType;
        [ProtoMember(2)]
        public byte[] MessageData;
        [ProtoMember(3)]
        public long IdentityID;
        [ProtoMember(4)]
        public ulong SteamID;
        [ProtoMember(5)]
        public string PluginVersion = "0";

        public ClientMessage(ClientMessageType Type)
        {
            MessageType = Type;

            if (MyAPIGateway.Multiplayer != null && !MyAPIGateway.Multiplayer.IsServer)
            {
                if (MyAPIGateway.Session.LocalHumanPlayer == null)
                {
                    return;
                }

                IdentityID = MySession.Static?.LocalHumanPlayer?.Identity?.IdentityId ?? 0;
                SteamID = MySession.Static?.LocalHumanPlayer?.Id.SteamId ?? 0;
                PluginVersion = SeamlessClient.Version;
            }
        }

        public ClientMessage() { }

        public void SerializeData<T>(T Data)
        {
            MessageData = Utility.Serialize(Data);
        }


        public Transfer GetTransferData()
        {
            if (MessageData == null)
                return default(Transfer);

            return Utility.Deserialize<Transfer>(MessageData);

        }

    }
}
