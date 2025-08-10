using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GuiltyCanvas : MonoBehaviour
{
    static public GuiltyCanvas instance;

    [SerializeField] float curseTime = 0.5f;
    [SerializeField] float curseFrameTime = 0.1f;
    [SerializeField] float curseShakeRange = 200f;
    [SerializeField] Transform middlePivot;
    [SerializeField] TextMeshProUGUI darkMarinerCurseText;
    Coroutine coroutineCarkMarinerCurseText = null;
    Coroutine coroutineCarkMarinerCurseShake = null;

    public void CurseView(string word)
    {
        Debug.Log($">> GuiltyCanvas.CurseView(string word) : 호출됨");
        Debug.Log($">> GuiltyCanvas.CurseView(string word) : 널값인가 ? {coroutineCarkMarinerCurseText == null}");

        if (darkMarinerCurseText.gameObject.activeSelf == false)
        {
            darkMarinerCurseText.gameObject.SetActive(true);
            darkMarinerCurseText.text = word;
            coroutineCarkMarinerCurseText = StartCoroutine(CoroutineCarkMarinerCurse());
        }
    }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        coroutineCarkMarinerCurseShake = StartCoroutine(CoroutineCurseShake());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator CoroutineCarkMarinerCurse()
    {
        float mTime = 0.0f;
        while (mTime < curseTime)
        {
            Debug.Log($">> GuiltyCanvas.CoroutineCarkMarinerCurse() : 시작 -> {mTime}, {curseTime}");

            mTime += Time.deltaTime;
            yield return null;
        }
        darkMarinerCurseText.gameObject.SetActive(false);
    }

    // 다만 매 프레임마다 호출되어야 함.
    IEnumerator CoroutineCurseShake()
    {
        float mTime = 0.0f;
        while (true)
        {
            mTime += Time.deltaTime;
            yield return null;
            if (mTime < curseFrameTime) continue;
            mTime = 0.0f;
            
            float radian = Random.Range(0, Mathf.PI * 2);
            float range = Random.Range(0, curseShakeRange);
            darkMarinerCurseText.transform.position = middlePivot.position + 
                new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0) * range;
        }
    }
}
