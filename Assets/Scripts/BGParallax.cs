using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGParallax : MonoBehaviour
{
    [Header("=== References ===")]
    public Camera cam;

    [Header("=== Settings ===")]
    public float parallaxEffect;

    private float m_length, m_startPos;

    // Start is called before the first frame update
    void Start() {
        m_startPos = transform.position.x;
        m_length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Update is called once per frame
    void Update() {
        if (cam == null) return;
        float temp = cam.transform.position.x * (1-parallaxEffect);
        float dist = cam.transform.position.x * parallaxEffect;
        transform.position = new Vector3(m_startPos + dist, transform.position.y, transform.position.z);
        if (temp > m_startPos + m_length) m_startPos += m_length;
        else if (temp < m_startPos - m_length) m_startPos -= m_length;
    }
}
