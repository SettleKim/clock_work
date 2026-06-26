using UnityEngine;

public class MapDoor : MonoBehaviour
{
    [SerializeField] float interactionRadius = 2f;
    [SerializeField] Vector2 destinationPosition;
    [SerializeField] bool setRespawnPointOnEnter = true;
    [SerializeField] bool useMapTransition;
    [SerializeField] UndergroundMapBootstrap.MapLayout targetMap = UndergroundMapBootstrap.MapLayout.Story;
    [SerializeField] Vector2 targetMapSpawnPosition;

    public Vector2 DestinationPosition => destinationPosition;

    public void Configure(Vector2 destination)
    {
        useMapTransition = false;
        destinationPosition = destination;
    }

    public void ConfigureMapTransition(UndergroundMapBootstrap.MapLayout map, Vector2 spawnPosition)
    {
        useMapTransition = true;
        targetMap = map;
        targetMapSpawnPosition = spawnPosition;
    }

    public bool IsPlayerInRange(Transform player)
    {
        if (player == null)
            return false;

        return Vector2.Distance(transform.position, player.position) <= interactionRadius;
    }

    public void Interact(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        if (useMapTransition)
        {
            UndergroundMapBootstrap bootstrap = FindFirstObjectByType<UndergroundMapBootstrap>();
            if (bootstrap != null)
            {
                bootstrap.SwitchMap(targetMap, targetMapSpawnPosition, setRespawnPointOnEnter);
                return;
            }
        }

        Transform playerTransform = playerObject.transform;
        playerTransform.position = destinationPosition;

        Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (!setRespawnPointOnEnter)
            return;

        PlayerRespawn respawn = playerObject.GetComponent<PlayerRespawn>();
        if (respawn != null)
            respawn.SetSpawnPosition(destinationPosition);
    }
}
