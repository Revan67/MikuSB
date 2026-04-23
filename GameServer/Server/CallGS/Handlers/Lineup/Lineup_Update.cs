namespace MikuSB.GameServer.Server.CallGS.Handlers.Lineup;

[CallGSApi("Lineup_Update")]
public class Lineup_Update : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        await CallGSRouter.SendScript(connection, "UpdateLineup", "{}", seqNo);
    }
}
