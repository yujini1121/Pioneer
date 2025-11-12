using UnityEngine;

public class CreatureEffect : MonoBehaviour
{
    public static CreatureEffect Instance;

    [Header("게임 오브젝트에 넣을 이펙트")]
    public ParticleSystem[] Effects;

    [Header("자신 혹은 먼 다른 게임오브젝트에 소환시킬 이펙트를 담은 프리펩")]
    public GameObject prefabForEffect;
    // 이후에 더 추가할 것
    // 그러나 게임오브젝트는 반드시 프로젝트 창에 있는 프리펩이여야지.
    // 절대로 씬에 위치한, 어떤 게임오브젝트의 자식 게임오브젝트를 긁어와선 안 됨

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    public void PlayEffect(ParticleSystem effect, Vector3 position)
    {
        ParticleSystem ps = effect;
        if (ps != null)
        {
            ps.transform.position = position;
            ps.transform.rotation = Quaternion.identity;
            ps.gameObject.SetActive(true);
            ps.Play();
        }
        else
        {
            Debug.Log("이펙트 널 버그");
        }
    }

    public void PlayEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab != null)
        {
            Instantiate(effectPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.Log("이펙트 널 버그");
        }
    }
}
