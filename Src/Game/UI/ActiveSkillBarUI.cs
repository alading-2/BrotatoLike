using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 四槽技能栏容器，由 BrotatoLikeHud 实例化，暴露 4 个 ActiveSkillSlotUI 的绑定方法。
/// </summary>
public partial class ActiveSkillBarUI : Control
{
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
        for (var i = 0; i < 4; i++)
        {
            GetSlot(i)?.Clear();
        }
    }
}
