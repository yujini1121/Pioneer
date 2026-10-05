using UnityEngine;

public static class ItemPresentation
{
    public static Color CategoryColor(EDataType category)
    {
        switch (category)
        {
            case EDataType.WeaponItem: return new Color(0.73f, 0.43f, 0.40f);
            case EDataType.CommonResource: return new Color(0.64f, 0.57f, 0.47f);
            case EDataType.ConsumeItem: return new Color(0.52f, 0.72f, 0.52f);
            case EDataType.BuildObject: return new Color(0.40f, 0.66f, 0.72f);
            default: return new Color(0.62f, 0.62f, 0.59f);
        }
    }

    public static string EffectSummary(SItemTypeSO item)
    {
        if (item is SItemConsumeTypeSO consume) return ConsumeEffectInfo.Summary(consume.ConsumeEffect);
        if (item is SItemWeaponTypeSO weapon) return $"기본 공격력 {weapon.weaponDamage:0.##}";
        if (item is SInstallableObjectDataSO build) return $"내구도 {build.maxHp}";
        return "";
    }

    public static string Description(SItemTypeSO item)
    {
        string summary = EffectSummary(item);
        if (string.IsNullOrEmpty(summary)) return item.infomation;
        Color ink = Color.Lerp(CategoryColor(item.categories), Color.black, 0.5f);
        return item.infomation + "\n\n<color=#" + ColorUtility.ToHtmlStringRGB(ink)
            + ">" + summary + "</color>";
    }
}
