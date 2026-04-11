using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System;

public class GuiltyPoolItem<T>
{
    public int id;
    public T Value;
}

public class GuiltyPool<T>
{
    int elementCount = 0;

    // �غ�� ��ü
    private readonly List<GuiltyPoolItem<T>> _readyList;

    // ��� �� ��ü (ID ���� ����)
    private readonly SortedDictionary<int, T> _inUse
        = new SortedDictionary<int, T>();

    public GuiltyPool()
    {
        _readyList = new List<GuiltyPoolItem<T>>();
    }

    public GuiltyPool(List<T> initialItems)
    {
        _readyList = new List<GuiltyPoolItem<T>>();

        for (int i = 0; i < initialItems.Count; ++i)
        {
            _readyList.Add(new GuiltyPoolItem<T> { id = i, Value = initialItems[i] });
        }
        elementCount = initialItems.Count;
    }

    // �ʱ� �߰�
    public void Add(T item)
    {
        _readyList.Add(new GuiltyPoolItem<T>() { id = elementCount, Value = item });
        elementCount++;
    }
    // �ʱ� �߰�
    public void Add(T item, out GuiltyPoolItem<T> self)
    {
        self = new GuiltyPoolItem<T>() { id = elementCount, Value = item };
        _readyList.Add(self);
        elementCount++;
    }

    // ��� ȹ��
    public GuiltyPoolItem<T> Possess()
    {
        if (_readyList.Count == 0)
            return default;

        int lastIndex = _readyList.Count - 1;
        GuiltyPoolItem<T> item = _readyList[lastIndex];
        _readyList.RemoveAt(lastIndex);

        int id = item.id;

        if (_inUse.ContainsKey(id))
            throw new InvalidOperationException("�ߺ� ID�� ������ ����");

        _inUse.Add(id, item.Value);

        return item;
    }

    // ��ȯ (Ʈ�� Ž�� �ð�)
    public void Release(GuiltyPoolItem<T> item)
    {
        if (item == null)
            return;

        int id = item.id;

        if (_inUse.Remove(id))
        {
            _readyList.Add(item);
        }
        // ������ ���� (Ȥ�� ���� ó�� ����)
    }
}