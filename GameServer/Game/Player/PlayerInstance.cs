using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Account;
using MikuSB.Database.Player;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Character;
using MikuSB.GameServer.Game.Inventory;
using MikuSB.GameServer.Server;
using MikuSB.Proto;
using MikuSB.TcpSharp;
using MikuSB.Util.Extensions;

namespace MikuSB.GameServer.Game.Player;

public class PlayerInstance(PlayerGameData data)
{
    #region Property
    public Connection? Connection { get; set; }

    public static readonly List<PlayerInstance> _playerInstances = [];
    public int Uid { get; set; }
    public bool Initialized { get; set; }
    public bool IsNewPlayer { get; set; }

    #endregion

    #region Data & Manager

    public PlayerGameData Data { get; set; } = data;
    public CharacterManager CharacterManager { get; set; } = null!;
    public InventoryManager InventoryManager { get; set; } = null!;

    #endregion

    #region Initializers
    public PlayerInstance(int uid) : this(new PlayerGameData { Uid = uid })
    {
        // new player
        IsNewPlayer = true;
        Data.Name = AccountData.GetAccountByUid(uid)?.Username;

        DatabaseHelper.CreateInstance(Data);

        var t = Task.Run(async () =>
        {
            await InitialPlayerManager();
            foreach (var card in GameData.CardData.Values) await CharacterManager.AddCharacter((ItemTypeEnum)card.Genre, card.Detail, card.Particular, card.Level);

            var bootstrapAttrs = BuildLobbyBootstrapAttrs();
            var existingAttrs = Data.Attrs
                .ToDictionary(x => (x.Gid, x.Sid));
            var seenAttrs = new HashSet<(uint Gid, uint Sid)>();

            foreach (var (gid, sid, value) in bootstrapAttrs)
            {
                if (!seenAttrs.Add((gid, sid)))
                    continue;

                if (existingAttrs.TryGetValue((gid, sid), out var attr))
                {
                    if (attr.Val < value)
                        attr.Val = value;

                    continue;
                }

                var newAttr = new PlayerAttr
                {
                    Gid = gid,
                    Sid = sid,
                    Val = value
                };

                Data.Attrs.Add(newAttr);
                existingAttrs[(gid, sid)] = newAttr;
            }
        });
        t.Wait();

        Initialized = true;
    }
    private async ValueTask InitialPlayerManager()
    {
        Uid = Data.Uid;
        Data.LastActiveTime = Extensions.GetUnixSec();
        InventoryManager = new InventoryManager(this);
        CharacterManager = new CharacterManager(this);

        await Task.CompletedTask;
    }
    public T InitializeDatabase<T>() where T : BaseDatabaseDataHelper, new()
    {
        var instance = DatabaseHelper.GetInstanceOrCreateNew<T>(Uid);
        return instance!;
    }

    #endregion

    #region Network
    public async ValueTask OnEnterGame()
    {
        if (!Initialized) await InitialPlayerManager();
    }

    public async ValueTask OnLogin()
    {
        _playerInstances.Add(this);
        await Task.CompletedTask;
    }

    public static PlayerInstance? GetPlayerInstanceByUid(long uid)
        => _playerInstances.FirstOrDefault(player => player.Uid == uid);
    public void OnLogoutAsync()
    {
        _playerInstances.Remove(this);
    }
    public async ValueTask SendPacket(BasePacket packet)
    {
        if (Connection?.IsOnline == true) await Connection.SendPacket(packet);
    }

    #endregion

    #region Actions
    public async ValueTask OnHeartBeat()
    {
        DatabaseHelper.ToSaveUidList.SafeAdd(Uid);
        await Task.CompletedTask;
    }

    #endregion

    #region Serialization

    public Proto.Player ToPlayerProto()
    {
        var proto = new Proto.Player
        {
            Pid = (ulong)Data.Uid,
            Account = Data.Name,
            Provider = Data.Name,
            Name = Data.Name,
            Level = Data.Level,
            Sex = Data.Gender,
            Vigor = 240,
            Solutions =
            {
                new Lineup // TODO Lineup Manager
                {
                    Index = 1,
                    Name = "Default",
                    Member1 = 1,
                    Member2 = 2,
                    Member3 = 3
                }
            },
        };

        //foreach(var weapon in InventoryManager.InventoryData.Weapons.Values) proto.Items.Add(weapon.ToProto());
        //foreach (var skin in InventoryManager.InventoryData.Skins.Values) proto.Items.Add(skin.ToProto());
        foreach (var chara in CharacterManager.CharacterData.Characters) proto.Items.Add(chara.ToProto());

        foreach (var x in Data.Attrs)
        {
            uint gid = x.Gid;
            uint sid = x.Sid;
            uint val = x.Val;

            if (gid == 0)
            {
                proto.Attrs[sid] = val;
                continue;
            }

            proto.Attrs[ToPackedAttrKey(gid, sid)] = val;
            proto.Attrs[ToShiftedAttrKey(gid, sid)] = val;
        }

        return proto;
    }

    private static uint ToPackedAttrKey(uint gid, uint sid)
    {
        if (gid == 0)
            return sid;

        return (gid * 10000) + sid;
    }

    private static uint ToShiftedAttrKey(uint gid, uint sid)
    {
        if (gid == 0)
            return sid;

        return (gid << 16) | sid;
    }

    private static IEnumerable<(uint Gid, uint Sid, uint Value)> BuildLobbyBootstrapAttrs()
    {
        // GuideLogic uses group 4. Value 999 is safely above every configured step count,
        // so the client treats these guides as already completed.
        yield return (4, 0, 5);
        yield return (11, 1, 1);
        yield return (57, 0, 1);
        yield return (99, 3, 30);
        yield return (110, 1, 1);
        yield return (178, 1, 1_700_000_000);
        yield return (187, 1, 2);

        for (uint guideId = 1; guideId <= 150; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 10_000; guideId <= 10_300; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 11_000; guideId <= 11_300; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 12_000; guideId <= 12_100; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 22_000; guideId <= 22_100; guideId++)
            yield return (4, guideId, 999);

        // Additional guide ids referenced directly by the Lua scripts and observed client logs.
        foreach (var guideId in new uint[] { 10_031, 10_041, 10_061, 10_081, 10_101, 10_224, 11_006, 11_202, 11_210, 22_002 })
            yield return (4, guideId, 999);

        // Launch.GPASSID = 22 stores pass counts. ChapterLevel.GID = 21 stores star flags.
        // Completing the prologue/early chapter range prevents function conditions from
        // treating the account as a fresh tutorial player.
        for (uint levelId = 10_000; levelId <= 11_007; levelId++)
        {
            yield return (21, levelId, 7);
            yield return (22, levelId, 1);
        }

        for (uint levelId = 12_001; levelId <= 12_075; levelId++)
        {
            yield return (21, levelId, 7);
            yield return (22, levelId, 1);
        }

        for (uint levelId = 13_002; levelId <= 13_081; levelId++)
        {
            yield return (21, levelId, 7);
            yield return (22, levelId, 1);
        }

        foreach (var levelId in new uint[] { 11_315, 11_311, 11_313, 11_322, 11_331, 11_333, 10_123, 50_000, 50_151 })
        {
            yield return (21, levelId, 7);
            yield return (22, levelId, 1);
        }
    }
    #endregion
}