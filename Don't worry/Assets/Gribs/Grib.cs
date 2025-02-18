using UnityEngine;
public class Grib : MonoBehaviour
{
    public string Name;
    public bool isPoisonous;
    [SerializeField] MeshRenderer border;
    public void ChangeBoard(bool isActive)
    {
        border.enabled = isActive;
    }
}
