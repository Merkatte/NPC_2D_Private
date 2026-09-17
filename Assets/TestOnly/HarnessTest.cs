using UnityEngine;

[DisallowMultipleComponent]
public sealed class HarnessTest : MonoBehaviour
{
    private const string SuccessMessage = "HarnessSuccess";

    private void Start()
    {
        Debug.Log(SuccessMessage, this);
    }
}
