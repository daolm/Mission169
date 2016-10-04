﻿using Slug.StateMachine;
using UnityEngine;

public class SoldierRunAwayBehaviour : Room {

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        brain.TurnBackToTarget();
    }

	override public void Update() {
        brain.Walk(1.6f);
	}

}
