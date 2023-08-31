using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PictureManager : MonoBehaviour
{
    public Picture PicturePrefab;
    public Transform PicSpawnPosition;
    public Vector2 StartPos = new Vector2(-2.15f, 3.62f);

    [Space]
    [Header("End Game Screen")]
    public GameObject EndGamePanel;
    public GameObject NewBestScoreText;
    public GameObject YourScoreText;
    public GameObject EndTimeText;

    public enum GameState
    {
        NoAction,
        MovingOnPositions,
        DeletingPuzzles,
        Flipback,
        Checking,
        GameEnd
    };

    public enum PuzzleState
    {
        PuzzleRotating,
        CanRotate
    };

    public enum RevealedState
    {
        NoRevealed,
        OneRevealed,
        TwoRevealed
    };

    [HideInInspector] public GameState CurrentGameState;
    [HideInInspector] public PuzzleState CurrentPuzzleState;
    [HideInInspector] public RevealedState PuzzleRevealedNumber;

    [HideInInspector] public List<Picture> PictureList;

    private Vector2 _offsetFor10Pairs = new Vector2(1.4f, 1.42f);
    private Vector2 _offsetFor15Pairs = new Vector2(1.08f, 1.22f);
    private Vector2 _offsetFor20Pairs = new Vector2(1.08f, 1.0f);

    private Vector3 _newScaleDown = new Vector3(0.9f, 0.9f, 0.001f);

    private List<Material> _materialList = new List<Material>();
    private List<string> _textTurePathList = new List<string>();
    private Material _firstMaterial;
    private string _firstTexturePath;

    private int _firstRevealedPic;
    private int _secondRevealedPic;
    private int _revealedPicNumber = 0;
    private int _pictureToDestroy1;
    private int _pictureToDestroy2;

    private bool _coroutineStarted = false;

    private int _pairNumbers;
    private int _removedPairs;
    private Timer _gameTimer;

    private void Start()
    {
        CurrentGameState = GameState.NoAction;
        CurrentPuzzleState = PuzzleState.CanRotate;
        PuzzleRevealedNumber = RevealedState.NoRevealed;
        _revealedPicNumber = 0;
        _firstRevealedPic = _secondRevealedPic = -1;

        _gameTimer = GameObject.Find("Main Camera").GetComponent<Timer>();

        _removedPairs = 0;
        _pairNumbers = (int)GameSettings.Instance.GetPairNumber();

        LoadMaterials();

        GameSettings.EPairNumber pairNumber = GameSettings.Instance.GetPairNumber();
        int rows, cols;
        Vector2 _offset;
        bool scaleDown = false;

        if (pairNumber == GameSettings.EPairNumber.E10Pairs)
        {
            rows = 4;
            cols = 5;
            _offset = _offsetFor10Pairs;
        } else if (pairNumber == GameSettings.EPairNumber.E15Pairs)
        {
            rows = 5;
            cols = 6;
            _offset = _offsetFor15Pairs;
        } else {
            rows = 5;
            cols = 8;
            _offset = _offsetFor20Pairs;
            scaleDown = true;
        }
        SpawnPictureMesh(rows, cols, StartPos, _offset, scaleDown);
        MovePicture(rows, cols, StartPos, _offset);
        CurrentGameState = GameState.MovingOnPositions;
    }

    private void LoadMaterials()
    {
        var materialFilePath = GameSettings.Instance.GetMaterialDirectoryName();
        var textureFilePath = GameSettings.Instance.GetPuzzleCategoryTextureDirectoryName();
        var pairNumber = (int)GameSettings.Instance.GetPairNumber();

        const string matBase = "Pic";
        var firstMateralName = "Back";

        for (int index = 1; index <= pairNumber; index++)
        {
            var currentFilePath = materialFilePath + matBase + index;
            Material mat = Resources.Load(currentFilePath, typeof(Material)) as Material;

            _materialList.Add(mat);

            var currentTextureFilePath = textureFilePath + matBase + index;
            _textTurePathList.Add(currentTextureFilePath);

        }

        _firstTexturePath = textureFilePath + firstMateralName;
        _firstMaterial = Resources.Load(materialFilePath + firstMateralName, typeof(Material)) as Material;   
    }

    private void Update()
    {
        if (CurrentGameState == GameState.DeletingPuzzles)
        {
            if (CurrentPuzzleState == PuzzleState.CanRotate)
            {
                DestroyPicture();
                CheckGameEnd();
            }
        }

        if (CurrentGameState == GameState.Flipback && !_coroutineStarted)
        {
            if (CurrentPuzzleState == PuzzleState.CanRotate)
            {
                StartCoroutine(FlipBack());
            }
        }

        if (CurrentGameState == GameState.GameEnd)
        {
            if (!PictureList[_firstRevealedPic].gameObject.activeSelf &&
                !PictureList[_secondRevealedPic].gameObject.activeSelf && 
                !EndGamePanel.activeSelf)
            {
                ShowEndGameInformation();
            }
        }
    }
    private void ShowEndGameInformation()
    {
        EndGamePanel.SetActive(true);
        if (Config.IsBestScore())
        {
            NewBestScoreText.SetActive(true);
            YourScoreText.SetActive(false);
        } else
        {
            NewBestScoreText.SetActive(false);
            YourScoreText.SetActive(true);
        }

        float timer = _gameTimer.GetCurrentTime();
        float minutes = Mathf.Floor(timer / 60);
        int seconds = Mathf.RoundToInt(timer % 60);

        string newText = minutes.ToString("00") + ":" + seconds.ToString("00");

        EndTimeText.GetComponent<Text>().text = newText;
    }

    private bool CheckGameEnd()
    {
        if (_removedPairs == _pairNumbers && CurrentGameState != GameState.GameEnd)
        {
            CurrentGameState = GameState.GameEnd;
            _gameTimer.StopTimer();
            Config.PlaceScoreOnBoard(_gameTimer.GetCurrentTime());
        }

        return CurrentGameState == GameState.GameEnd;
    }
    public void CheckPicture()
    {
        CurrentGameState = GameState.Checking;
        _revealedPicNumber = 0;

        for (int id = 0; id < PictureList.Count; id++)
        {
            if (PictureList[id].Revealed && _revealedPicNumber < 2) 
            {
                if (_revealedPicNumber == 0)
                {
                    _firstRevealedPic = id;
                    _revealedPicNumber++;
                } else if (_revealedPicNumber == 1)
                {
                    _secondRevealedPic = id;
                    _revealedPicNumber++;
                }
            }
        }

        if (_revealedPicNumber == 2)
        {
            if (PictureList[_firstRevealedPic].GetIndex() == PictureList[_secondRevealedPic].GetIndex() && _firstRevealedPic != _secondRevealedPic)
            {
                CurrentGameState = GameState.DeletingPuzzles;
                _pictureToDestroy1 = _firstRevealedPic;
                _pictureToDestroy2 = _secondRevealedPic;
            }
            else
            {
                CurrentGameState = GameState.Flipback;
            }
        }

        CurrentPuzzleState = PuzzleState.CanRotate;

        if (CurrentGameState == GameState.Checking)
        {
            CurrentGameState = GameState.NoAction;
        }
    }

    private void DestroyPicture()
    {
        PuzzleRevealedNumber = RevealedState.NoRevealed;
        PictureList[_pictureToDestroy1].Deactivate();
        PictureList[_pictureToDestroy2].Deactivate();
        _revealedPicNumber = 0;
        _removedPairs++;
        CurrentGameState = GameState.NoAction;
        CurrentPuzzleState = PuzzleState.CanRotate;
    }

    private IEnumerator FlipBack()
    {
        _coroutineStarted = true;

        yield return new WaitForSeconds(0.5f);

        PictureList[_firstRevealedPic].FlipBack();
        PictureList[_secondRevealedPic].FlipBack();

        PictureList[_firstRevealedPic].Revealed = false;
        PictureList[_secondRevealedPic].Revealed = false;

        PuzzleRevealedNumber = RevealedState.NoRevealed;
        CurrentGameState = GameState.NoAction;

        _coroutineStarted = false;
    }

    private void SpawnPictureMesh(int rows, int columns, Vector2 Pos, Vector2 offset, bool scaleDown)
    {
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                var tempPicture = (Picture)Instantiate(PicturePrefab, PicSpawnPosition.position, PicturePrefab.transform.rotation);
                
                if (scaleDown)
                {
                    tempPicture.transform.localScale = _newScaleDown;
                }

                tempPicture.name = tempPicture.name + "c" + col + "r" + row;
                PictureList.Add(tempPicture);
            }
        }

        ApplyTextures();
    }

    public void ApplyTextures()
    {
        var rndMatIndex = Random.Range(0, _materialList.Count);
        var AppliedTimes = new int[_materialList.Count];

        for (int i = 0; i < _materialList.Count; i++)
        {
            AppliedTimes[i] = 0;
        }

        foreach (var o in PictureList)
        {
            var randPrevious = rndMatIndex;
            int counter = 0;
            bool forceMat = false;

            while (AppliedTimes[rndMatIndex] >= 2 || (randPrevious == rndMatIndex && !forceMat))
            {
                rndMatIndex = Random.Range(0, _materialList.Count);
                counter++;
                if (counter > 100)
                {
                    for (int j = 0; j < _materialList.Count; j++)
                    {
                        if (AppliedTimes[j] < 2)
                        {
                            rndMatIndex = j;
                            forceMat = true;
                        }
                    }

                    if (!forceMat)
                        return;
                }
            }

            o.SetFirstMaterial(_firstMaterial, _firstTexturePath);
            o.ApplyFirstMaterial();
            o.SetSecondMaterial(_materialList[rndMatIndex], _textTurePathList[rndMatIndex]);
            o.SetIndex(rndMatIndex);
            o.Revealed = false;
            AppliedTimes[rndMatIndex]++;
            forceMat = false;
        }
    }

    private void MovePicture(int rows, int cols, Vector2 Pos, Vector2 offset)
    {
        int index = 0;
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                var targetPos = new Vector3(Pos.x + offset.x * row, Pos.y - offset.y * col, 0.0f);
                StartCoroutine(MoveToPosition(targetPos, PictureList[index]));
                index++;
            }
        }
    }

    private IEnumerator MoveToPosition(Vector3 target, Picture obj)
    {
        var randomDistance = 7;
        while (target != obj.transform.position)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, target, randomDistance * Time.deltaTime);
            yield return 0;
        }
    }
}
