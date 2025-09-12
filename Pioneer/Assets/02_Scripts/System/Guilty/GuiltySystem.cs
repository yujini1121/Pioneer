using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class GuiltySystem : MonoBehaviour, IBegin
{
    public static GuiltySystem instance;

    public bool IsSlowed => Time.time < slowEndTime;

    public bool canUseESC = false;

    

    [Header("Dark Object")]
    public Vector3 forwardVector; // 카메라가 사선으로 배치된 경우, 이는 중요합니다.
    public Vector3 rightVector;
    [SerializeField] AudioSource AudioSourceScream;
    [SerializeField] GameObject prefabDarkObject;
    [SerializeField] GameObject prefabDarkFog;
    [SerializeField] GameObject player;
    [SerializeField] Transform pivot;
    [SerializeField] Vector2 size;
    [SerializeField] float darkObjectLifeTime;
    [SerializeField] float darkObjectTerm;
    [SerializeField] float darkFogTime;
    [SerializeField] List<float> darkFogSpawnTerm;
    [SerializeField] List<float> screamSoundChance;
    [SerializeField] List<float> screamSoundTerm;
    [SerializeField] List<float> screamSoundVolume;
    [SerializeField] Volume volumeScreenTransformation;
    private Coroutine darkObjectCoroutine;
    private Coroutine darkFogCoroutine;
    private Coroutine screamCoroutine;
    private Vector3[] mEdgePoints;
    private Vector3 mForwardVector;
    private Vector3 mRightVector;
    private Vector2 mSize;
    private float slowEndTime = 0.0f;
    private int deadCount = 0;
    private int maxAttackWeight = 20; // 변수명 레퍼런스 : https://www.notion.so/2025e8a380a580c7abe6c8c80736cb6e?v=2025e8a380a580feb76f000c763770ff&p=1e970641e0a78013a100caebc2a28a4d&pm=s
    private int currentAttackWeight = 0; // 변수명 레퍼런스 : https://www.notion.so/2025e8a380a580c7abe6c8c80736cb6e?v=2025e8a380a580feb76f000c763770ff&p=1e970641e0a78013a100caebc2a28a4d&pm=s
    private int level = 0;

    public void ChangeWeight(int value)
    {

        currentAttackWeight += value;
        currentAttackWeight = Mathf.Clamp(currentAttackWeight, 0, maxAttackWeight);

        switch (currentAttackWeight)
        {
            case >= 16: level = 4; break;
            case >= 12: level = 3; break;
            case >= 8: level = 2; break;
            case >= 4: level = 1; break;
            default: level = 0; break;
        }
        Debug.Log($">> GuiltySystem.ChangeWeight({value}) / level = {level}");


        
        if (level >= 4)
        {
            canUseESC = false;
        }
        else
        {
            canUseESC = true;
        }
        if (level >= 3)
        {
            if (darkFogCoroutine == null)
            {
                darkFogCoroutine = StartCoroutine(CoroutineDarkFog());
            }
        }
        else
        {
            if (darkFogCoroutine != null)
            {
                StopCoroutine(darkFogCoroutine);
                darkFogCoroutine = null;
            }
        }

        if (level >= 2)
        {
            volumeScreenTransformation.enabled = true;

            if (screamCoroutine == null)
            {
                screamCoroutine = StartCoroutine(CoroutineScreamSound());
            }
        }
        else
        {
            if (screamCoroutine != null)
            {
                StopCoroutine(screamCoroutine);
            }
                
            volumeScreenTransformation.enabled = false;
        }
        if (level >= 1)
        {
            if (darkObjectCoroutine == null)
            {
                darkObjectCoroutine = StartCoroutine(CoroutineDarkObject());
            }
        }
        else
        {
            if (darkObjectCoroutine != null)
            {
                StopCoroutine(darkObjectCoroutine);
            }
        }
    }

    public void CrewDead()
    {
        deadCount++;
        ChangeWeight(2);
    }
    public void TimeReachedToDayTime() => ChangeWeight(-1);
    public void Drink() => ChangeWeight(-2);
    public void DarkFogTouched() => slowEndTime = Time.time + darkFogTime;

    private void Awake()
    {
        instance = this;

        mForwardVector = forwardVector.normalized;
        mRightVector = rightVector.normalized;
        mSize = size / 2;
        Vector3 mHalf = size / 2;

        mEdgePoints = new Vector3[4]
        {
            new Vector3(size.x / 2, player.transform.position.y, size.y / 2),
            new Vector3(-size.x / 2, player.transform.position.y, size.y / 2),
            new Vector3(size.x / 2, player.transform.position.y, -size.y / 2),
            new Vector3(-size.x / 2, player.transform.position.y, -size.y / 2)
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.X) && Input.GetKeyDown(KeyCode.K))
        {
            CrewDead();
        }
        if (Input.GetKey(KeyCode.X) && Input.GetKeyDown(KeyCode.L))
        {
            TimeReachedToDayTime();
        }
        if (Input.GetKey(KeyCode.X) && Input.GetKeyDown(KeyCode.Semicolon))
        {
            Drink();
        }
    }

    private void SpawnDarkObject()
    {
        Vector3 mEdge1 = Vector3.zero;
        Vector3 mEdge2 = Vector3.zero;
        switch (Random.Range(0, 4))
        {
            case 0: mEdge1 = mEdgePoints[0]; mEdge2 = mEdgePoints[1]; break;
            case 1: mEdge1 = mEdgePoints[0]; mEdge2 = mEdgePoints[2]; break;
            case 2: mEdge1 = mEdgePoints[1]; mEdge2 = mEdgePoints[3]; break;
            case 3: mEdge1 = mEdgePoints[2]; mEdge2 = mEdgePoints[3]; break;
            default: break;
        }

        GameObject darkObject = Instantiate(prefabDarkObject,
            Vector3.Lerp(mEdge1, mEdge2, Random.Range(0f, 1f)), Quaternion.identity);
        darkObject.transform.LookAt(player.transform.position);
        Destroy(darkObject, darkObjectLifeTime);
    }
    
    private void SpawnDarkFog()
    {
        Debug.Log($">> GuiltySystem.SpawnDarkFog()");
        GameObject fog = Instantiate(prefabDarkFog, player.transform.position, Quaternion.identity);
        fog.GetComponent<DarkFog>().armedTime = Time.time + 3.0f;
    }

    IEnumerator CoroutineDarkObject()
    {
        while (true)
        {
            yield return new WaitForSeconds(darkObjectTerm);

            float mProbability = 0.0f;
            switch (level)
            {
                case 1: mProbability = 0.4f; break;
                case 2: mProbability = 0.5f; break;
                case 3: mProbability = 0.5f; break;
                case 4: mProbability = 0.7f; break;
                default: break;
            }

            if (Random.Range(0.0f, 1.0f) < mProbability)
            {
                SpawnDarkObject();
            }

        }
    }

    IEnumerator CoroutineScreamSound()
    {
        while (true)
        {
            yield return new WaitForSeconds(screamSoundTerm[level]);

            if (Random.Range(0.0f, 1.0f) < screamSoundChance[level])
            {
                AudioSourceScream.volume = screamSoundVolume[level];
                AudioSourceScream.Play();
            }
        }
    }

    IEnumerator CoroutineDarkFog()
    {
        while (true)
        {
            yield return new WaitForSeconds(darkFogSpawnTerm[level]);

            SpawnDarkFog();
        }
    }
}
