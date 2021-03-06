﻿using UnityEngine;
using Slug;
using UnityEngine.SceneManagement;
using SlugLib;

namespace Mission169 {

    public class GameManager : Singleton<GameManager> {

        protected GameManager() { }

        private static readonly int playerLifeStart = 3;
        private static int playerLifeCount = playerLifeStart;
        private int playerScore = 0;
        private readonly float waitTimeBeforeSpawn = 1.5f;
        private int currentMissionID = 0;
        private string[] missionList = new[] { "mission1", "mission2" };

        private TimeUtils timeUtils;
        private Transform missionStartPos;
        private GameObject playerGameObject;
        private Transform playerTransform;
        private PlayerDeathManager playerDeathManager;
        private HUDManager hud;
        private MainMenu mainMenu;
        private DialogManager dialog;

        public GameObject playerPrefab;
        public GameObject readyGOPrefab;

        void Awake() {
            DontDestroyOnLoad(this);

            EventManager.Instance.StartListening(GlobalEvents.PlayerDead, OnplayerDeath);
            EventManager.Instance.StartListening(GlobalEvents.BossDead, OnMissionSuccess);
            EventManager.Instance.StartListening(GlobalEvents.PointsEarned, UpdatePlayerPoints);
            timeUtils = gameObject.AddComponent<TimeUtils>();
            playerGameObject = Instantiate( Resources.Load("Player")) as GameObject;
            playerDeathManager = playerGameObject.GetComponentInChildren<PlayerDeathManager>();
            playerGameObject.SetActive(false);
            playerGameObject.transform.parent = transform;

            //FIXME  you know what
            playerTransform = playerGameObject.transform.GetChild(0).transform;

            hud = UIManager.Instance.HUD;
            mainMenu = UIManager.Instance.MainMenuT;
            dialog = UIManager.Instance.Dialog;
        }

        public GameObject GetPlayer() {
            return playerGameObject;
        }

        public void Home() {
            ResetGameData();
            MissionLoad();
            MissionInit();
            mainMenu.SetVisible(true);
            hud.SetVisible(false);
        }

        public void MissionInit() {
            playerGameObject.GetComponentInChildren<AnimationManager>().ResetAnimators();
            playerGameObject.layer = (int)SlugLayers.Player;
            GameObject startPos = GameObject.Find("StartLocation");
            playerTransform.position = startPos.transform.position;
            playerGameObject.SetActive(false);
        }

        public void MissionStart() {
            EventManager.Instance.TriggerEvent(GlobalEvents.MissionStart);
            mainMenu.SetVisible(false);
            hud.SetLifeCount(playerLifeCount);
            hud.SetVisible(true);
            hud.ShowReadyGo();
            timeUtils.TimeDelay(1.8f, () => {
                playerGameObject.SetActive(true);
                playerDeathManager.SpawnPlayer();
            });
        }

        public void MissionRetry() {
            ResetGameData();
            MissionLoad();
            MissionInit();
            // Work around for some reason the music does not start again otherwise...
            Invoke("MissionStart", 0.2f);
        }

        public void GoNextMission() {
            currentMissionID++;
            if (currentMissionID < missionList.Length) {
                MissionLoad();
                MissionInit();
                MissionStart();
            } else {
                Home();
            }
        }

        //TODO block user input too
        public void PauseGame(bool paused) {
            Time.timeScale = paused ? 0 : 1;
        }

        private void UpdatePlayerPoints(float pts) {
            playerScore = playerScore + (int)pts;
            hud.SetScore(playerScore);
        }

        private void OnplayerDeath() {
            playerLifeCount--;
            hud.SetLifeCount(playerLifeCount);
            if (playerLifeCount >= 0) {
                timeUtils.TimeDelay(waitTimeBeforeSpawn, () => {
                    playerDeathManager.SpawnPlayer();
                    EventManager.Instance.TriggerEvent(GlobalEvents.PlayerSpawned);
                });
            } else {
                GameOver();
            }
        }

        private void OnMissionSuccess() {
            MissionEnd();
            Invoke("ShowSuccessDialog", 3);
            EventManager.Instance.TriggerEvent(GlobalEvents.MissionSuccess);
        }

        private void MissionLoad() {
            hud.SetVisible(false);
            SceneManager.LoadScene(missionList[currentMissionID]);
            hud.SetVisible(true);
        }

        private void GameOver() {
            MissionEnd();
            ResetGameData();
            hud.SetVisible(false);
            dialog.Activate(DialogType.GameOver);
            EventManager.Instance.TriggerEvent(GlobalEvents.GameOver);
        }

        private void ResetGameData() {
            playerScore = 0;
            currentMissionID = 0;
            playerLifeCount = playerLifeStart;
            hud.SetScore(playerScore);
            hud.SetLifeCount(playerLifeCount);
        }

        private void MissionEnd() {
            EventManager.Instance.TriggerEvent(GlobalEvents.MissionEnd);
            playerGameObject.GetComponentInChildren<MovementManager>().StopMoving();
            playerGameObject.GetComponentInChildren<InputManager>().enabled = false;
            playerGameObject.GetComponentInChildren<AnimationManager>().MissionCompleteAnim();
            playerTransform.gameObject.layer = (int)SlugLayers.IgnoreRaycast; //to ignore any potential projectile still going
           // AchievementManager.Instance.SaveAchievementsLocally();
        }

        private void ShowSuccessDialog() {
            dialog.Activate(DialogType.MissionSuccess);
        }

    }
}
