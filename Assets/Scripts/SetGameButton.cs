using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetGameButton : MonoBehaviour
{
    public enum EButtonType
    {
        NotSet,
        PairNumberBtn,
        PuzzleCategoryBtn,
    };

    [SerializeField] public EButtonType ButtonType = EButtonType.NotSet;
    [HideInInspector] public GameSettings.EPairNumber PairNumber = GameSettings.EPairNumber.NotSet;
    [HideInInspector] public GameSettings.EPuzzleCategories PuzzleCategories = GameSettings.EPuzzleCategories.NotSet;

    public void SetGameOption(string SceneName)
    {
        var comp = gameObject.GetComponent<SetGameButton>();

        switch(comp.ButtonType)
        {
            case EButtonType.PairNumberBtn:
                GameSettings.Instance.SetPairNumber(comp.PairNumber);
                break;

            case EButtonType.PuzzleCategoryBtn:
                GameSettings.Instance.SetPuzzleCategories(comp.PuzzleCategories);
                break;
        }
        
        if (GameSettings.Instance.AllSettingsReady())
        {
            SceneManager.LoadScene(SceneName);
        }

    }
}
