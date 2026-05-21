using Godot;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 四槽技能栏容器，由 BrotatoLikeHud 实例化，暴露 4 个 ActiveSkillSlotUI 的绑定方法。
/// </summary>
public partial class ActiveSkillBarUI : Control
{
    /// <summary>可见主动技能槽数量。</summary>
    public const int VisibleSlotCapacity = 4;

    private ActiveSkillSlotUI? slot1;
    private ActiveSkillSlotUI? slot2;
    private ActiveSkillSlotUI? slot3;
    private ActiveSkillSlotUI? slot4;

    /// <inheritdoc />
    public override void _Ready()
    {
        var container = GetNode<HBoxContainer>("%SlotContainer");
        slot1 = container.GetNode<ActiveSkillSlotUI>("Slot1");
        slot2 = container.GetNode<ActiveSkillSlotUI>("Slot2");
        slot3 = container.GetNode<ActiveSkillSlotUI>("Slot3");
        slot4 = container.GetNode<ActiveSkillSlotUI>("Slot4");
    }

    /// <summary>
    /// 获取指定索引的技能槽（0-3）。
    /// </summary>
    public ActiveSkillSlotUI? GetSlot(int index)
    {
        return index switch
        {
            0 => slot1,
            1 => slot2,
            2 => slot3,
            3 => slot4,
            _ => null
        };
    }

    /// <summary>
    /// 清空所有槽位显示。
    /// </summary>
    public void ClearAll()
    {
        for (var i = 0; i < VisibleSlotCapacity; i++)
        {
            GetSlot(i)?.Clear();
        }
    }

    /// <summary>
    /// 写入 loadout 结构化证据，供 headless artifact 区分可见槽和总拥有技能。
    /// </summary>
    public void SetLoadoutEvidence(
        string source,
        EntityIdList ownedIds,
        EntityIdList visibleIds,
        int selectedIndex)
    {
        var selectedId = selectedIndex >= 0 && selectedIndex < visibleIds.Count
            ? visibleIds[selectedIndex].Value
            : string.Empty;
        SetMeta("LoadoutSource", source);
        SetMeta("OwnedAbilityIds", JoinIds(ownedIds));
        SetMeta("VisibleSlotIds", JoinIds(visibleIds));
        SetMeta("SelectedIndex", selectedIndex);
        SetMeta("SelectedAbilityId", selectedId);
        SetMeta("TotalOwnedCount", ownedIds.Count);
        SetMeta("VisibleSlotCount", visibleIds.Count);
        SetMeta("HiddenOwnedCount", Mathf.Max(0, ownedIds.Count - visibleIds.Count));
    }

    private static string JoinIds(EntityIdList ids)
    {
        var values = new string[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            values[i] = ids[i].Value;
        }

        return string.Join(",", values);
    }
}
