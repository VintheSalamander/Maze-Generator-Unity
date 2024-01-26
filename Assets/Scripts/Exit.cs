using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Exit : MonoBehaviour
{
    private static bool hasKey;
    public GameObject helpDialogue;
    private TMP_Text helpText;

    void Start(){
        hasKey = false;
        helpText = helpDialogue.GetComponentInChildren<TMP_Text>();
    }

    private void OnTriggerEnter(Collider other){
        if (other.CompareTag("Player")){
            if (hasKey){
                WinGame();
            }else{
                StartCoroutine(ShowKeyNeeded());
            }
        }
    }

    private IEnumerator ShowKeyNeeded(){
        helpText.text = "Go get the plant!";
        helpDialogue.SetActive(true);
        yield return new WaitForSeconds(2f);
        helpDialogue.SetActive(false);
    }

    private IEnumerator ShowKeyCollected(){
        helpText.text = "EVAAAAA";
        helpDialogue.SetActive(true);
        yield return new WaitForSeconds(2f);
        helpDialogue.SetActive(false);
    }

    private void WinGame(){
        SceneManager.LoadScene("WinMenu");
    }

    public void KeyCollected(){
        hasKey = true;
        StartCoroutine(ShowKeyCollected());
    }
}