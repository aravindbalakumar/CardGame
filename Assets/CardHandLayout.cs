using System.Collections.Generic;
using UnityEngine;

public class CardHandLayout : MonoBehaviour
{
    [Header("Card Layout")]
    [SerializeField] private float cardSpacing = 110f;
    [SerializeField] private float verticalCurve = 35f;
    [SerializeField] private float maxRotation = 8f;

    [Header("Position")]
    [SerializeField] private float handYOffset = 0f;

    private List<RectTransform> cardTransforms = new List<RectTransform>();

    public void AddCard(RectTransform card)
    {
        if (!cardTransforms.Contains(card))
        {
            cardTransforms.Add(card);
            card.SetParent(transform, false);
        }

        ArrangeCards();
    }

    public void RemoveCard(RectTransform card)
    {
        if (cardTransforms.Contains(card))
        {
            cardTransforms.Remove(card);
        }

        ArrangeCards();
    }

    public void ArrangeCards()
    {
        int count = cardTransforms.Count;

        if (count == 0)
            return;

        float center = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cardTransforms[i];

            // Distance from center
            float offset = i - center;

            // Horizontal position
            float x = offset * cardSpacing;

            // Create a slight curved hand
            float normalized = count == 1
                ? 0f
                : offset / center;

            float y = -Mathf.Abs(normalized) * verticalCurve;

            // Rotation
            float rotation = normalized * -maxRotation;

            card.anchoredPosition = new Vector2(
                x,
                y + handYOffset
            );

            card.localRotation = Quaternion.Euler(
                0f,
                0f,
                rotation
            );

            // Make sure cards overlap in the correct order
            card.SetSiblingIndex(i);
        }
    }
}