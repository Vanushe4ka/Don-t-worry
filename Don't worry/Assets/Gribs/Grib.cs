using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grib : MonoBehaviour
{
    // Start is called before the first frame update
    public string Name;
    public bool isPoisonous;
    [SerializeField] MeshRenderer border;
    public void ChangeBoard(bool isActive)
    {
        border.enabled = isActive;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
