﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour {

  public GameObject gameControllerPrefab;

	// Use this for initialization
	void Start () {
    if(isServer && isLocalPlayer){
      CmdSpawnGameController();
    }
	}
	
	// Update is called once per frame
	void Update () {
    if(!isLocalPlayer){
      return;
    }

    if(InputController.InputConfirm()){
      if (CursorController.moveEnabled && GameController.state == GameController.State.PickAction && Cursor.hovered){
        if(CursorController.selected && CursorController.selected == Cursor.hovered){
          CmdMoveAlong(CursorController.selected.xPos, CursorController.selected.zPos);
        }else if(!Cursor.hovered.standingUnit && Cursor.hovered.movable){
          CursorController.instance.ShowPath();
        }
      }
    }
	}

  [Command]
  public void CmdMoveAlong(int x, int z) {
    CursorController.instance.CmdMoveAlong(x, z);
  }

  [Command]
  void CmdSpawnGameController(){
    GameObject gameController = Instantiate(gameControllerPrefab, Vector3.zero, Quaternion.identity);
    NetworkServer.Spawn(gameController);
  }

  void Confirm () {
  }

}
