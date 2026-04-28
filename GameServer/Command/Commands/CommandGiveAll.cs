using MikuSB.Data;
using MikuSB.Database.Inventory;
using MikuSB.Enums.Item;
using MikuSB.Enums.Player;
using MikuSB.GameServer.Server.Packet.Send.Misc;
using MikuSB.Internationalization;

namespace MikuSB.GameServer.Command.Commands;

[CommandInfo("giveall", "Game.Command.GiveAll.Desc", "Game.Command.GiveAll.Usage", ["ga"], [PermEnum.Admin, PermEnum.Support])]
public class CommandGiveAll : ICommands
{
    [CommandMethod("weapon")]
    public async ValueTask GiveAllWeapon(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (await arg.GetOption('p') is not int particular) return;
        if (await arg.GetOption('l') is not int level) return;

        var detail = arg.GetInt(0);
        level = Math.Clamp(level, 1, 90);
        var player = arg.Target!.Player!;
        List<GameWeaponInfo> weapons = [];
        if (detail == -1)
        {
            // add all
            foreach (var config in GameData.WeaponData.Values)
            {
                var weapon = await player.InventoryManager!
                    .AddWeaponItem((ItemTypeEnum)config.Genre,config.Detail,config.Particular,config.Level,(uint)level,false);
                if (weapon != null) weapons.Add(weapon);
            }
        }
        else
        {
            var weapon = await player.InventoryManager!.AddWeaponItem(ItemTypeEnum.TYPE_WEAPON, (uint)detail,(uint)particular,1,(uint)level,false);
            if (weapon == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.NotFound", I18NManager.Translate("Word.Weapon")));
                return;
            }
            weapons.Add(weapon);
        }
        if (weapons.Count > 0) await player.SendPacket(new PacketNtfCallScript(weapons));
        await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.GiveAllItems",
            I18NManager.Translate("Word.Weapon"), weapons.Count.ToString()));
    }

    [CommandMethod("card")]
    public async ValueTask GiveAllSupportCard(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (await arg.GetOption('p') is not int particular) return;
        if (await arg.GetOption('l') is not int level) return;

        var detail = arg.GetInt(0);
        var player = arg.Target!.Player!;
        List<GameSupportCardInfo> supportCards = [];
        if (detail == -1)
        {
            // add all
            foreach (var config in GameData.SupportCardData)
            {
                var supportCard = await player.InventoryManager!
                    .AddSupportCardItem(config.Detail, config.Particular, config.Level, (uint)level, false);
                if (supportCard != null) supportCards.Add(supportCard);
            }
        }
        else
        {
            var supportCard = await player.InventoryManager!.AddSupportCardItem((uint)detail, (uint)particular, 1, (uint)level, false);
            if (supportCard == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.NotFound", I18NManager.Translate("Word.SupportCard")));
                return;
            }
            supportCards.Add(supportCard);
        }
        if (supportCards.Count > 0) await player.SendPacket(new PacketNtfCallScript(supportCards));
        await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.GiveAllItems",
            I18NManager.Translate("Word.SupportCard"), supportCards.Count.ToString()));
    }

    [CommandMethod("weaponskin")]
    public async ValueTask GiveAllWeaponSkin(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (await arg.GetOption('p') is not int particular) return;

        var detail = arg.GetInt(0);
        var player = arg.Target!.Player!;
        List<BaseGameItemInfo> weaponSkins = [];
        if (detail == -1)
        {
            // add all
            foreach (var config in GameData.WeaponSkinData.Values)
            {
                var weaponSkin = await player.InventoryManager!
                    .AddWeaponSkinItem((ItemTypeEnum)config.Genre, config.Detail, config.Particular, 1, false);
                if (weaponSkin != null) weaponSkins.Add(weaponSkin);
            }
        }
        else
        {
            var weaponSkin = await player.InventoryManager!.AddWeaponSkinItem(ItemTypeEnum.TYPE_WEAPON, (uint)detail, (uint)particular, 1, false);
            if (weaponSkin == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.NotFound", I18NManager.Translate("Word.WeaponSkin")));
                return;
            }
            weaponSkins.Add(weaponSkin);
        }
        if (weaponSkins.Count > 0) await player.SendPacket(new PacketNtfCallScript(weaponSkins));
        await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.GiveAllItems",
            I18NManager.Translate("Word.WeaponSkin"), weaponSkins.Count.ToString()));
    }
}