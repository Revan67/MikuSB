using MikuSB.GameServer.Game.Player;
using MikuSB.TcpSharp;
using MikuSB.Proto;
using MikuSB.Util;
using MikuSB.Util.Extensions;

namespace MikuSB.GameServer.Server.Packet.Send.Login;

public class PacketRspLogin : BasePacket
{
    private static readonly Logger Logger = new("RspLogin");

    public PacketRspLogin(PlayerInstance player) : base(CmdIds.RspLogin)
    {
        var proto = new RspLogin
        {
            Timestamp = (uint)Extensions.GetUnixSec(),
            WorldChannel = 1,
            AreaId = 1,
            Data = player.ToPlayerProto(),
            NeedRename = false
        };

        var bytes = Google.Protobuf.MessageExtensions.ToByteArray(proto);
        Logger.Info($"RspLogin proto size: {bytes.Length} bytes (limit: 65535)");

        SetData(bytes);
    }
}
