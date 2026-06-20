using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class resetAnim : MonoBehaviour
{
    public void ResetAnim()
    {
        GetComponent<Animator>().SetBool("isSlash", false);
    }
}
