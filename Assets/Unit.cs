﻿using System.Collections;
using System.Collections.Generic;
using GridFramework.Grids;
using GridFramework.Renderers.Rectangular;
using GridFramework.Extensions.Align;
using UnityEngine;
using UnityEngine.Networking;

public class Unit: NetworkBehaviour {

  public GameObject[] actions;
  public GameObject[] stances;

  private bool _isMoving;
  private bool _isMovingUp;
  private bool _isMovingDown;
  private float _moveSpeed = 5f;
  private Vector3 _goal;
  private RectGrid _grid;
  private Parallelepiped _renderer;

  [SyncVar]
  public int xPos;

  [SyncVar]
  public int zPos;

  public class CoordinateList : SyncListStruct<CursorController.Coordinate> {};

  public int yPos;
  private bool resetPath = false;

  [SyncVar]
  private CoordinateList _path = new CoordinateList();

  [SyncVar]
  private Color _color;

  public static Unit current;
  public GameObject hitsPrefab;
  public static Unit hovered;

  public int maxHp = 30;

  [SyncVar(hook = "OnChangeHp")]
  public int currentHp = 30;

  public int maxTp = 100;
  private int _pathIndex = 0;
  private bool _canWalkPath = false;

  [SyncVar]
  private int currentTp = 0;

  public int maxMp;
  [SyncVar]
  public int currentMp;

  public string defense = "Free";
  public IStance stance;

  public bool hasActed = false;
  public bool hasMoved = false;

  public float attackModifier = 1;
  public float physicalResistModifier = 1;

  public List<GameObject> buffs = new List<GameObject>();

	// Use this for initialization
	void Start () {
    _grid = GameObject.Find("Grid").GetComponent<RectGrid>();
    _renderer = _grid.gameObject.GetComponent<Parallelepiped>();

    Vector3 position = transform.position;

    yPos = VoxelController.GetElevation(xPos, zPos);

    position.x = xPos + .5f;
    position.z = zPos + .5f;
    position.y = yPos + 1.5f;

    transform.position = position;

    transform.Find("Marker").GetComponent<Renderer>().material.color = Color.white;

    transform.Find("Body").GetComponent<Renderer>().material.color = _color;
    transform.parent = GameObject.Find("Units").transform;
	}

  bool IsMovingAnywhere(){
    return(_isMoving || _isMovingDown || _isMovingUp);
  }

  public int CurrentTp(){
    return(currentTp);
  }

  [Command]
  public void CmdSetTp(int newTp){
    currentTp = newTp;
  }

  public void ReceiveBuff(GameObject buff){
    NetworkServer.Spawn(buff);
    buff.transform.parent = transform.Find("Buffs");
    RpcSyncBuffParent(buff);
    buff.GetComponent<IBuff>().Up(this);
  }

  [ClientRpc]
  public void RpcSyncBuffParent(GameObject buff){
    buff.transform.parent = transform.Find("Buffs");
  }

  public void AdvanceBuffs(){
    foreach(Transform buffTransform in transform.Find("Buffs")){
      IBuff buff = buffTransform.GetComponent<IBuff>();
      if(buff.TurnsLeft() < 1){
        buff.Down();
        Destroy(buffTransform.gameObject);
      }else{
        buff.DeductTurn();
      }
    }
  }

  void FixedUpdate() {
    if (IsMovingAnywhere()) {
      Move();
    } else {
      PickNext();
    }
  }

  [Command]
  public void CmdAddTp(int tpToAdd){
    currentTp += tpToAdd;
  }

  [Command]
  public void CmdSetColor(Color color){
    _color = color;
  }

  public int TpDiff(){
    return(maxTp - currentTp);
  }

  void OnMouseEnter() {
    GameController.ShowProfile(this);
    hovered = this;
    SetHighlight();
  }

  void OnMouseExit() {
    GameController.HideProfile();
    hovered = null;
    UnsetHighlight();
  }

  void SetHighlight(){
    gameObject.transform.Find("Body").GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.red * 100);
    gameObject.transform.Find("Body").GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
  }

  void UnsetHighlight(){
    gameObject.transform.Find("Body").GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
    gameObject.transform.Find("Body").GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
  }

  private void Die(){
    GameController.RemoveUnit(this);
    Destroy(gameObject);
  }

  public int MoveLength(){
    return(10);
    //return(stance.NegotiateMoveLength(10));
  }

  public void ReceiveDamage(int damage){
    currentHp = currentHp - damage;
    //damage = stance.NegotiateDamage(damage);
    damage = 15;
    if(currentHp < 1){
      Die();
    }
  }

  public void OnChangeHp(int amount){
    GameObject hitsObject = Instantiate(hitsPrefab, transform.position, Quaternion.identity);
    hitsObject.GetComponent<Hits>().damage = amount;
  }

  public static void SetCurrent(Unit unit){
    print("setting current unit" + unit);
    if(Unit.current) Unit.current.UnsetMarker();
    Unit.current = unit;
    unit.SetMarker();
    unit.currentMp += 2;
    if(unit.currentMp > unit.maxMp){
      unit.currentMp = unit.maxMp;
    }
  }

  public void SetMarker(){
    transform.Find("Marker").GetComponent<MeshRenderer>().enabled = true;
  }

  public void UnsetMarker(){
    transform.Find("Marker").GetComponent<MeshRenderer>().enabled = false;
  }

  [Command]
  public void CmdSetPath(CursorController.Coordinate[] path){
    currentTp -= 25;
    _path.Clear();
    foreach(CursorController.Coordinate coordinate in path){
      _path.Add(coordinate);
    }
    hasMoved = true;
    RpcRefreshMenuAndFreezeInputs();
  }

  [ClientRpc]
  public void RpcRefreshMenuAndFreezeInputs(){
    _canWalkPath = true;
    GameController.FreezeInputs();
    Menu.Hide();
    Menu.Show();
  }

  [Command]
  public void CmdDoAction(GameObject cursorObject, GameObject actionObject){
    IAction action = actionObject.GetComponent<IAction>();

    currentTp -= action.TpCost();
    currentMp -= action.MpCost();

    action.DoAction(cursorObject.GetComponent<Cursor>());
    hasActed = true;
    GameController.instance.RpcDoActionResponse();
  }

  public bool DoneWithTurn(){
    return(hasActed && hasMoved);
  }

  public void ReadyNextTurn(){
    hasActed = false;
    hasMoved = false;
  }

  private void Move() {
    var t = _moveSpeed * Time.deltaTime;
    var position = transform.position;

    if(_isMovingUp){
      position.y = Mathf.MoveTowards(transform.position.y, _goal.y, t);

      transform.position = position;

      var deltaY = Mathf.Abs(transform.position.y - _goal.y);
      if( deltaY < 0.01f) {
        _isMovingUp = false;
      }
    }else if(_isMoving){
      position.x = Mathf.MoveTowards(transform.position.x, _goal.x, t);
      position.z = Mathf.MoveTowards(transform.position.z, _goal.z, t);

      transform.position = position;

      // Check if we reached the destination (use a certain tolerance so
      // we don't miss the point becase of rounding errors)
      var deltaX = Mathf.Abs(transform.position.x - _goal.x);
      var deltaZ = Mathf.Abs(transform.position.z - _goal.z);
      if( deltaX < 0.01f && deltaZ < 0.01f) {
        _isMoving = false;
      }
    }else if(_isMovingDown){
      position.y = Mathf.MoveTowards(transform.position.y, _goal.y, t);

      transform.position = position;

      var deltaY = Mathf.Abs(transform.position.y - _goal.y);
      if( deltaY < 0.01f) {
        _isMovingDown = false;
      }
    }

    if(!_isMoving && !_isMovingUp && !_isMovingDown && resetPath){
      CursorController.UnsetMovement();
      CursorController.ResetPath();
      resetPath = false;
      GameController.Next();
      GameController.UnfreezeInputs();
    }
  }

  private void PickNext() {
    if(_canWalkPath){
      Vector3 direction;  // Direction to move in (grid-coordinates)

      if(_pathIndex >= _path.Count){
        return;
      }

      CursorController.Coordinate nextStep = _path[_pathIndex];
      _pathIndex++;
      if(_pathIndex >= _path.Count) {
        resetPath = true;
        _canWalkPath = false;
        _pathIndex = 0;
      }

      CursorController.cursorMatrix[xPos][zPos].standingUnit = null;

      int newY = nextStep.elevation - yPos;
      yPos = yPos + newY;

      if (nextStep.x > xPos) {
        direction = new Vector3(1, newY, 0);
        xPos++;
      } else if (nextStep.x < xPos) {
        direction = new Vector3(-1, newY, 0);
        xPos--;
      } else if (nextStep.z > zPos) {
        direction = new Vector3(0, newY, 1);
        zPos++;
      } else if (nextStep.z < zPos) {
        direction = new Vector3(0, newY, -1);
        zPos--;
      } else {
        return;
      }

      CursorController.cursorMatrix[xPos][zPos].standingUnit = this;

      _goal = _grid.WorldToGrid(transform.position) + direction;

      _goal = _grid.GridToWorld(_goal);
      _isMoving = true;

      if(newY > 0) _isMovingUp = true;
      if(newY < 0) _isMovingDown = true;
    }
  }
}
