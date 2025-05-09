using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class UiManagerTeaYoon : MonoBehaviour
{
    static public UiManagerTeaYoon instance;

    [SerializeField] TextMeshProUGUI text;
    [SerializeField] LayerMask mapMaskLayer;

    public void UpdateText()
    {
        text.text = $"Wood : {PlayerStatus.instance.Wood}\nEnergy : {PlayerStatus.instance.Energy}";
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateText();
        Shot();
    }

    void Shot()
    {



        Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        RaycastHit hitAtGameObject;

        RaycastHit m_hitOnMap;
        RaycastHit m_hitOnUnit;

        Debug.DrawRay(m_rayFromMouse.origin, m_rayFromMouse.direction*200);
        
        if (Input.GetMouseButtonDown(0) == false) return;
        Debug.LogWarning("AAA");


        if (Physics.Raycast(
            m_rayFromMouse.origin,
            m_rayFromMouse.direction,
            out m_hitOnMap,
            maxDistance: 1000.0f,
            mapMaskLayer))
        {
            // ... RayCastHit m_hitOnMap의 정보를 활용한 코드

            Debug.LogWarning("Cliocked");

            RaycastObjectTeaYoon raycastObject = m_hitOnMap.collider.gameObject.gameObject.GetComponent<RaycastObjectTeaYoon>();
            if (raycastObject != null)
            {
                if (raycastObject.info == "wood")
                {
                    PlayerStatus.instance.AddWood(1);
                }
                if (raycastObject.info == "enemy")
                {
                    Instantiate(PrefabHolderTeaYoon.instance.woods, m_hitOnMap.collider.transform.position + new Vector3(UnityEngine.Random.Range(-2, 2), 0, UnityEngine.Random.Range(-2, 2)), quaternion.identity);
                }

                Destroy(m_hitOnMap.collider.gameObject);
            }




            


        }
    }
}
