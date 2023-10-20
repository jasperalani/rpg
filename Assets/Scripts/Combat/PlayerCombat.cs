using System.Collections;
using System.Collections.Generic;
using RPG.Player;
using UnityEngine;

namespace RPG.Combat
{
    public class PlayerCombat : MonoBehaviour
    {

        public float autoAttackRange = 10;
        PlayerController playerControl;

        // Start is called before the first frame update
        void Start()
        {
            playerControl = GetComponent<PlayerController>();
            autoAttackRange = 10;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SelectEnemy(EnemyTarget target)
        {
            print("EnemySelected");
        }

        public void AutoAttack(EnemyTarget target)
        {
            print("Start auto attacking if we are in range");

            bool isInRange = Vector3.Distance(this.transform.position, playerControl.selectedEnemy.transform.position) < autoAttackRange;

            if (playerControl.selectedEnemy != null && !isInRange)
            {
                // Selected but not in range
                print("Not in range to auto attack");
                // TODO: Make animation for auto attack (combat stance)
            } else if (playerControl.selectedEnemy != null && isInRange)
            {
                print("In range to auto attack");
                // TODO: Make animation for auto attack (combat damage)

            }
        }

    }
}


