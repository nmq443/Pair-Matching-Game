using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Picture : MonoBehaviour
{
    public AudioClip PressedSound;
    private Material _firstMaterial;
    private Material _secondMaterial;
    private Quaternion _currentRotation;

    [HideInInspector] public bool Revealed = false;
    private PictureManager _pictureManager;

    private bool _clicked = false;
    private int index;
    private AudioSource _audio;

    public void SetIndex(int id) { index = id; }

    public int GetIndex() { return index; }

    private void Start()
    {
        _clicked = false;
        _pictureManager = GameObject.Find("[PictureManager]").GetComponent<PictureManager>();
        _currentRotation = gameObject.transform.rotation; 

        _audio = GetComponent<AudioSource>();
        _audio.clip = PressedSound;
    }

    private void Update()
    {
        
    }

    private void OnMouseDown()
    {
        if (!_clicked)
        {
            _pictureManager.CurrentPuzzleState = PictureManager.PuzzleState.PuzzleRotating;

            if (!GameSettings.Instance.IsSoundEffectMutedPermanently())
                _audio.Play();

            StartCoroutine(LoopRotation(45, false));
            _clicked = true;
        }
    }

    public void FlipBack()
    {
        if (gameObject.activeSelf)
        {
            _pictureManager.CurrentPuzzleState = PictureManager.PuzzleState.PuzzleRotating;
            Revealed = false;

            if (!GameSettings.Instance.IsSoundEffectMutedPermanently())
                _audio.Play();

            StartCoroutine(LoopRotation(45, true));
        }
    }

    private IEnumerator LoopRotation(float angle, bool FirstMat)
    {
        float rot = 0f;
        const float dir = 1f;
        const float rotSpeed = 180.0f;
        const float rotSpeed1 = 90.0f;
        bool assigned = false;
        float startAngle = angle;
        
        if (FirstMat)
        {
            while (rot < angle)
            {
                float step = Time.deltaTime * rotSpeed1;
                gameObject.GetComponent<Transform>().Rotate(new Vector3(0, 2, 0) * step * dir);
                if (rot >= (startAngle - 2) && !assigned)
                {
                    ApplyFirstMaterial();
                    assigned = true;
                }
                rot += (1 * step * dir);
                yield return null;
            }
        } else
        {
            while (angle > 0)
            {
                float step = Time.deltaTime * rotSpeed;
                gameObject.GetComponent<Transform>().Rotate(new Vector3(0, 2, 0) * step * dir);
                angle -= (1 * step * dir);
                yield return null;
            }
        }

        gameObject.GetComponent<Transform>().rotation = _currentRotation;

        if (!FirstMat)
        {
            Revealed = true;
            ApplySecondMaterial();
            _pictureManager.CheckPicture();
        } else
        {
            _pictureManager.CurrentPuzzleState = PictureManager.PuzzleState.CanRotate;
            _pictureManager.PuzzleRevealedNumber = PictureManager.RevealedState.NoRevealed;
        }

        _clicked = false;
    } 

    public void SetFirstMaterial(Material mat, string texturePath)
    {
        _firstMaterial = mat;
        _firstMaterial.mainTexture = Resources.Load(texturePath, typeof(Texture2D)) as Texture2D;
    }

    public void SetSecondMaterial(Material mat, string texturePath)
    {
        _secondMaterial = mat;
        _secondMaterial.mainTexture = Resources.Load(texturePath, typeof(Texture2D)) as Texture2D;
    }

    public void ApplyFirstMaterial()
    {
        gameObject.GetComponent<Renderer>().material = _firstMaterial;
    }

    public void ApplySecondMaterial()
    {
        gameObject.GetComponent<Renderer>().material = _secondMaterial;
    }

    public void Deactivate()
    {
        StartCoroutine(DeactivateCoroutine());
    }

    private IEnumerator DeactivateCoroutine()
    {
        Revealed = false;

        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
}
