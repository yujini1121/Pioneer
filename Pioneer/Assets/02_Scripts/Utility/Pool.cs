using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System;

public class IdObject<T>
{
    public int id;
    public T Value;
}

public class Pool<T>
{
    int elementCount = 0;

    // 준비된 객체
    private readonly List<IdObject<T>> _readyList;

    // 사용 중 객체 (ID 기준 정렬)
    private readonly SortedDictionary<int, T> _inUse
        = new SortedDictionary<int, T>();

    public Pool()
    {
        _readyList = new List<IdObject<T>>();
    }

    public Pool(List<T> initialItems)
    {
        _readyList = new List<IdObject<T>>();

        for (int i = 0; i < initialItems.Count; ++i)
        {
            _readyList.Add(new IdObject<T> { id = i, Value = initialItems[i] });
        }
        elementCount = initialItems.Count;
    }

    // 초기 추가
    public void Add(T item)
    {
        _readyList.Add(new IdObject<T>() { id = elementCount, Value = item });
        elementCount++;
    }
    // 초기 추가
    public void Add(T item, out IdObject<T> self)
    {
        self = new IdObject<T>() { id = elementCount, Value = item };
        _readyList.Add(self);
        elementCount++;
    }

    // 즉시 획득
    public IdObject<T> Possess()
    {
        if (_readyList.Count == 0)
            return default;

        int lastIndex = _readyList.Count - 1;
        IdObject<T> item = _readyList[lastIndex];
        _readyList.RemoveAt(lastIndex);

        int id = item.id;

        if (_inUse.ContainsKey(id))
            throw new InvalidOperationException("중복 ID는 허용되지 않음");

        _inUse.Add(id, item.Value);

        return item;
    }

    // 반환 (트리 탐색 시간)
    public void Release(IdObject<T> item)
    {
        if (item == null)
            return;

        int id = item.id;

        if (_inUse.Remove(id))
        {
            _readyList.Add(item);
        }
        // 없으면 무시 (혹은 예외 처리 가능)
    }
}