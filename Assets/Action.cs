using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Action : MonoBehaviour {
  public Unit Unit(){
    return(transform.parent.transform.parent.GetComponent<Unit>());
  }
}
