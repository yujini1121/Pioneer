using UnityEngine;
using UnityEngine.UI;

// PlayerController: 입력만 처리해서 다른 스크립트에 명령 내리기
public class PlayerController : MonoBehaviour
{
    private PlayerCore playerCore;
    private PlayerFishing playerFishing;

    private Vector3 lastMoveDirection;

    [Header("낚시 1번 바다 확인 방법 ")]
    public float rayOffset;
    public LayerMask seaLayer;
    public LayerMask groundLayer;
    public bool isSeaInFront = false;
    public GameObject fishingUI;
    LayerMask combinedMask;

    [Header("낚시 2번 바다 확인 방법 ")]
    public Vector3 checkBoxCenter;
    public Vector3 checkBoxHalfExtents;
    public float checkBoxOffset;

    private float currentChargeTime;
    private bool isCharging;
    public float ChargeTime;
    public Slider chargeSlider;

    void Awake()
    {
        playerCore = GetComponent<PlayerCore>();
        playerFishing = GetComponent<PlayerFishing>();
        // playerAttack = GetComponentInChildren<PlayerAttack>();

        combinedMask = seaLayer | groundLayer;
    }

    private void Start()
    {
        
    }

    void Update()
    {
        // 낚시 1번 바다 확인 방법
        isSeaInFront = CheckSea();

        if (isSeaInFront)
        {
            fishingUI.gameObject.SetActive(true);

            if (Input.GetKeyDown(KeyCode.Q))
            {
                isCharging = true;
                currentChargeTime = 0f;
            }

            // q를 1초이상 누를 경우 낚시 시작 조건 위치
            if (Input.GetKey(KeyCode.Q))
            {
                currentChargeTime += Time.deltaTime;
                playerCore.SetState(PlayerCore.PlayerState.ChargingFishing);

                if (currentChargeTime >= ChargeTime)
                {
                    Debug.Log("낚시 시작!");
                    playerCore.SetState(PlayerCore.PlayerState.ActionFishing);
                    playerFishing.StartFishingLoop();
                    isCharging = false;
                }
            }

            if (isCharging && Input.GetKeyUp(KeyCode.Q))
            {
                isCharging = false;
                currentChargeTime = 0f;
                playerCore.SetState(PlayerCore.PlayerState.Default);
            }

            chargeSlider.value = currentChargeTime / ChargeTime;
        }
        else
        {
            playerCore.SetState(PlayerCore.PlayerState.Default);
            fishingUI.gameObject.SetActive(false);
        }

        // move
        if (playerCore.currentState == PlayerCore.PlayerState.Default)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            Vector3 moveInput = new Vector3(moveX, 0, moveY);
            Vector3 moveDirection = moveInput.normalized;
            playerCore.Move(moveInput);

            if (moveDirection != Vector3.zero)
            {
                lastMoveDirection = moveDirection;
            }

            if (Input.GetMouseButtonDown(0))
            {
                playerCore.Attack();
            }
        }
    }

    
    private bool CheckSea()
    {
        // 바다 체크 1번 방법
        Vector3 startRayPoint = transform.position + lastMoveDirection * rayOffset;
        Vector3 rayDir = Vector3.down;

        Debug.DrawRay(startRayPoint, rayDir, Color.white);
        if (Physics.Raycast(startRayPoint, rayDir, out RaycastHit hit, combinedMask))
        {
            int hitLayer = hit.collider.gameObject.layer;

            if ((seaLayer.value & (1 << hitLayer)) > 0)
                return true;
            else
                return false;
        }
        else
            return false;

        /*// 바다 체크 2번 방법
        return Physics.CheckBox(transform.position + Vector3.down * checkBoxOffset, checkBoxHalfExtents, Quaternion.identity, seaLayer);*/
    }

    /*/// <summary>
    /// Scene 뷰에 바다 감지 박스를 그립니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        // 게임이 실행 중일 때만 그리도록 하여 에디터 오류를 방지합니다.
        if (!Application.isPlaying)
            return;

        // 1. 중심점(Center)을 Physics.CheckBox와 동일한 변수인 checkBoxCenter로 설정합니다.
        Vector3 gizmoCenter = transform.position + Vector3.down * checkBoxOffset;

        // 2. 크기(Size)를 Physics.CheckBox와 동일하게 설정합니다.
        // Physics.CheckBox는 크기의 '절반(HalfExtents)'을 사용하고,
        // Gizmos.DrawCube는 '전체(Full)' 크기를 사용하므로 2를 곱해줍니다.
        Vector3 gizmoSize = checkBoxHalfExtents * 2;

        // 3. 색상(Color)을 실제 체크 결과인 isSeaInFront 변수와 연동합니다.
        // isSeaInFront가 true이면 파란색, false이면 회색으로 표시됩니다.
        Gizmos.color = isSeaInFront ? Color.blue : Color.white;

        // 4. Gizmos.DrawCube 함수로 박스를 직접 그립니다.
        // CheckBox의 회전값이 Quaternion.identity(회전 없음)이므로, 이 방법이 더 간단하고 직관적입니다.
        Gizmos.DrawCube(gizmoCenter, gizmoSize);
    }*/
}
