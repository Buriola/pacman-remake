using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using Pacman.UI;

namespace Pacman.Controllers
{
    /// <summary>
    /// The main game controller. Responsible for changing scenes and saving score
    /// </summary>
    public class GameController : MonoBehaviour
    {
        //Singleton
        private static GameController instance;
        public static GameController Instance { get { return instance; } }

        public static bool isOnePlayerGame;
        public const string HIGH_SCORE = "HighScore";

        private bool changingScene;

        private void Awake()
        {
            if (instance != null && instance != this)
                Destroy(gameObject);

            instance = this;
            isOnePlayerGame = true;

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            RequestSceneChange(1);
        }

        /// <summary>
        /// Saves the high score
        /// </summary>
        /// <param name="score">The score you want to save</param>
        public void SaveHighScore(int score)
        {
            if (PlayerPrefs.GetInt(HIGH_SCORE) < score)
            {
                PlayerPrefs.SetInt(HIGH_SCORE, score);
            }
        }

        /// <summary>
        /// Loads the high score and returns it
        /// </summary>
        /// <returns></returns>
        public int LoadHighScore()
        {
            if (PlayerPrefs.HasKey(HIGH_SCORE))
            {
                return PlayerPrefs.GetInt(HIGH_SCORE);
            }
            else
            {
                PlayerPrefs.SetInt(HIGH_SCORE, 10000);
                return 10000;
            }
        }

        /// <summary>
        /// Trigger the Change scene coroutine
        /// </summary>
        /// <param name="sceneID">The scene you want to load</param>
        public void RequestSceneChange(int sceneID)
        {
            //Can only request scene change once
            if (!changingScene)
            {
                changingScene = true;
                StartCoroutine(ChangeScene(sceneID));
            }
        }

        /// <summary>
        /// Loads a scene
        /// </summary>
        /// <param name="sceneID"></param>
        /// <returns></returns>
        private IEnumerator ChangeScene(int sceneID)
        {
            yield return SceneManager.LoadSceneAsync(sceneID);
            changingScene = false;
        }

    }
}