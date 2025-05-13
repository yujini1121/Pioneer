using UnityEngine;
using UnityEngine.UI;

public class CheckBottom : MonoBehaviour
{
    public int playerHp = 100;
    public int maxHp = 100;
    public Slider hpBar;
    public float breathTime = 30f;
    public float damageInterval = 5f;
    public int damageAmount = 10;
    public float returnSpeed = 2f;
    public KeyCode returnKey = KeyCode.B;
    public Vector3 shipPosition = Vector3.zero;

    private bool isInSea = false;
    private float seaTimer = 0f;
    private float damageTimer = 0f;
    private bool isReturning = false;

    void Start()
    {
        if (hpBar != null)
        {
            hpBar.maxValue = maxHp;
            hpBar.value = playerHp;
        }
    }

    void Update()
    {
        if (isInSea)
        {
            seaTimer += Time.deltaTime;

            if (seaTimer > breathTime)
            {
                damageTimer += Time.deltaTime;
                if (damageTimer >= damageInterval)
                {
                    ApplyDamage(damageAmount);
                    damageTimer = 0f;
                }
            }

            if (Input.GetKeyDown(returnKey))
            {
                isReturning = true;
            }
        }

        if (isReturning)
        {
            transform.position = Vector3.MoveTowards(transform.position, shipPosition, returnSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, shipPosition) < 0.1f)
            {
                isReturning = false;
                isInSea = false;
                seaTimer = 0f;
                damageTimer = 0f;
                Debug.Log("복귀 완료");
            }
        }
    }

    private void ApplyDamage(int amount)
    {
        playerHp -= amount;
        playerHp = Mathf.Clamp(playerHp, 0, maxHp);

        if (hpBar != null)
        {
            hpBar.value = playerHp;
        }

        Debug.Log($"HP 감소: {amount}, 현재 HP: {playerHp}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Sea"))
        {
            isInSea = true;
            seaTimer = 0f;
            damageTimer = 0f;
            Debug.Log("바다에 진입");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Sea"))
        {
            isInSea = false;
            seaTimer = 0f;
            damageTimer = 0f;
            Debug.Log("바다에서 나옴");
        }
    }
}
