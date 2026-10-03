using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

public class HighscoreTable : MonoBehaviour
{
    private Transform entryContainer;
    private Transform entryTemplate;
    private List<HighscoreEntry> highscoreEntryList;
    private List<Transform> highscoreEntryTransformList;
    private int no1 = 0;
    private int no2 = 0;
    private int no3 = 0;
    private int no4 = 0;
    private int no5 = 0;
    private int now = 0;
    private void Awake()
    {
        entryContainer = transform.Find("highscoreEntryContainer");
        entryTemplate = entryContainer.Find("highscoreEntryTemplate");
        entryTemplate.gameObject.SetActive(false);
        now = PlayerPrefs.GetInt("HighScore");
        no1 = PlayerPrefs.GetInt("No1Score");
        no2 = PlayerPrefs.GetInt("No2Score");
        no3 = PlayerPrefs.GetInt("No3Score");
        no4 = PlayerPrefs.GetInt("No4Score");
        no5 = PlayerPrefs.GetInt("No5Score");
        PlayerPrefs.SetInt("HighScore", 0);
        if (now > no1)
        {
            PlayerPrefs.SetInt("No1Score", now);
            PlayerPrefs.SetInt("No2Score", no1);
            PlayerPrefs.SetInt("No3Score", no2);
            PlayerPrefs.SetInt("No4Score", no3);
            PlayerPrefs.SetInt("No5Score", no4);
            PlayerPrefs.Save();
            highscoreEntryList = new List<HighscoreEntry>() {
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No1Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No2Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No3Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No4Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No5Score")},
    };
        }
        else if (now > no2)
        {
            PlayerPrefs.SetInt("No1Score", no1);
            PlayerPrefs.SetInt("No2Score", now);
            PlayerPrefs.SetInt("No3Score", no2);
            PlayerPrefs.SetInt("No4Score", no3);
            PlayerPrefs.SetInt("No5Score", no4);
            PlayerPrefs.Save();
            highscoreEntryList = new List<HighscoreEntry>() {
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No1Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No2Score") },
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No3Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No4Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No5Score")},
    };
        }
        else if (now > no3)
        {
            PlayerPrefs.SetInt("No1Score", no1);
            PlayerPrefs.SetInt("No2Score", no2);
            PlayerPrefs.SetInt("No3Score", now);
            PlayerPrefs.SetInt("No4Score", no3);
            PlayerPrefs.SetInt("No5Score", no4);
            PlayerPrefs.Save();
            highscoreEntryList = new List<HighscoreEntry>() {
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No1Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No2Score") },
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No3Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No4Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No5Score")},
        };
        }
        else if (now > no4)
        {
            PlayerPrefs.SetInt("No1Score", no1);
            PlayerPrefs.SetInt("No2Score", no2);
            PlayerPrefs.SetInt("No3Score", no3);
            PlayerPrefs.SetInt("No4Score", now);
            PlayerPrefs.SetInt("No5Score", no4);
            PlayerPrefs.Save();
            highscoreEntryList = new List<HighscoreEntry>() {
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No1Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No2Score") },
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No3Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No4Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No5Score")},
        };
        }
        else if (now > no5)
        {
            PlayerPrefs.SetInt("No1Score", no1);
            PlayerPrefs.SetInt("No2Score", no2);
            PlayerPrefs.SetInt("No3Score", no3);
            PlayerPrefs.SetInt("No4Score", no4);
            PlayerPrefs.SetInt("No5Score", now);
            PlayerPrefs.Save();
            highscoreEntryList = new List<HighscoreEntry>() {
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No1Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No2Score") },
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No3Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No4Score")},
        new HighscoreEntry{score = PlayerPrefs.GetInt ("No5Score")},
        };
        }
        else
        {
            highscoreEntryList = new List<HighscoreEntry>() {
        new HighscoreEntry{ score = PlayerPrefs.GetInt ("No1Score") },
        new HighscoreEntry{ score = PlayerPrefs.GetInt ("No2Score") },
        new HighscoreEntry{ score = PlayerPrefs.GetInt ("No3Score") },
        new HighscoreEntry{ score = PlayerPrefs.GetInt ("No4Score") },
        new HighscoreEntry{ score = PlayerPrefs.GetInt ("No5Score") },
    };
        }
        for (int i = 0; i < highscoreEntryList.Count; i++)
        {
            for (int j = i + 1; j < highscoreEntryList.Count; j++)
            {
                if (highscoreEntryList[j].score > highscoreEntryList[i].score)
                {
                    HighscoreEntry tmp = highscoreEntryList[i];
                    highscoreEntryList[i] = highscoreEntryList[j];
                    highscoreEntryList[j] = tmp;
                }
            }
        }
        highscoreEntryTransformList = new List<Transform>();
        foreach (HighscoreEntry highscoreEntry in highscoreEntryList)
        {
            CreateHighscoreEntryTransform
        (highscoreEntry, entryContainer, highscoreEntryTransformList);
        }
        Highscores highscores = new Highscores
        {
            highscoreEntryList =
       highscoreEntryList
        };
        string json = JsonUtility.ToJson(highscores);
        PlayerPrefs.SetString("HighscoreTable", json);
        PlayerPrefs.Save();
    }
    private void CreateHighscoreEntryTransform(HighscoreEntry highscoreEntry,
   Transform container, List<Transform> transformList)
    {
        float tempateHeight = -30f;
        Transform entryTransform = Instantiate(entryTemplate, container);
        RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>
       ();
        entryRectTransform.anchoredPosition = new Vector2(0,
       tempateHeight * transformList.Count);
        entryTransform.gameObject.SetActive(true);
        int rank = transformList.Count + 1;
        if (rank <= 5)
        {
            string rankString;
            switch (rank)
            {
                default:
                    rankString = rank + "TH"; break;
                case 1: rankString = "1ST"; break;
                case 2: rankString = "2ND"; break;
                case 3: rankString = "3RD"; break;
            }
            entryTransform.Find("posText").GetComponent<TMP_Text>().text =
        rankString;
            int score = highscoreEntry.score;
            entryTransform.Find("scoreText").GetComponent<TMP_Text>().text =
        score.ToString();
            transformList.Add(entryTransform);
        }
    }
    private void AddHighscoreEntry(int score)
    {
        HighscoreEntry highscoreEntry = new HighscoreEntry { score = score };
        string jsonString = PlayerPrefs.GetString("HighscoreTable");
        Highscores highscores = JsonUtility.FromJson<Highscores>(jsonString);
        highscores.highscoreEntryList.Add(highscoreEntry);
        string json = JsonUtility.ToJson(highscores);
        PlayerPrefs.SetString("HighscoreTable", json);
        PlayerPrefs.Save();
    }
    private class Highscores
    {
        public List<HighscoreEntry> highscoreEntryList;
    }
    [System.Serializable]
    private class HighscoreEntry
    {
        public int score;
    }
}