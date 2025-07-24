using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TY_PlayerStatus : MonoBehaviour, IBegin
{
    static public TY_PlayerStatus instance;

    public int Wood { get => wood; }
    public int Energy { get => energy; }

    private int wood = 0;
    private int energy = 0;

    public void AddWood(int count)
    {
        wood += count;
    }

    public bool UseWood(int count)
    {
        if (wood < count) return false;
        wood -= count;
        return true;
    }

    public bool UseEnergy(int count)
    {
        if (energy < count) return false;
        energy -= count;
        return true;
    }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Init()
    {
        StartCoroutine(GetEnergy());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator GetEnergy()
    {
        while (true)
        {
            yield return new WaitForSeconds(6);
            energy++;
        }
    }
}
