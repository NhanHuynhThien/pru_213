using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public enum ResourceType { Copper, Tin, Bronze, TurtleShell }

    [Header("Resource Info")]
    public ResourceType type = ResourceType.Copper;
    public int amount = 5;
    public float respawnTime = 10f;

    [Header("Visuals")]
    public GameObject nodeModel;
    public ParticleSystem collectEffect;
    public GameObject depletedModel;
    public float rotateSpeed = 20f;

    private bool isDepleted = false;

    void Update()
    {
        if (!isDepleted && nodeModel != null)
        {
            nodeModel.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDepleted) return;
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) pc = FindAnyObjectByType<PlayerController>();
        if (pc == null || pc.stats == null) return;

        int amountGained = amount;

        switch (type)
        {
            case ResourceType.Copper:
                pc.stats.copperCount += amountGained;
                break;
            case ResourceType.Tin:
                pc.stats.tinCount += amountGained;
                break;
            case ResourceType.Bronze:
                pc.stats.bronzeIngot += amountGained;
                break;
            case ResourceType.TurtleShell:
                pc.stats.turtleShell += amountGained;
                break;
        }

        AudioManager.Instance?.PlayPickupSound();
        ParticleManager.Instance?.PlayHealEffect(transform);

        StartCoroutine(CollectSequence());
    }

    System.Collections.IEnumerator CollectSequence()
    {
        isDepleted = true;

        if (collectEffect != null)
        {
            collectEffect.Play();
        }

        if (nodeModel != null)
            nodeModel.SetActive(false);
        if (depletedModel != null)
            depletedModel.SetActive(true);

        Debug.Log($"[ResourceNode] Da thu thap {amount} {type}!");

        yield return new WaitForSeconds(respawnTime);

        if (nodeModel != null)
            nodeModel.SetActive(true);
        if (depletedModel != null)
            depletedModel.SetActive(false);

        isDepleted = false;
    }
}
