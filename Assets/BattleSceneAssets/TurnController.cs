﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TurnController : NetworkBehaviour {

  public static TurnController instance;

  void Awake(){
    instance = this;
  }

  [Command]
  public void CmdAdvanceTp(){
    List<Unit> sudoUnits = Unit.All();
    sudoUnits.Sort((a, b) => a.TpDiff().CompareTo(b.TpDiff()));
    int difference = sudoUnits[0].TpDiff();
    foreach(Unit unit in sudoUnits){
      unit.CmdAddTp(difference);
    }
  }

  [Command]
  public void CmdAdvanceTpToNext(){
    CmdAdvanceTp();
    CmdSetCurrentUnit();
  }

  [Command]
  public void CmdSetCurrentUnit(){
    List<GameObject> units = new List<GameObject>();
    foreach(Transform unitObject in GameObject.Find("Units").transform){
      units.Add(unitObject.gameObject);
    }
    units.Sort((a, b) => a.GetComponent<Unit>().TpDiff().CompareTo(b.GetComponent<Unit>().TpDiff()));
    Unit unit = units[0].GetComponent<Unit>();
    unit.CmdSetCurrent();

    unit.currentMp += 2;
    if(unit.currentMp > unit.maxMp){
      unit.currentMp = unit.maxMp;
    }
  }

  public static void Next() {
    if(GameController.gameFinished){
      GameController.EndGame();
    }else{
      if(Unit.current.DoneWithTurn()){
        Unit.current.ReadyNextTurn();
        CursorController.moveEnabled = true;
        if(NetworkServer.active) instance.CmdAdvanceTpToNext();
      }
    }
  }
}
