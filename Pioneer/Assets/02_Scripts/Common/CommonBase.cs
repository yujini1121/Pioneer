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
    [Range(0f, 1f)] public float hitFlashAmount = 1f;
    public float hitFlashDurations = 0.2f;
    public Color hitFlashColor = Color.white;
    public float hitFlashEmission = 2f;

    // public으로 변경해서 외부에서 설정 가능하게
    [HideInInspector] public SpriteRenderer spriteRenderer;
    private Material material;
    private Coroutine hitFlashCoroutine;

    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashEmissionID = Shader.PropertyToID("_FlashEmission");

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

            // 셰이더 프로퍼티 기본값 초기화
            if (material.HasProperty(FlashColorID))
                material.SetColor(FlashColorID, hitFlashColor);

            if (material.HasProperty(FlashAmountID))
                material.SetFloat(FlashAmountID, 0f);

            if (material.HasProperty(FlashEmissionID))
                material.SetFloat(FlashEmissionID, hitFlashEmission);
        }
        else
        {
            // SpriteRenderer가 없는 오브젝트는 피격 이펙트 생략
            return;
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

                // 연속 피격 시 이전 플래시가 남지 않도록 즉시 초기화
                if (material.HasProperty(FlashAmountID))
                    material.SetFloat(FlashAmountID, 0f);
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
        if (material == null)
            yield break;

        if (material.HasProperty(FlashColorID))
            material.SetColor(FlashColorID, hitFlashColor);

        if (material.HasProperty(FlashEmissionID))
            material.SetFloat(FlashEmissionID, hitFlashEmission);

        if (material.HasProperty(FlashAmountID))
            material.SetFloat(FlashAmountID, hitFlashAmount);

        yield return new WaitForSeconds(hitFlashDurations);

        if (material.HasProperty(FlashAmountID))
            material.SetFloat(FlashAmountID, 0f);

        hitFlashCoroutine = null;
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