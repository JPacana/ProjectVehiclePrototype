using UnityEngine;

public class VehicleAttachment : MonoBehaviour
{
    [SerializeField] private bool _terminal;
    public bool Terminal => _terminal;
}
