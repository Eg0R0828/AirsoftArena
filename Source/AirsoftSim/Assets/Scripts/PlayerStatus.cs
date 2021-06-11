using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PlayerStatus : NetworkBehaviour {

    [SerializeField] PlayerSetup player_setup;

    // Данные о бое и достижениях игрока
    [SerializeField] int earnings = 0;
    [SerializeField] float bonus = 1.0f;
    public int position = -1;
    public string playmode = "";

    // Вспомогательные поля (зависят от хар-к боя)
    [SerializeField] Transform respawnPoint = null;
    [SerializeField] float time_to_respawn = 30.0f;
    public GameObject neutralFlagPref, team1FlagPref, team2FlagPref;
    [SerializeField] GameObject neutralFlagObj, team1FlagObj, team2FlagObj;

    // Данные о самом игроке
    [SyncVar(hook = nameof(SyncStatus))] string status = "ready";
    [SyncVar(hook = nameof(SyncPlayProgress))] string playProgress = "without flag";
    [SyncVar(hook = nameof(SyncKills))] int kills = 0;
    [SyncVar(hook = nameof(SyncTeam))] int team = 0;

    // Поля для ЛОКАЛЬНОГО использования
    GameObject deadInfoLabel, aliveInfoLabel, timeToRespawn;
    float timer = 0.0f;
    bool start_interval = false;
    [SerializeField] GameObject[] network_players;

    void Update() {
        if (GetPlayProgress == "without flag") { neutralFlagObj.SetActive(false); team1FlagObj.SetActive(false); team2FlagObj.SetActive(false); }
        else if ((GetPlayProgress == "allied flag" && GetTeam == 1) || (GetPlayProgress == "enemy flag" && GetTeam == 2)) team1FlagObj.SetActive(true);
        else if ((GetPlayProgress == "allied flag" && GetTeam == 2) || (GetPlayProgress == "enemy flag" && GetTeam == 1)) team2FlagObj.SetActive(true);

        if (isLocalPlayer) {
            if (position != -1 && GetTeam == 0) {
                // Инициализация зависимых от статуса игрока элементов интерфейса
                while (!deadInfoLabel) deadInfoLabel = GameObject.Find("Canvas/RespawnLabel");
                while (!aliveInfoLabel) aliveInfoLabel = GameObject.Find("Canvas/ReadyLabel");
                while (!timeToRespawn) timeToRespawn = GameObject.Find("Canvas/TimeToRespawn");

                // Назначение команды, точки "мертвяка" и режима игры, которого он будет придерживаться, локальному игроку
                int team_number;
                if (position < (player_setup.lobby_controller.teamSpawners.Length / 2)) {
                    team_number = 1;
                    respawnPoint = player_setup.lobby_controller.team1ItemSpawner;
                } else {
                    team_number = 2;
                    respawnPoint = player_setup.lobby_controller.team2ItemSpawner;
                }
                CmdSetTeam(team_number);
            }

            // Проверка готовности начинать (что все необходимые локальные поля определены и все игроки загрузились)
            if (GetTeam == 0 || !respawnPoint || playmode == "") return;
            int lobby_players_count = 0;
            foreach (NetworkLobbyPlayer player in player_setup.game_manager.network_manager.lobbySlots) if (player) lobby_players_count++;
            network_players = GameObject.FindGameObjectsWithTag("Player");
            if (lobby_players_count == 0 || network_players.Length == 0 || network_players.Length < lobby_players_count) return;

            // Переключение статусов игрока (-> Ready[ждет разрешения на "возрождение"] -> Alive[активен] -> Dead[выведен из игры] ->)
            if (GetStatus == "dead" && playmode != "Encounter battle" && playmode != "Grand battle") {
                deadInfoLabel.SetActive(true);
                if (Vector3.Distance(transform.position, respawnPoint.position) <= 10.0f) {
                    CmdChangeStatus("ready");
                    timeToRespawn.SetActive(true);
                }
            } else deadInfoLabel.SetActive(false);
            if (GetStatus == "ready") {
                timeToRespawn.GetComponent<Text>().text = "Time to re-spawn: " + ((int)(time_to_respawn - timer)).ToString() + " s";
                timer += Time.deltaTime;
                if (timer >= time_to_respawn) {
                    timer = 0.0f;
                    if (!start_interval) start_interval = true;
                    CmdChangeStatus("alive");
                }
            } else timeToRespawn.SetActive(false);
            if (GetStatus == "alive" && Vector3.Distance(transform.position, respawnPoint.position) <= 10.0f) {
                aliveInfoLabel.SetActive(true);
                if (GetPlayProgress == "neutral flag" || GetPlayProgress == "enemy flag") CmdChangePlayProgress("complete");
            }
            else aliveInfoLabel.SetActive(false);

            // Проверка исхода боя (и закончился ли он вообще) в зависимости от его режима;
            if (start_interval && !player_setup.endMatch) {
                switch (playmode) {
                    case "Encounter battle":
                        bool allEnemiesAreDead = true;
                        bool allAlliesAreDead = true;
                        foreach (GameObject player in network_players) {
                            PlayerStatus player_status = player.GetComponent<PlayerStatus>();
                            if (player_status.GetTeam != GetTeam && player_status.GetStatus != "dead") allEnemiesAreDead = false;
                            if (player_status.GetTeam == GetTeam && player_status.GetStatus != "dead") allAlliesAreDead = false;
                        }
                        if (allAlliesAreDead) player_setup.MatchResults(false, MatchEarnings(), GetKills);
                        else if (allEnemiesAreDead) {
                            if (GetStatus == "Alive") bonus += 1.5f; else bonus += 1.0f;
                            player_setup.MatchResults(true, MatchEarnings(), GetKills);
                        }
                        break;
                    case "Capturing flags":
                        if (GetPlayProgress == "complete") {
                            if (GetStatus == "Alive") bonus += 3.0f; else bonus += 2.0f;
                            player_setup.MatchResults(true, MatchEarnings(), GetKills);
                        } else foreach (GameObject player in network_players) {
                            PlayerStatus player_status = player.GetComponent<PlayerStatus>();
                            if (player_status.GetPlayProgress == "complete" && player_status.GetTeam == GetTeam) {
                                if (GetStatus == "Alive") bonus += 1.5f; else bonus += 1.0f;
                                player_setup.MatchResults(true, MatchEarnings(), GetKills);
                            } else if (player_status.GetPlayProgress == "complete" && player_status.GetTeam != GetTeam) player_setup.MatchResults(false, MatchEarnings(), GetKills);
                        }
                        break;
                    case "Capturing flag":
                        if (GetPlayProgress == "complete") {
                            if (GetStatus == "Alive") bonus += 4.0f; else bonus += 3.0f;
                            player_setup.MatchResults(true, MatchEarnings(), GetKills);
                        } else foreach (GameObject player in network_players) {
                            PlayerStatus player_status = player.GetComponent<PlayerStatus>();
                            if (player_status.GetPlayProgress == "complete" && player_status.GetTeam == GetTeam) {
                                if (GetStatus == "Alive") bonus += 1.5f; else bonus += 1.0f;
                                player_setup.MatchResults(true, MatchEarnings(), GetKills);
                            } else if (player_status.GetPlayProgress == "complete" && player_status.GetTeam != GetTeam) player_setup.MatchResults(false, MatchEarnings(), GetKills);
                        }
                        break;
                    case "Grand battle":
                        if (GetStatus == "dead") player_setup.MatchResults(false, MatchEarnings(), GetKills);
                        else {
                            bool isVictory = true;
                            foreach (GameObject player in network_players) if (player.GetComponent<PlayerStatus>().GetStatus != "dead") isVictory = false;
                            bonus += 4.0f;
                            if (isVictory) player_setup.MatchResults(true, MatchEarnings(), GetKills);
                        }
                        break;
                    case "Bomb planting":
                        break;
                    case "Training": break;
                }
            }

            // Сбрасывание флага
            if (Input.GetKeyDown(KeyCode.G) && GetPlayProgress != "without flag" && GetPlayProgress != "complete") DropFlag();
        }
    }

    // Поднятие флага
    public void DragFlag(string newPlayStatus) {
        CmdChangePlayProgress(newPlayStatus);
    }

    // Сбрасывание флага
    void DropFlag() {
        CmdChangePlayProgress("without flag");
    }

    // Считывание попаданий в СЕБЯ
    public void Penetration() {
        if (player_setup.inventory) player_setup.InventoryExit();
        if (GetPlayProgress != "without flag") DropFlag();
        CmdChangeStatus("dead");
        Debug.Log(gameObject.name + ": Oh, I have ♂ PENETRATION ♂ !");
    }

    // Подсчет выручки за бой
    public int MatchEarnings() {
        return (int)(earnings * bonus);
    }

    // Засчитывание "убийства противника"
    public void AddKill() {
        CmdAddKill();
        earnings += 30;
    }

    //=================================================================================================================================
    // "Интерфейс" управления SyncVar-переменными
    [Command] void CmdChangeStatus(string newStatus) { SyncStatus(newStatus); }
    void SyncStatus(string newStatus) { status = newStatus; }
    public string GetStatus => status;

    [Command] void CmdChangePlayProgress(string newPlayStatus) {
        if (newPlayStatus == "without flag") {
            GameObject flag = null;
            if (GetPlayProgress == "neutral flag") flag = Instantiate(neutralFlagPref, transform.position, Quaternion.identity);
            else if ((GetPlayProgress == "allied flag" && GetTeam == 1) || (GetPlayProgress == "enemy flag" && GetTeam == 2))
                flag = Instantiate(team1FlagPref, transform.position, Quaternion.identity);
            else if ((GetPlayProgress == "allied flag" && GetTeam == 2) || (GetPlayProgress == "enemy flag" && GetTeam == 1))
                flag = Instantiate(team2FlagPref, transform.position, Quaternion.identity);
            if (flag) NetworkServer.Spawn(flag);
        }
        SyncPlayProgress(newPlayStatus);
    }
    void SyncPlayProgress(string newPlayStatus) { playProgress = newPlayStatus; }
    public string GetPlayProgress => playProgress;

    [Command] void CmdAddKill() { SyncKills(GetKills + 1); }
    void SyncKills(int newCount) { kills = newCount; }
    public int GetKills => kills;

    [Command] void CmdSetTeam(int newTeam) { SyncTeam(newTeam); }
    void SyncTeam(int newTeam) { team = newTeam; }
    public int GetTeam => team;
}
