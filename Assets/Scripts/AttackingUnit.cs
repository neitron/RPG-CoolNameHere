using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackingUnit : Unit
{
    private void Awake()
    {
        speed = 2;
        attack = 5;
        armor = 1;
    }
}
