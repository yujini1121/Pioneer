using UnityEngine;

[CreateAssetMenu(fileName = "InstallableObject", menuName = "ScriptableObjects/Installables/InstallableObjects")]
public class SInstallableObjectDataSO : SItemTypeSO
{
    public enum CreationType { Platform, Wall, Door, Barricade, CraftingTable, Ballista, Trap, Lantern, Storage }

    public enum AnchorType
    {
        Center,
        EdgeX,
        EdgeZ,
        Corner
    }

    [Header("Install Type")]
    public CreationType installType;

    [Header("Prefab And Settings")]
    public GameObject prefab;
    public Vector3 size = Vector3.one;

    [Header("Grid")]
    public float gridCellSize = 0f;

    [Header("Footprint")]
    public Vector2Int footprint = Vector2Int.one;
    public bool swapFootprintOnRotate90 = true;

    [Header("Anchor")]
    public AnchorType anchor = AnchorType.Center;

    [Header("Stats")]
    public int maxHp = 20;
    public float buildTime = 2f;

    public Vector2Int GetFootprintByRotateN(int rotateN)
    {
        var fp = footprint;
        if (!swapFootprintOnRotate90)
            return fp;

        bool is90or270 = (rotateN & 1) == 1;
        if (is90or270)
            return new Vector2Int(fp.y, fp.x);

        return fp;
    }

    public Vector2 GetAnchorOffsetCellsByRotateN(int rotateN)
    {
        Vector2 offset;
        switch (anchor)
        {
            default:
            case AnchorType.Center: offset = new Vector2(0f, 0f); break;
            case AnchorType.EdgeX: offset = new Vector2(0.5f, 0f); break;
            case AnchorType.EdgeZ: offset = new Vector2(0f, 0.5f); break;
            case AnchorType.Corner: offset = new Vector2(0.5f, 0.5f); break;
        }

        int r = ((rotateN % 4) + 4) % 4;
        if (anchor == AnchorType.EdgeX || anchor == AnchorType.EdgeZ)
        {
            if ((r & 1) == 1)
                offset = new Vector2(offset.y, offset.x);
        }

        return offset;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gridCellSize < 0f) gridCellSize = 0f;
        if (footprint.x < 1) footprint.x = 1;
        if (footprint.y < 1) footprint.y = 1;
        if (buildTime < 0.1f) buildTime = 0.1f;
        if (maxHp < 1) maxHp = 1;
    }
#endif
}
