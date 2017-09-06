using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {
    LineRenderer m_lineRenderer;

    public Transform StartTransform;
    public Transform EndTransform;
    public int Recurse = 6;
    public float Offset = 1;
    public Vector3 StartPos;
    public Vector3 EndPos;

    private int m_startIndex;
    private List<KeyValuePair<Vector3, Vector3>> m_linePosList = new List<KeyValuePair<Vector3, Vector3>>();
    // Use this for initialization
    void Start () {
        m_lineRenderer = GetComponent<LineRenderer>();

    }

    void Triggle()
    {
        Vector3 start = StartPos, end = EndPos;
        if (StartTransform != null)
        {
            start += StartTransform.position;
        }
        if (EndTransform != null)
        {
            end += EndTransform.position;
        }

        GenLinePos(start, end, Recurse, Offset);
        UpdateLightning();
    }


    void UpdateLightning()
    {
        if (m_linePosList.Count == 0) return; 

        int lineIdx = 0;
        m_lineRenderer.positionCount = m_linePosList.Count - m_startIndex + 1;

        m_lineRenderer.SetPosition(lineIdx++, m_linePosList[0].Key);

        for (int i = m_startIndex; i < m_linePosList.Count; ++i)
        {
            var p = m_linePosList[i];
            m_lineRenderer.SetPosition(lineIdx++, p.Value);
        }

        m_linePosList.Clear();
    }

    void GenLinePos(Vector3 start, Vector3 end, int recurse, float offset)
    {
        m_linePosList.Add(new KeyValuePair<Vector3, Vector3>(start, end));
        m_startIndex = 0;
        while (recurse -- > 0)
        {
            int preStartIndex = m_startIndex;
            m_startIndex = m_linePosList.Count;
            for (int i = preStartIndex, size = m_linePosList.Count; i < size; ++i)
            {
                Vector3 p1 = m_linePosList[i].Key;
                Vector3 p2 = m_linePosList[i].Value;

                Vector3 mid = (p1 + p2) * 0.5f;
                Vector3 dir;
                RandomVector(ref p1, ref p2, offset, out dir);

                mid += dir;

                m_linePosList.Add(new KeyValuePair<Vector3, Vector3>(p1, mid));
                m_linePosList.Add(new KeyValuePair<Vector3, Vector3>(mid, p2));
            }
            offset *= 0.5f;
        }
    }

    void RandomVector(ref Vector3 start, ref Vector3 end, float offset, out Vector3 vec)
    {
        //Vector3 directionNormalized = (end - start).normalized;
        //Vector3 side = new Vector3(-directionNormalized.y, directionNormalized.x, directionNormalized.z);
        Vector3 side = new Vector3(Random.value, Random.value, Random.value).normalized;
        float distance = (2*Random.value - 1.0f) * offset;
        vec = side * distance;

    }
	
	// Update is called once per frame
	void Update () {
        Triggle();
    }
}
