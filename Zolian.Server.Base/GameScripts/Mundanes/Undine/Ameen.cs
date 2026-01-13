using Darkages.Common;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Undine;

[Script("Ameen")]
public sealed class Ameen(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private Item? _itemDetail;
    private uint _cost;

    private const ushort R_SelectItemFromSellDialog = 0x00;
    private const ushort R_Top_Detail = 0x01;
    private const ushort R_Top_Nothing = 0x02;
    private const ushort R_Intro_Proceed = 0x03;
    private const ushort R_Confirm_Yes = 0x19;
    private const ushort R_Confirm_No = 0x20;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        ShowTopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        ShowTopMenu(client);
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseID)
        {
            case R_SelectItemFromSellDialog:
                HandleItemSelection(client, args);
                return;

            case R_Top_Detail:
                ShowDetailIntro(client);
                return;

            case R_Top_Nothing:
                ShowGoodbye(client);
                return;

            case R_Intro_Proceed:
                ShowEligibleItemsList(client);
                return;

            case R_Confirm_Yes:
                HandleConfirmYes(client);
                return;

            case R_Confirm_No:
                HandleConfirmNo(client);
                return;

            default:
                ShowTopMenu(client);
                return;
        }
    }

    private void ShowTopMenu(WorldClient client)
    {
        var options = new[]
        {
            new Dialog.OptionsDataItem(R_Top_Detail, "Detail"),
            new Dialog.OptionsDataItem(R_Top_Nothing, "Nothing"),
        };

        client.SendOptionsDialog(Mundane, "*cleans glass* Need something?", options);
    }

    private void ShowDetailIntro(WorldClient client)
    {
        var options = new[]
        {
            new Dialog.OptionsDataItem(R_Intro_Proceed, "Let's do that."),
            new Dialog.OptionsDataItem(R_Top_Nothing, "Not now."),
        };

        client.SendOptionsDialog(
            Mundane,
            "You've heard of my service then? I'm able to increase the quality of an item for a fee.",
            options
        );
    }

    private void ShowGoodbye(WorldClient client) => client.SendOptionsDialog(Mundane, "You know where I'll be.");
    private void SayPublic(WorldClient client, string message) => client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: {message}");

    private void ApplyUpgradeSideEffectsAndCharge(WorldClient client, Item item, uint cost)
    {
        ItemQualityVariance.ItemDurability(item, item.ItemQuality);
        client.Aisling.Inventory.UpdateSlot(client, item);
        client.Aisling.GoldPoints -= cost;
        client.SendAttributes(StatUpdateType.WeightGold);
        SayPublic(client, $"{client.Aisling.Username}, good as new!");
        client.SendOptionsDialog(Mundane, $"I put a lot of work into your {item.DisplayName}{{=a, hope you like it.");
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item is null)
            return;

        // Fast reject: stackable or not enchantable -> never polishable
        if (item.Template.CanStack || !item.Template.Enchantable)
        {
            SayPublic(client, $"{client.Aisling.Username}, I can't polish that.");
            return;
        }

        // Drop-path supports polishing only for these states
        if (item.ItemQuality is Item.Quality.Uncommon or Item.Quality.Common or Item.Quality.Damaged)
        {
            HandleItemSelection(client, item.InventorySlot.ToString());
            return;
        }

        SayPublic(client, $"{client.Aisling.Username}, sorry I don't have the skill to polish that.");
    }

    #region Item Selection and Confirmation

    private void ShowEligibleItemsList(WorldClient client)
    {
        // Keep your existing eligibility list source
        var eligible = NpcShopExtensions.GetCharacterDetailingByteListForMidGradePolish(client);

        if (eligible.Count == 0)
        {
            client.SendOptionsDialog(Mundane, "Your backpack doesn't have any items that I can detail for you.");
            return;
        }

        client.SendItemSellDialog(
            Mundane,
            "I can polish and upgrade Damaged, Common, or Uncommon items. Pick an item you'd like polished.",
            eligible
        );
    }

    private void HandleItemSelection(WorldClient client, string args)
    {
        // Compute cost and capture selected item from shared extension state
        _cost = NpcShopExtensions.GetDetailCosts(client, args);
        _itemDetail = NpcShopExtensions.ItemDetail;

        if (!TryLoadAndValidateDetailTarget(client, _itemDetail, _cost, out var item, out var cost))
            return;

        ShowConfirm(client, cost);
    }

    private void ShowConfirm(WorldClient client, uint cost)
    {
        var options = new[]
        {
            new Dialog.OptionsDataItem(R_Confirm_Yes, ServerSetup.Instance.Config.MerchantConfirmMessage),
            new Dialog.OptionsDataItem(R_Confirm_No,  ServerSetup.Instance.Config.MerchantCancelMessage),
        };

        client.SendOptionsDialog(Mundane, $"It'll cost you {cost} gold for this service.", options);
    }

    private void HandleConfirmYes(WorldClient client)
    {
        // Re-acquire item from extension
        _itemDetail = NpcShopExtensions.ItemDetail;

        if (!TryLoadAndValidateDetailTarget(client, _itemDetail, _cost, out var item, out var cost))
            return;

        if (client.Aisling.GoldPoints < cost)
        {
            client.SendOptionsDialog(Mundane, "Looks like you don't have enough.");
            return;
        }

        if (!TryPolishUpgrade(item, out var failureReason))
        {
            client.SendOptionsDialog(Mundane, failureReason);
            return;
        }

        ApplyUpgradeSideEffectsAndCharge(client, item, cost);
    }

    private void HandleConfirmNo(WorldClient client) => client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);

    #endregion

    #region Validation

    private bool TryLoadAndValidateDetailTarget(WorldClient client, Item? item, uint cost, out Item validatedItem, out uint validatedCost)
    {
        validatedItem = null!;
        validatedCost = cost;

        if (cost == 0)
        {
            client.SendOptionsDialog(Mundane, "Yea, I won't touch that.");
            return false;
        }

        if (item is null)
        {
            client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
            return false;
        }

        // Don’t touch stacked items
        if (item.Stacks > 1 && item.Template.CanStack)
        {
            client.SendOptionsDialog(Mundane, "Yea, I won't touch that.");
            return false;
        }

        // If original quality is better than current quality, item needs repair first
        if (CompareQuality(item.OriginalQuality, item.ItemQuality) > 0)
        {
            client.SendOptionsDialog(
                Mundane,
                "I can't help you with that. That item needs to be repaired first, bring it back once it's restored."
            );
            return false;
        }

        // This NPC is explicitly a polish ladder up to Rare
        if (item.ItemQuality is Item.Quality.Rare
            or Item.Quality.Epic
            or Item.Quality.Legendary
            or Item.Quality.Forsaken
            or Item.Quality.Mythic
            or Item.Quality.Primordial
            or Item.Quality.Transcendent)
        {
            client.SendOptionsDialog(Mundane, "Sorry, I don't have the expertise to polish items of that quality.");
            return false;
        }

        if (item.ItemQuality is not (Item.Quality.Damaged or Item.Quality.Common or Item.Quality.Uncommon))
        {
            client.SendOptionsDialog(Mundane, "Yea, I won't touch that.");
            return false;
        }

        validatedItem = item;
        return true;
    }

    private static bool TryPolishUpgrade(Item item, out string failureReason)
    {
        failureReason = string.Empty;

        var before = item.ItemQuality;

        var after = before switch
        {
            Item.Quality.Damaged => Item.Quality.Common,
            Item.Quality.Common => Item.Quality.Uncommon,
            Item.Quality.Uncommon => Item.Quality.Rare,
            _ => default
        };

        if (after == default)
        {
            failureReason = "I can only polish Damaged, Common, or Uncommon items.";
            return false;
        }

        if (CompareQuality(after, before) <= 0)
        {
            failureReason = "I won't make it worse than it already is.";
            return false;
        }

        item.ItemQuality = after;

        if (CompareQuality(item.OriginalQuality, item.ItemQuality) < 0)
            item.OriginalQuality = item.ItemQuality;

        return true;
    }

    private static int CompareQuality(Item.Quality a, Item.Quality b) => ((int)a).CompareTo((int)b);

    #endregion
}
