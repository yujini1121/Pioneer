using UnityEngine;

public class CreatureEffect : MonoBehaviour
{
    public static CreatureEffect Instance;

    [Header("게임 오브젝트에 넣을 이펙트")]
    public ParticleSystem[] Effects;

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
}
