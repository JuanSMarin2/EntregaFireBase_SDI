using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject scorePanel;

    [Header("References")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private AuthHandler authHandler;

    [Header("Score Settings")]
    [SerializeField] private int maxScore = 1000;
    [SerializeField] private int minScore = 10;
    [SerializeField] private float maxTime = 90f;

    [SerializeField] private ScoreManager scoreManager;

    private float timer;
    private bool running;

    private Vector3 playerStartPosition;

    void Start()
    {
        // Validar referencias
        if (player == null)
        {
            Debug.LogError("PlayerMovement no esta asignado en TimeManager");
        }

        if (authHandler == null)
        {
            Debug.LogError("AuthHandler no esta asignado en TimeManager");
        }

        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager no esta asignado en TimeManager");
        }

        if (timerText == null)
        {
            Debug.LogError("Timer Text no esta asignado en TimeManager");
        }

        if (scorePanel == null)
        {
            Debug.LogError("Score Panel no esta asignado en TimeManager");
        }

        if (player != null)
        {
            playerStartPosition = player.transform.position;
        }

        UpdateTimerUI();
    }

    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

 

    public void StartTimer()
    {
        timer = 0f;
        running = true;
    }



    public void FinishTimer()
    {
        running = false;

        if (authHandler == null)
        {
            Debug.LogError("AuthHandler no esta asignado en TimeManager");
            return;
        }

        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager no esta asignado en TimeManager");
            return;
        }

        int score = CalculateScore();
        Debug.Log("Score obtenido: " + score);

        // Enviar score a Firebase
        authHandler.SendScore(score);

        // Reset posición del jugador
        player.transform.position = playerStartPosition;
        player.canMove = false;

        // Mostrar panel de score
        scorePanel.SetActive(true);

        // Actualizar UI con score actual
        string username = PlayerPrefs.GetString("Username", "Player");
        scoreManager.SetCurrentScore(username, score);

        // Cargar leaderboard actualizado
        scoreManager.LoadLeaderboardFromFirebase();
        
        Debug.Log("Score enviado y leaderboard actualizado");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!running)
        {
            Debug.LogWarning("Trigger toqued pero el timer no esta activo");
            return;
        }

        Debug.Log("Trigger detectado, finalizando timer");
        FinishTimer();
    }



    int CalculateScore()
    {
        float t = Mathf.Clamp01(timer / maxTime);

        int score = Mathf.RoundToInt(Mathf.Lerp(maxScore, minScore, t));

        return score;
    }
}