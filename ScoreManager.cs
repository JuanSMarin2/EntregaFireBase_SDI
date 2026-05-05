using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class ScoreManager : MonoBehaviour
{
    [Header("Current")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text currentPlayerText;

    [Header("Leaderboard UI")]
    [SerializeField] private TMP_Text[] leaderboardNames;
    [SerializeField] private TMP_Text[] leaderboardScores;
    [SerializeField] private AuthHandler authHandler;

    private FirebaseDatabase database;
    private bool leaderboardLoaded = false;
    private bool firebaseReady = false;

    private List<PlayerScore> scores = new List<PlayerScore>();

    private const int MAX_PLAYERS = 5;

    void Start()
    {
        firebaseReady = false;
        
        // Validar referencias
        if (authHandler == null)
        {
            Debug.LogError("AuthHandler no esta asignado en ScoreManager");
        }
        
        if (leaderboardNames == null || leaderboardScores == null)
        {
            Debug.LogError("Leaderboard arrays no estan asignados en ScoreManager");
        }
        // Si ya existe una instancia de FirebaseApp, usarla en vez de reinicializar
        try
        {
            if (Firebase.FirebaseApp.DefaultInstance != null)
            {
                database = FirebaseDatabase.DefaultInstance;
                if (database == null)
                {
                    Debug.LogError("Firebase Database es null despues de obtener DefaultInstance");
                }
                else
                {
                    firebaseReady = true;
                    Debug.Log("ScoreManager usando FirebaseApp.DefaultInstance");
                }
            }
            else
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    if (task == null)
                    {
                        Debug.LogError("Firebase CheckAndFixDependencies retorno null");
                        return;
                    }
                    
                    if (task.Result == DependencyStatus.Available)
                    {
                        try
                        {
                            database = FirebaseDatabase.DefaultInstance;
                            if (database == null)
                            {
                                Debug.LogError("Firebase Database es null despues de inicializar");
                                return;
                            }
                            firebaseReady = true;
                            Debug.Log("ScoreManager Firebase inicializado exitosamente");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("Excepcion al inicializar Firebase en ScoreManager: " + e.ToString());
                        }
                    }
                    else
                    {
                        Debug.LogError("Firebase no disponible en ScoreManager: " + task.Result);
                    }
                });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Excepcion verificando FirebaseApp.DefaultInstance: " + e.ToString());
        }
    }

    private void Update()
    {
        if (authHandler != null && authHandler.IsLoggedIn && !leaderboardLoaded && firebaseReady)
        {
            LoadLeaderboardFromFirebase();
            leaderboardLoaded = true;
        }
    }

    // =========================
    // CURRENT SCORE
    // =========================

    public void SetCurrentScore(string playerName, int score)
    {
        if (currentScoreText != null)
            currentScoreText.text = score.ToString();
        
        if (currentPlayerText != null)
            currentPlayerText.text = playerName ?? "Desconocido";
    }

    // =========================
    // FIREBASE LEADERBOARD
    // =========================

    public void LoadLeaderboardFromFirebase()
    {
        if (!firebaseReady)
        {
            Debug.LogWarning("Firebase no esta listo, no se puede cargar leaderboard");
            return;
        }

        if (database == null)
        {
            Debug.LogError("database es null en LoadLeaderboardFromFirebase");
            return;
        }

        StartCoroutine(GetLeaderboardFromFirebase());
    }

    IEnumerator GetLeaderboardFromFirebase()
    {
        if (database == null)
        {
            Debug.LogError("database es null al iniciar GetLeaderboardFromFirebase");
            yield break;
        }

        // Leer todos los usuarios con sus scores
        var task = database.RootReference
            .Child("users")
            .OrderByChild("score")
            .LimitToLast(MAX_PLAYERS)
            .GetValueAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Error leaderboard: " + task.Exception.ToString());
            yield break;
        }

        if (task.Result == null)
        {
            Debug.LogWarning("task.Result es null en leaderboard");
            yield break;
        }

        scores.Clear();

        try
        {
            if (task.Result.Exists)
            {
                // Los resultados vienen en orden ascendente, pero necesitamos descendente
                var userDataList = new List<(string username, int score, string userId)>();

                foreach (var child in task.Result.Children)
                {
                    try
                    {
                        if (child == null)
                        {
                            Debug.LogWarning("Child es null en leaderboard");
                            continue;
                        }

                        // Obtener username
                        var usernameNode = child.Child("username");
                        string username = "Desconocido";
                        if (usernameNode != null && usernameNode.Value != null)
                        {
                            username = usernameNode.Value.ToString();
                        }

                        // Obtener score de forma segura
                        var scoreNode = child.Child("score");
                        int score = 0;
                        if (scoreNode != null && scoreNode.Value != null)
                        {
                            string scoreStr = scoreNode.Value.ToString();
                            if (!int.TryParse(scoreStr, out score))
                            {
                                Debug.LogWarning("No se pudo parsear score: " + scoreStr + " para usuario: " + username);
                                score = 0;
                            }
                        }

                        string userId = child.Key ?? "unknown";

                        userDataList.Add((username, score, userId));
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error parsing user data: " + e.ToString());
                    }
                }

                // Ordenar descendente por score y tomar los primeros MAX_PLAYERS
                userDataList = userDataList
                    .OrderByDescending(x => x.score)
                    .Take(MAX_PLAYERS)
                    .ToList();

                // Convertir a PlayerScore
                foreach (var item in userDataList)
                {
                    scores.Add(new PlayerScore
                    {
                        name = item.username,
                        score = item.score,
                        userId = item.userId
                    });
                }

                Debug.Log("Leaderboard cargado exitosamente: " + scores.Count + " jugadores");
            }
            else
            {
                Debug.Log("No hay datos de usuarios en la base de datos");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Excepcion en GetLeaderboardFromFirebase: " + e.ToString());
        }

        UpdateLeaderboardUI();
    }

    // =========================
    // UI
    // =========================

    void UpdateLeaderboardUI()
    {
        if (leaderboardNames == null || leaderboardScores == null)
        {
            Debug.LogError("Leaderboard UI arrays son null");
            return;
        }

        int maxLength = Mathf.Min(leaderboardNames.Length, leaderboardScores.Length);

        for (int i = 0; i < maxLength; i++)
        {
            if (leaderboardNames[i] == null || leaderboardScores[i] == null)
            {
                Debug.LogWarning("Leaderboard UI elemento " + i + " es null");
                continue;
            }

            if (i < scores.Count)
            {
                leaderboardNames[i].text = (i + 1) + ". " + (scores[i].name ?? "Desconocido");
                leaderboardScores[i].text = scores[i].score.ToString();
            }
            else
            {
                leaderboardNames[i].text = (i + 1) + ". ---";
                leaderboardScores[i].text = "---";
            }
        }

        Debug.Log("Leaderboard UI actualizado");
    }

    // DEPRECATED - Mantener para compatibilidad si es necesario
    public void LoadLeaderboardFromAPI()
    {
        LoadLeaderboardFromFirebase();
    }
}

// PlayerScore is defined in AuthHandler.cs; keep a single shared definition there.