using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RadialProgress : MonoBehaviour
{
    public GameObject LoadingText;
    public TMP_Text ProgressIndicator;
    public Image LoadingBar;
    float currentValue;
    public float speed;

    void Update()
    {
        if (currentValue < 100)
        {
            currentValue += speed * Time.deltaTime;
            ProgressIndicator.text = ((int)currentValue).ToString() + "%";
            LoadingText.SetActive(true);
        }
        else
        {
            LoadingText.SetActive(false);
            Application.LoadLevel("PlayScene");
        }
        LoadingBar.fillAmount = currentValue / 100;
    }
}