using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

abstract public class UnitAction : NetworkBehaviour {
  public enum ActionType { None, Melee, Ranged, Magic, Support };
  public enum CursorModes { Radial, Line };

  public GameObject visualPrefab;
  public bool used = false;

  public int currentMp;

  [SyncVar]
  public NetworkInstanceId parentNetId;

  void Start(){
    // if (parentNetId != null) { 
    //   GameObject parentObject = ClientScene.FindLocalObject(parentNetId);
    //   transform.SetParent(parentObject.transform.Find("Actions"));
    // }
  }

  public virtual bool VariableMp(){
    return false;
  }

  abstract public ActionType actionType();

  public Unit Unit(){
    return(transform.parent.transform.parent.GetComponent<Unit>());
  }

  public void BeginAction(GameObject targetObject){
    used = true;

    DoAction(targetObject.GetComponent<Cursor>());
  }

  public virtual bool HeightAssisted(){
    return(false);
  }

  public virtual int MpCost(){
    return(0);
  }

  public virtual int MinDistance(){
    return(0);
  }

  public virtual int MaxDistance(){
    return(1);
  }
  
  public virtual int MaxHeightDifference(){
    return(10);
  }

  public virtual int RadialDistance(){
    return(0);
  }

  public virtual int LineDistance(){
    return(0);
  }

  public virtual Helpers.Affinity Affinity(){
    return(Helpers.Affinity.None);
  }

  public virtual bool CanTargetSelf(){
    return(false);
  }

  public virtual bool CanTargetOwnTeam(){
    return(true);
  }

  public virtual bool CanTargetOthers(){
    return(true);
  }

  public virtual bool NeedsLineOfSight(){
    return(false);
  }

  public virtual CursorModes CursorMode()
  {
    return(CursorModes.Radial);
  }

  abstract public string Name();
  abstract public string Description();
  abstract public void ReceiveVisualFeedback(Cursor cursor);

  protected virtual void DoAction(Cursor cursor){
    CreateVisual(cursor, cursor.transform.position);
  }

  protected void CreateVisual(Cursor target, Vector3 visualPosition){
    MainCamera.CenterOnWorldPoint(visualPosition);
    GameObject visualObject = Instantiate(visualPrefab, visualPosition, Quaternion.identity);
    Visual visual = visualObject.transform.Find("Main").GetComponent<Visual>();
    visual.action = this.GetComponent<UnitAction>();
    visual.cursor = target;
  }

  protected void SendDamage(int damage, Unit unit){
    unit.ReceiveDamage(damage, Unit(), this);
  }
}
