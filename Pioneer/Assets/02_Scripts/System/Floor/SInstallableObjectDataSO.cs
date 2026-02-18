using UnityEngine;

[CreateAssetMenu(fileName = "InstallableObject", menuName = "ScriptableObjects/Installables/InstallableObjects")]
public class SInstallableObjectDataSO : SItemTypeSO
{
    public enum CreationType { Platform, Wall, Door, Barricade, CraftingTable, Ballista, Trap, Lantern, Storage }

    public enum AnchorType
    {
        Center,   // 셀 중심(기본): (0, 0)
        EdgeX,    // X만 반칸: (0.5, 0)
        EdgeZ,    // Z만 반칸: (0, 0.5)
        Corner    // X,Z 둘 다 반칸: (0.5, 0.5)
    }

    [Header("설치 타입")]
    public CreationType installType;

    [Header("설치 프리팹 및 설정")]
    public GameObject prefab;                  // 설치 대상 프리팹

    [Tooltip("설치 판정용 Overlap/CheckBox 크기(미터) 그러니까 Unity 단위상 모델링의 실제 크기 작성")]
    public Vector3 size = Vector3.one;         // 설치 판정용 Overlap 크기

    [Header("Footprint(셀 점유)")]
    [Tooltip("그리드 셀 기준 점유 크기. 지금은 cellSize를 2x2로 놓은 걸 기준으로 작성했어요. (X=가로, Y=세로(Z))")]
    public Vector2Int footprint = Vector2Int.one;

    [Tooltip("직사각형(footprint.x != footprint.y) 오브젝트는 90/270도 회전 시 (x,y)를 자동 스왑할지 (변수명 진짜 안예쁘다..)")]
    public bool swapFootprintOnRotate90 = true;

    [Header("Anchor(스냅 오프셋)")]
    [Tooltip("셀 중심 스냅 기준으로 반칸 오프셋을 줄지 결정")]
    public AnchorType anchor = AnchorType.Center;

    [Header("기능 확장")]
    public int maxHp = 20;                     // 내구도
    public float buildTime = 2f;               // 설치 시간

    /// <summary>
    /// rotateN: 0,1,2,3 (0=0도, 1=90도, 2=180도, 3=270도)
    /// 반환: 현재 회전 기준 점유 셀 (X,Z)
    /// </summary>
    public Vector2Int GetFootprintByRotateN(int rotateN)
    {
        var fp = footprint;
        if (!swapFootprintOnRotate90) return fp;

        // 90/270도에서만 스왑하기
        bool is90or270 = (rotateN & 1) == 1;
        if (is90or270)
            return new Vector2Int(fp.y, fp.x);

        return fp;
    }

    /// <summary>
    /// 셀 중심 스냅 좌표에 더해줄 "셀 단위 오프셋"을 반환합니다.
    /// 예) Corner면 (0.5, 0.5) 셀 만큼 이동 = 월드에선 (cellSize*0.5, cellSize*0.5)
    /// rotateN에 따라 EdgeX/EdgeZ는 서로 바뀔 수 있습니다.
    /// 머리 터질 것 같아요? 저도요.......
    /// </summary>
    public Vector2 GetAnchorOffsetCellsByRotateN(int rotateN)
    {
        // 기본(회전 0 기준)
        Vector2 offset;
        switch (anchor)
        {
            default:
            case AnchorType.Center: offset = new Vector2(0f, 0f); break;
            case AnchorType.EdgeX: offset = new Vector2(0.5f, 0f); break;
            case AnchorType.EdgeZ: offset = new Vector2(0f, 0.5f); break;
            case AnchorType.Corner: offset = new Vector2(0.5f, 0.5f); break;
        }

        // 90/180/270 회전 시 오프셋도 회전(셀 좌표계에서)
        int r = ((rotateN % 4) + 4) % 4;
        if (anchor == AnchorType.EdgeX || anchor == AnchorType.EdgeZ)
        {
            // 90/270이면 X/Z 반칸이 서로 바뀜
            if ((r & 1) == 1)
                offset = new Vector2(offset.y, offset.x);
        }

        return offset;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 실수 방지: 0 이하 입력 방지
        if (footprint.x < 1) footprint.x = 1;
        if (footprint.y < 1) footprint.y = 1;
        if (buildTime < 0.1f) buildTime = 0.1f;
        if (maxHp < 1) maxHp = 1;
    }
#endif
}
