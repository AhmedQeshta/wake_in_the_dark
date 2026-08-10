using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField]
    private float verticalOffset = 0.2f;

    public Vector3 GetRespawnPosition()
    {
        return transform.position +
               Vector3.up * verticalOffset;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            0.15f
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            Vector3.up * 0.5f
        );
    }
}