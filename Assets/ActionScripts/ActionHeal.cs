﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class ActionHeal : UnitAction
{
    private int targetsToResolve = 0;
    private List<Unit> unitsAffected = new List<Unit>();

    public override string Name()
    {
        return ("Heal");
    }

    public override string Description()
    {
        return ("Heals targetm restoring 25% of their max HP.");
    }

    public override int MaxDistance()
    {
        return (4);
    }

    public override int MpCost(){
        return(2);
    }

    public override void ReceiveVisualFeedback(Cursor cursor)
    {
        if (cursor.standingUnit)
        {
            targetsToResolve--;
            cursor.standingUnit.HealDamage(Mathf.RoundToInt(cursor.standingUnit.maxHp/4));

            List<Cursor> tiles = Helpers.GetRadialTiles(cursor.xPos, cursor.zPos, 2, 10, true);
            foreach (Cursor tile in tiles)
            {
                if (tile.standingUnit && !unitsAffected.Contains(tile.standingUnit))
                {
                    unitsAffected.Add(tile.standingUnit);
                    targetsToResolve++;
                    StartCoroutine(DoScript(tile));
                }
            }
        }

        if(targetsToResolve == 0) Unit().FinishAction();
    }

    protected override void DoAction(Cursor cursor){
        if(cursor.standingUnit) {
            targetsToResolve++;
            unitsAffected.Add(cursor.standingUnit);
        }

        CreateVisual(cursor, cursor.transform.position);
    }

    private IEnumerator DoScript(Cursor tile){
        CreateVisual(tile, tile.transform.position);
        yield return new WaitForSeconds(1f);
    }

    public override ActionType actionType(){
        return ActionType.Magic;
    }

    public override Helpers.Affinity Affinity(){
        return(Helpers.Affinity.Water);
    }
}
