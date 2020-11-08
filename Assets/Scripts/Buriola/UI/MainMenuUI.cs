using UnityEngine;
using UnityEngine.UI;

namespace Buriola.UI
{
    /// <summary>
    /// Main menu class
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        //Aux bools to set the arrow
        private bool isPlayerOne = true;
        private bool choosingMap = false;

        //Player select menu
        public GameObject playerSelectObject;
        //Map select menu
        public GameObject mapSelectObject;

        public Text playerOneText;
        public Text playerTwoText;
        public Text arrowText;

        //Arrow positions
        private Vector3 upPosition;
        private Vector3 downPosition;

        private void Start()
        {
            upPosition = new Vector3(arrowText.transform.localPosition.x, playerOneText.transform.localPosition.y,
                            arrowText.transform.localPosition.z);

            downPosition = arrowText.transform.localPosition = new Vector3(arrowText.transform.localPosition.x, playerTwoText.transform.localPosition.y,
                        arrowText.transform.localPosition.z);

            playerSelectObject.SetActive(true);
            mapSelectObject.SetActive(false);

            choosingMap = false;
            isPlayerOne = true;
            arrowText.transform.localPosition = upPosition;
        }

        private void Update()
        {
            if (Input.GetAxis("Vertical") > 0)
            {
                if (!choosingMap)
                {
                    if (!isPlayerOne)
                    {
                        isPlayerOne = true;
                        arrowText.transform.localPosition = upPosition;

                        GameController.isOnePlayerGame = true;
                    }
                }
                else
                {
                    arrowText.transform.localPosition = upPosition;
                }
            }
            else if (Input.GetAxis("Vertical") < 0)
            {
                if (!choosingMap)
                {
                    if (isPlayerOne)
                    {
                        isPlayerOne = false;
                        arrowText.transform.localPosition = downPosition;

                        GameController.isOnePlayerGame = false;
                    }
                }
                else
                {
                    arrowText.transform.localPosition = downPosition;
                }
            }

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Return))
            {
                if (!choosingMap)
                {
                    choosingMap = true;
                    playerSelectObject.SetActive(false);
                    mapSelectObject.SetActive(true);

                    arrowText.transform.localPosition = upPosition;
                }
                else
                {
                    if (arrowText.transform.localPosition == upPosition)
                        GameController.Instance.RequestSceneChange(2);
                    else
                        GameController.Instance.RequestSceneChange(3);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (choosingMap)
                {
                    mapSelectObject.SetActive(false);
                    playerSelectObject.SetActive(true);

                    isPlayerOne = true;
                    GameController.isOnePlayerGame = true;

                    arrowText.transform.localPosition = upPosition;
                    choosingMap = false;
                }
            }
        }
    }
}
