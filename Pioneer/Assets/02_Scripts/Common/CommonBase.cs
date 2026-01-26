using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 최상위 부모 스크립트
public class CommonBase : MonoBehaviour, IBegin
{
    public int hp;
    public int maxHp;
    public bool IsDead = false;
    public GameObject attacker = null;
    public Vector3 dropOffset;
    public int CurrentHp => hp;

    [Header("Hit Effect")]
    public float hitFlashDuration = 0.1f;
    public Color hitColor = new Color(3f, 3f, 3f, 1f);

    // public으로 변경해서 외부에서 설정 가능하게
    [HideInInspector] public SpriteRenderer spriteRenderer;
    private Material material;
    private Coroutine hitFlashCoroutine;

    void Start()
    {
        hp = maxHp;

        // SpriteRenderer를 찾아서 초기화
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        InitializeHitEffect();
    }

    // 나중에 초기화될 수도 있으니 별도 함수로 분리
    public void InitializeHitEffect()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            Debug.Log($"[{gameObject.name}] SpriteRenderer 찾음! 오브젝트: {spriteRenderer.gameObject.name}");

            // Material을 인스턴스화
            if (material == null)
            {
                material = new Material(spriteRenderer.material);
                spriteRenderer.material = material;
                Debug.Log($"[{gameObject.name}] Material 생성 완료!");
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] SpriteRenderer를 찾을 수 없습니다!");
        }
    }

    void Update()
    {

    }

    // 데미지 받는 함수
    public virtual void TakeDamage(int damage, GameObject attacker)
    {
        if (IsDead) return;

        hp -= damage;
        Debug.Log(gameObject.name + "가 " + damage + "의 데미지를 입었습니다! 현재 체력: " + hp);
        this.attacker = attacker;

        // Material이 null이면 다시 초기화 시도
        if (material == null)
        {
            InitializeHitEffect();
        }

        // 피격 효과 실행
        if (material != null && spriteRenderer != null)
        {
            if (hitFlashCoroutine != null)
            {
                StopCoroutine(hitFlashCoroutine);
            }

            hitFlashCoroutine = StartCoroutine(HitFlashEffect());
        }

        if (hp <= 0)
        {
            IsDead = true;
            WhenDestroy();
        }
    }

    // 피격 효과
    private IEnumerator HitFlashEffect()
    {
        // 밝은 흰색으로 빛나게
        Color brightWhite = new Color(2.5f, 2.5f, 2.5f, 1f);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", brightWhite);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", brightWhite);

        material.color = brightWhite;

        yield return new WaitForSeconds(hitFlashDuration);

        // 원래 색상으로 복구
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        material.color = Color.white;
    }

    // 사라졌을때 호출하는 변수 (생명체인 경우 사망했을 때)
    public virtual void WhenDestroy()
    {
        Debug.Log($"{gameObject.name} 오브젝트 파괴");
        ItemDropper dropper = GetComponent<ItemDropper>();
        if (dropper != null)
        {
            ItemDropManager.instance.Drop(dropper.GetDroppedItems(), transform.position + dropOffset);
        }
        Destroy(gameObject);
    }
}