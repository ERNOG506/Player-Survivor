using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    public float speed = 2f;

    private Transform player;
    private EnemyController controller;

    private void Start()
    {
        controller = GetComponent<EnemyController>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (controller != null || player == null)
        {
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }
}
