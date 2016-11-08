using System;
using System.Collections.Generic;
using UnityEngine;


public class PoolMgr
{
    public class Pool
    {
        int m_NextID = 1;
        int m_MaxSize = 50;
        GameObject m_Prefab;
        Stack<GameObject> m_Stack;

        public Pool(GameObject prefab, int maxsize = 50)
        {
            m_MaxSize = maxsize;
            m_Prefab = prefab;
            m_Stack = new Stack<GameObject>();
        }

        public GameObject Spawn(Vector3 pos, Quaternion rot)
        {
            GameObject obj;
            if (m_Stack.Count == 0)
            {
                obj = (GameObject)GameObject.Instantiate(m_Prefab, pos, rot);
                obj.AddComponent<PoolMember>().myPool = this;
                //obj.name = m_Prefab.name + " (" + (m_NextID++) + ")";
            }
            else
            {
                obj = m_Stack.Pop();
                if (obj == null)
                {
                    Spawn(pos, rot);
                }
            }
            obj.transform.SetParent(m_Prefab.transform.parent);
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            obj.SetActive(true);
            return obj;
        }

        public void Despawn(GameObject go)
        {
            if (m_Stack.Count > m_MaxSize)
            {
                GameObject.Destroy(go);
                return;
            }
            go.SetActive(false);
            m_Stack.Push(go);
        }
    }


    class PoolMember : MonoBehaviour
    {
        public Pool myPool;
    }


    Dictionary<GameObject, Pool> m_Pools;

    public void Init(GameObject prefab, int maxsize = 50)
    {
        if (m_Pools == null) m_Pools = new Dictionary<GameObject, Pool>();
        if (prefab != null && m_Pools.ContainsKey(prefab) == false)
        {
            m_Pools.Add(prefab, new Pool(prefab, maxsize));
        }
    }

    public void Preload(GameObject prefab, int count)
    {

        for (int i = 0; i < count; i++)
        {
            var obj = m_Pools[prefab].Spawn(Vector3.zero, Quaternion.identity);
            Despawn(obj);
        }

    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        Init(prefab);

        return m_Pools[prefab].Spawn(pos, rot);
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        PoolMember pm = obj.GetComponent<PoolMember>();
        if (pm == null)
        {
            Debug.Log("Object '" + obj.name + "' wasn't spawned from a pool. Destroying it instead.");
            GameObject.Destroy(obj);
        }
        else
        {
            pm.myPool.Despawn(obj);
        }
    }

}

