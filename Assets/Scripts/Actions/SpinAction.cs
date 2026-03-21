using System;
using UnityEngine;

public class SpinAction : BaseAction
{
    private float totalSpinAmount;

    private void Update()
    {
        if (!isActive) return;
        
        float speedAddAmount = 360 * Time.deltaTime;
        transform.eulerAngles += new Vector3(0, speedAddAmount, 0);
        
        totalSpinAmount += speedAddAmount;
        if (totalSpinAmount >= 360) isActive = false;
        
    }

    public void Spin()
    {
        isActive = true;
    }
}
