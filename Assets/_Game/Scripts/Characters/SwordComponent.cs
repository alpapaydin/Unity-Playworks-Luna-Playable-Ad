using System.Collections.Generic;
using UnityEngine;

public class SwordRotationSystem : MonoBehaviour
{
    [Header("Sword Settings")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private float rotationRadius = 1.5f;
    [SerializeField] private float rotationSpeed = 180f;

    private Character owner;
    private List<Sword> activeSwords = new List<Sword>();
    private Transform swordParent;
    private Vector3 lastPosition;
    private float deltaAngle;
    private readonly List<Sword> swordsToRemove = new List<Sword>();

    private void Awake()
    {
        owner = GetComponent<Character>();
        swordParent = new GameObject("RotatingSwords").transform;
        swordParent.SetParent(transform);
        swordParent.localPosition = new Vector3(0, 0, 1);
        lastPosition = transform.position;
    }

    public void UpdateSwordCount(int newCount)
    {
        while (activeSwords.Count > newCount)
        {
            int lastIndex = activeSwords.Count - 1;
            if (activeSwords[lastIndex] != null)
            {
                Destroy(activeSwords[lastIndex].gameObject);
            }
            activeSwords.RemoveAt(lastIndex);
        }
        if (activeSwords.Count < newCount)
        {
            int swordsToAdd = newCount - activeSwords.Count;
            for (int i = 0; i < swordsToAdd; i++)
            {
                CreateSword();
            }
            ArrangeSwordsEvenly();
        }
        RemoveNullSwords();
    }

    public void RemoveSpecificSword(Sword swordToRemove)
    {
        if (swordToRemove != null && activeSwords.Remove(swordToRemove))
        {
            owner?.RemoveSwords(1);
        }
    }

    private void CreateSword()
    {
        GameObject swordObj = Instantiate(swordPrefab, Vector3.zero, Quaternion.identity, swordParent);
        Sword sword = swordObj.GetComponent<Sword>();
        sword.gameSingleton = owner.gameSingleton;
        sword.owner = owner;
        activeSwords.Add(sword);
    }

    private void ArrangeSwordsEvenly()
    {
        RemoveNullSwords();
        int totalSwords = activeSwords.Count;
        if (totalSwords == 0) return;

        float angleStep = 360f / totalSwords;
        for (int i = 0; i < totalSwords; i++)
        {
            activeSwords[i]?.Initialize(rotationRadius, rotationSpeed, i * angleStep);
        }
    }

    private void Update()
    {
        if (activeSwords.Count == 0) return;

        Vector3 currentPosition = transform.position;
        if (currentPosition != lastPosition)
        {
            lastPosition = currentPosition;
        }

        deltaAngle = rotationSpeed * Time.deltaTime;

        RemoveNullSwords();
        UpdateSwordPositions();
    }

    private void UpdateSwordPositions()
    {
        for (int i = 0; i < activeSwords.Count; i++)
        {
            Sword sword = activeSwords[i];
            if (sword != null)
            {
                sword.currentAngle += deltaAngle;
                sword.UpdatePosition(lastPosition);
            }
        }
    }

    private void RemoveNullSwords()
    {
        activeSwords.RemoveAll(sword => sword == null);
    }

}