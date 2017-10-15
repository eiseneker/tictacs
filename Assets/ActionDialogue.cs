﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionDialogue : MonoBehaviour {

  private Text text;
  public IAction action;
  private float lifetime = 0;
  public System.Action whenDone;

	// Use this for initialization
	void Start () {
    text = GetComponent<Text>();
    text.text = action.Name().ToString();
    transform.parent = GameObject.Find("Popups").transform;
    transform.position = GameObject.Find("Main Camera").GetComponent<Camera>().WorldToScreenPoint(transform.position);
	}
	
	// Update is called once per frame
	void Update () {
    lifetime = lifetime + Time.deltaTime;
    if(lifetime > 1) {
      whenDone();
      Destroy(gameObject);
    };
	}
}
