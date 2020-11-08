using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Buriola.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private bool _isPlayerOne;
        private bool _choosingMap;

        [FormerlySerializedAs("playerSelectObject")] 
        public GameObject PlayerSelectObject;
        [FormerlySerializedAs("mapSelectObject")] 
        public GameObject MapSelectObject;

        [FormerlySerializedAs("playerOneText")] 
        public Text PlayerOneText;
        [FormerlySerializedAs("playerTwoText")] 
        public Text PlayerTwoText;
        [FormerlySerializedAs("arrowText")] 
        public Text ArrowText;

        //Arrow positions
        private Vector3 _upPosition;
        private Vector3 _downPosition;

        private void Start()
        {
            _upPosition = new Vector3(ArrowText.transform.localPosition.x, PlayerOneText.transform.localPosition.y,
                            ArrowText.transform.localPosition.z);

            _downPosition = ArrowText.transform.localPosition = new Vector3(ArrowText.transform.localPosition.x, PlayerTwoText.transform.localPosition.y,
                        ArrowText.transform.localPosition.z);

            PlayerSelectObject.SetActive(true);
            MapSelectObject.SetActive(false);

            _choosingMap = false;
            _isPlayerOne = true;
            ArrowText.transform.localPosition = _upPosition;
        }

        private void Update()
        {
            if (Input.GetAxis("Vertical") > 0)
            {
                if (!_choosingMap)
                {
                    if (!_isPlayerOne)
                    {
                        _isPlayerOne = true;
                        ArrowText.transform.localPosition = _upPosition;

                        GameController.IsOnePlayerGame = true;
                    }
                }
                else
                {
                    ArrowText.transform.localPosition = _upPosition;
                }
            }
            else if (Input.GetAxis("Vertical") < 0)
            {
                if (!_choosingMap)
                {
                    if (_isPlayerOne)
                    {
                        _isPlayerOne = false;
                        ArrowText.transform.localPosition = _downPosition;

                        GameController.IsOnePlayerGame = false;
                    }
                }
                else
                {
                    ArrowText.transform.localPosition = _downPosition;
                }
            }

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Return))
            {
                if (!_choosingMap)
                {
                    _choosingMap = true;
                    PlayerSelectObject.SetActive(false);
                    MapSelectObject.SetActive(true);

                    ArrowText.transform.localPosition = _upPosition;
                }
                else
                {
                    if (ArrowText.transform.localPosition == _upPosition)
                        GameController.Instance.RequestSceneChange(2);
                    else
                        GameController.Instance.RequestSceneChange(3);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_choosingMap)
                {
                    MapSelectObject.SetActive(false);
                    PlayerSelectObject.SetActive(true);

                    _isPlayerOne = true;
                    GameController.IsOnePlayerGame = true;

                    ArrowText.transform.localPosition = _upPosition;
                    _choosingMap = false;
                }
            }
        }
    }
}
