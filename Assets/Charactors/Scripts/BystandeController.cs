using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class BystandeController : MonoBehaviour
{
    private Animator m_animator;
    
    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("hit");
        // no need to compare the tag since only player moves
        if (other.transform.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(transform.localScale.y, transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale =
                new Vector3(transform.localScale.y * -1, transform.localScale.y, transform.localScale.z);
        }
        m_animator.SetBool("attack", true);
        Invoke("ResetAnimator", 0.1f);
    }

    private void ResetAnimator()
    {
        m_animator.SetBool("attack", false);
    }
    

}
