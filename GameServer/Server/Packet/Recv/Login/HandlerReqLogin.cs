using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Account;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.Packet.Send.Friend;
using MikuSB.GameServer.Server.Packet.Send.Login;
using MikuSB.Proto;
using MikuSB.TcpSharp;
using MikuSB.Util;

namespace MikuSB.GameServer.Server.Packet.Recv.Login;

[Opcode(CmdIds.ReqLogin)]
public class HandlerReqLogin : Handler
{
    public override async Task OnHandle(Connection connection, byte[] data, ushort seqNo)
    {
        var req = ReqLogin.Parser.ParseFrom(data);
        var account = AccountData.GetAccountByUid(1);
        if (account == null)
        {
            AccountData.CreateAccount("MIKU", 0, "");
            account = AccountData.GetAccountByUid(1);
            if (account == null)
            {
                await connection.SendPacket(CmdIds.NtfLogout);
                return;
            }
        }
        if (!ResourceManager.IsLoaded)
            // resource manager not loaded, return
            return;
        var prev = Listener.GetActiveConnection(account.Uid);
        if (prev != null)
        {
            await connection.SendPacket(CmdIds.NtfLogout);
            prev.Stop();
        }

        connection.State = SessionStateEnum.WAITING_FOR_LOGIN;
        var pd = DatabaseHelper.GetInstance<PlayerGameData>(account.Uid);
        connection.Player = pd == null ? new PlayerInstance(account.Uid) : new PlayerInstance(pd);

        connection.DebugFile = Path.Combine(ConfigManager.Config.Path.LogPath, "Debug/", $"{account.Uid}/",
            $"Debug-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.log");
        await connection.Player.OnEnterGame();
        connection.Player.Connection = connection;
        await connection.SendPacket(new PacketRspLogin(connection.Player!));

        var supplySync = new MikuSB.Proto.NtfSyncPlayer();
        foreach (var item in connection.Player.GetSupplyItems())
            supplySync.Items.Add(item.ToProto());
        if (supplySync.Items.Count > 0)
            await CallGSRouter.SendScript(connection, "", "{}", supplySync);

        await connection.Player.OnHeartBeat();
        await connection.SendPacket(new PacketNtfUpdateFriend(connection.Player!));
    }
}
