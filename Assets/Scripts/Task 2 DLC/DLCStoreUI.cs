using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DLCStoreUI : MonoBehaviour
{
    [SerializeField] private Button X;
    [SerializeField] private GameObject DLCUI;
    [SerializeField] private GameObject Overlay;

    private void Awake()
    {
       
        X.onClick.AddListener(() =>
        {
            DLCUI.SetActive(false);
            Overlay.SetActive(true);

        });
    }
}
