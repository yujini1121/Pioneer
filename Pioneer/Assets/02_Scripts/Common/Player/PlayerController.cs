using UnityEngine;

// PlayerController: 입력만 처리해서 다른 스크립트에 명령 내리기
public class PlayerController : MonoBehaviour
{
    private PlayerCore playerCore;
    private PlayerAttack playerAttack;

    void Awake()
    {
        playerCore = GetComponent<PlayerCore>();
        playerAttack = GetComponentInChildren<PlayerAttack>();
    }

    void Update()
    {
        // move
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        playerCore.Move(new Vector3(moveX, 0, moveY));

        if(Input.GetMouseButtonDown(0))
        {
            playerCore.Attack();
        }
    }
}
