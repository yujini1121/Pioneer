using UnityEngine;

public static class ConsumeEffectInfo
{
    public enum Stat { None, Health, Fullness, Mental }

    public static bool TryGet(int effect, out Stat stat, out int amount)
    {
        stat = Stat.None;
        amount = 0;
        switch (effect)
        {
            case 801: stat = Stat.Health; amount = 15; break;
            case 802: stat = Stat.Health; amount = 40; break;
            case 803: stat = Stat.Health; amount = 70; break;
            case 804: stat = Stat.Fullness; amount = 5; break;
            case 805: stat = Stat.Fullness; amount = 20; break;
            case 806: stat = Stat.Fullness; amount = 40; break;
            case 807: stat = Stat.Fullness; amount = 70; break;
            case 808: stat = Stat.Mental; amount = 10; break;
            case 809: stat = Stat.Mental; amount = 30; break;
            case 810: stat = Stat.Mental; amount = 60; break;
        }
        return stat != Stat.None;
    }

    public static string Summary(int effect)
    {
        if (!TryGet(effect, out var stat, out int amount)) return "";
        string label = stat == Stat.Health ? "체력" : stat == Stat.Fullness ? "허기" : "정신력";
        return $"{label} +{amount}";
    }

    public static void Apply(int effect, PlayerCore player)
    {
        if (player == null || !TryGet(effect, out var stat, out int amount)) return;
        switch (stat)
        {
            case Stat.Health:
                player.hp = Mathf.Min(player.maxHp, player.hp + amount);
                if (CreatureEffect.Instance != null)
                    CreatureEffect.Instance.PlayEffectFollow(CreatureEffect.Instance.GetEffect(3), player.transform, Vector3.zero);
                break;
            case Stat.Fullness: player.EatFoodFullness(amount); break;
            case Stat.Mental: player.UpdateMental(amount); break;
        }
    }
}
