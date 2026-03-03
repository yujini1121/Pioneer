using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameUiChunk
{
    public List<GameObject> UiGameobjects = new List<GameObject>(); // UI 게임 오브젝트 리스트
    public bool isNeedCloseAction = false; // 닫을 때 작업이 필요한가
    public System.Action CloseAction = () => { }; // 닫을 때 작업
    public int id = -1;

    public InGameUiChunk() { }
    public InGameUiChunk(List<GameObject> uiGameobjects)
    {
        UiGameobjects = uiGameobjects;
    }
    public InGameUiChunk(List<GameObject> uiGameobjects, bool isNeedCloseAction, Action closeAction)
    {
        UiGameobjects = uiGameobjects;
        this.isNeedCloseAction = isNeedCloseAction;
        CloseAction = closeAction;
    }
}
