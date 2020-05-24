using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmoredUnit : Unit
{
    private void Awake()
    {
        speed = 2;
        attack = 2;
        armor = 4;
    }
}
