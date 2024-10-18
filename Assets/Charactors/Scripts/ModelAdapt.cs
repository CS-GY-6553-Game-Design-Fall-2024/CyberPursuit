using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAdapt : MonoBehaviour
{
    private Movement movement;
    private PolygonCollider2D slideCollider;
    private CapsuleCollider2D standCollider;
    private Vector2 standPosition = Vector2.zero;
    private Vector2 slidePosition = new Vector2(0, -0.394f);
    
    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<Movement>();
        slideCollider = GetComponent<PolygonCollider2D>();
        standCollider = GetComponent<CapsuleCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (movement.isSliding && !slideCollider.enabled)
        {
            slideCollider.enabled = true;
            standCollider.enabled = false;
            // transform.position = slidePosition;
        }
        else if (!movement.isSliding && slideCollider.enabled)
        {
            standCollider.enabled = true;
            slideCollider.enabled = false;
            // transform.position = standPosition;
        }
    }
}
