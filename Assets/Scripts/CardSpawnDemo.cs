using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSpawnDemo : MonoBehaviour
{
    [SerializeField] private List<RectTransform> cards;
    [SerializeField]  private CardHandLayout _layout;

    public void Start()
    {
        foreach (var c in cards)
        {
            RectTransform a = Instantiate(c);
            _layout.AddCard(a);
        }
    }
}
