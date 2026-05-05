using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Firebase.Auth;
using Firebase;
using Firebase.Extensions;
using Firebase.Database;

public class AuthHandler : MonoBehaviour
{
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private FirebaseDatabase database;
    private string Username;
    
    // Flags de seguridad
    private bool firebaseReady = false;
    private bool isProcessing = false;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text usernameLabel;

    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject loadingAnim;

    public bool IsLoggedIn => currentUser != null && firebaseReady;
    public string UserId => (currentUser != null && firebaseReady) ? currentUser.UserId : "";
    public string CurrentUsername => Username ?? "";
    public bool FirebaseReady => firebaseReady;

    void Start()
    {
        firebaseReady = false;
        isProcessing = false;
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task == null)
            {
                Debug.LogError("Firebase CheckAndFixDependencies retorno null");
                StopLoading("Error critico en Firebase");
                return;
            }
            
            if (task.Result == DependencyStatus.Available)
            {
                try
                {
                    auth = FirebaseAuth.DefaultInstance;
                    database = FirebaseDatabase.DefaultInstance;
                    
                    if (auth == null || database == null)
                    {
                        Debug.LogError("Firebase Auth o Database es null despues de inicializar");
                        StopLoading("Error: Firebase no esta disponible");
                        firebaseReady = false;
                        return;
                    }
                    
                    currentUser = auth.CurrentUser;
                    firebaseReady = true;
                    isProcessing = false;
                    
                    Debug.Log("Firebase inicializado exitosamente");

                    if (currentUser != null)
                    {
                        Username = currentUser.DisplayName ?? PlayerPrefs.GetString("Username", currentUser.Email?.Split('@')[0] ?? "Jugador");
                        SetUIForUserLogged();
                    }
                    else
                    {
                        Logout();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Excepcion al inicializar Firebase: " + e.ToString());
                    StopLoading("Error en inicializacion");
                    firebaseReady = false;
                }
            }
            else
            {
                Debug.LogError("Firebase Error: " + task.Result);
                StopLoading("Error de configuracion Firebase");
                firebaseReady = false;
            }
        });
    }

    void StartLoading()
    {
        if (loadingAnim != null)
            loadingAnim.SetActive(true);
        if (statusText != null)
            statusText.text = "Cargando...";
    }

    void StopLoading(string message)
    {
        if (loadingAnim != null)
            loadingAnim.SetActive(false);
        if (statusText != null)
            statusText.text = message;
    }

    // =========================
    // LOGIN
    // =========================

    public void LoginButtonHandler()
    {
        if (!firebaseReady)
        {
            if (statusText != null)
                statusText.text = "Firebase no esta listo. Espere...";
            return;
        }

        if (isProcessing)
        {
            if (statusText != null)
                statusText.text = "Operacion en progreso...";
            return;
        }

        string email = emailInputField != null ? emailInputField.text : "";
        string password = passwordInputField != null ? passwordInputField.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (statusText != null)
                statusText.text = "Ingrese correo y contrasena";
            return;
        }

        StartCoroutine(LoginCoroutine(email, password));
    }

    IEnumerator LoginCoroutine(string email, string password)
    {
        isProcessing = true;
        StartLoading();

        if (auth == null)
        {
            StopLoading("Error: Firebase Auth no disponible");
            Debug.LogError("auth es null en LoginCoroutine");
            isProcessing = false;
            yield break;
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            isProcessing = false;

            FirebaseException fbEx = loginTask.Exception.GetBaseException() as FirebaseException;
            if (fbEx == null)
            {
                StopLoading("Error desconocido en login");
                Debug.LogError("Exception inesperada en login: " + loginTask.Exception.ToString());
                yield break;
            }

            string fbMsg = (fbEx.Message ?? "").ToLower();
            string errorMessage = "Error de login: ";

            if (fbMsg.Contains("invalid") && fbMsg.Contains("email") || fbMsg.Contains("invalid-email"))
                errorMessage += "Correo invalido";
            else if (fbMsg.Contains("wrong") && fbMsg.Contains("password") || fbMsg.Contains("wrong-password"))
                errorMessage += "Contrasena incorrecta";
            else if (fbMsg.Contains("user") && fbMsg.Contains("not") || fbMsg.Contains("user-not-found"))
                errorMessage += "Usuario no encontrado";
            else
                errorMessage += fbEx.Message ?? "Error desconocido";

            StopLoading(errorMessage);
            Debug.LogError("Error en login: " + fbEx.ToString());
            yield break;
        }

        // Use auth.CurrentUser as the source of truth
        currentUser = auth.CurrentUser;
        if (currentUser != null)
        {
            Username = currentUser.DisplayName ?? currentUser.Email?.Split('@')[0] ?? "Usuario";
            PlayerPrefs.SetString("Username", Username);
            PlayerPrefs.SetString("UserId", currentUser.UserId);

            StopLoading("Login exitoso");
            SetUIForUserLogged();
            
            Debug.Log("Login exitoso para usuario: " + Username);
        }
        else
        {
            StopLoading("Error: No se pudo completar el login");
            Debug.LogError("currentUser es null despues del login");
        }
        
        isProcessing = false;
    }

    // =========================
    // REGISTER
    // =========================

    public void RegisterButtonHandler()
    {
        if (!firebaseReady)
        {
            if (statusText != null)
                statusText.text = "Firebase no esta listo. Espere...";
            return;
        }

        if (isProcessing)
        {
            if (statusText != null)
                statusText.text = "Operacion en progreso...";
            return;
        }

        string username = usernameInputField != null ? usernameInputField.text : "";
        string email = emailInputField != null ? emailInputField.text : "";
        string password = passwordInputField != null ? passwordInputField.text : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (statusText != null)
                statusText.text = "Complete todos los campos";
            return;
        }

        StartCoroutine(RegisterCoroutine(username, email, password));
    }

    IEnumerator RegisterCoroutine(string username, string email, string password)
    {
        isProcessing = true;
        StartLoading();

        if (auth == null)
        {
            StopLoading("Error: Firebase Auth no disponible");
            Debug.LogError("auth es null en RegisterCoroutine");
            isProcessing = false;
            yield break;
        }

        var createTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => createTask.IsCompleted);

        if (createTask.Exception != null)
        {
            FirebaseException fbEx = createTask.Exception.GetBaseException() as FirebaseException;
            if (fbEx == null)
            {
                StopLoading("Error desconocido en registro");
                Debug.LogError("Exception inesperada en registro: " + createTask.Exception.ToString());
                isProcessing = false;
                yield break;
            }

            string fbMsg = (fbEx.Message ?? "").ToLower();
            string errorMessage = "Error de registro: ";

            if (fbMsg.Contains("invalid") && fbMsg.Contains("email") || fbMsg.Contains("invalid-email"))
                errorMessage += "Correo invalido";
            else if (fbMsg.Contains("email") && fbMsg.Contains("already") || fbMsg.Contains("email-already-in-use"))
                errorMessage += "El correo ya esta registrado";
            else if (fbMsg.Contains("weak") && fbMsg.Contains("password") || fbMsg.Contains("weak-password"))
                errorMessage += "Contrasena muy debil (minimo 6 caracteres)";
            else
                errorMessage += fbEx.Message ?? "Error desconocido";

            StopLoading(errorMessage);
            Debug.LogError("Error en registro: " + fbEx.ToString());
            isProcessing = false;
            yield break;
        }

        // Use auth.CurrentUser as the source of truth
        currentUser = auth.CurrentUser;

        if (currentUser == null)
        {
            isProcessing = false;
            StopLoading("Error: No se pudo crear el usuario");
            Debug.LogError("currentUser es null despues de CreateUserAsync (auth.CurrentUser)");
            yield break;
        }

        // Actualizar el DisplayName del usuario
        var userProfileUpdate = new UserProfile { DisplayName = username };
        var updateTask = currentUser.UpdateUserProfileAsync(userProfileUpdate);

        yield return new WaitUntil(() => updateTask.IsCompleted);
        if (updateTask.Exception != null)
        {
            Debug.LogError("Error al actualizar perfil: " + updateTask.Exception.ToString());
            StopLoading("Usuario creado pero con error en perfil");
            isProcessing = false;
            yield break;
        }

        // Guardar informacion del usuario en la base de datos
        yield return StartCoroutine(SaveUserToDatabase(currentUser.UserId, username, email));

        Username = username;
        PlayerPrefs.SetString("Username", Username);
        PlayerPrefs.SetString("UserId", currentUser.UserId);

        StopLoading("Usuario registrado correctamente");

        // Cargar leaderboard despues del registro
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.LoadLeaderboardFromFirebase();
        }
        else
        {
            Debug.LogWarning("ScoreManager no encontrado en escena");
        }

        SetUIForUserLogged();
        isProcessing = false;
    }

    private IEnumerator SaveUserToDatabase(string userId, string username, string email)
    {
        if (database == null)
        {
            Debug.LogError("database es null en SaveUserToDatabase");
            yield break;
        }

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
        {
            Debug.LogError("Parametros invalidos en SaveUserToDatabase");
            yield break;
        }

        var userDict = new Dictionary<string, object>
        {
            { "userId", userId },
            { "username", username },
            { "email", email },
            { "score", 0 }
        };

        var saveTask = database.RootReference
            .Child("users")
            .Child(userId)
            .SetValueAsync(userDict);

        yield return new WaitUntil(() => saveTask.IsCompleted);
        if (saveTask.Exception != null)
        {
            Debug.LogError("Error guardando usuario en BD: " + saveTask.Exception.ToString());
        }
        else
        {
            Debug.Log("Usuario guardado en Firebase Realtime Database: " + username);
        }
    }

    // =========================
    // PASSWORD RECOVERY
    // =========================

    public void SendPasswordResetEmail()
    {
        if (!firebaseReady)
        {
            if (statusText != null)
                statusText.text = "Firebase no esta listo";
            return;
        }

        if (isProcessing)
        {
            if (statusText != null)
                statusText.text = "Operacion en progreso...";
            return;
        }

        string email = emailInputField != null ? emailInputField.text : "";

        if (string.IsNullOrEmpty(email))
        {
            if (statusText != null)
                statusText.text = "Ingrese su correo";
            return;
        }

        StartCoroutine(SendPasswordResetCoroutine(email));
    }

    IEnumerator SendPasswordResetCoroutine(string email)
    {
        isProcessing = true;
        StartLoading();

        if (auth == null)
        {
            StopLoading("Error: Firebase Auth no disponible");
            Debug.LogError("auth es null en SendPasswordResetCoroutine");
            isProcessing = false;
            yield break;
        }

        var resetTask = auth.SendPasswordResetEmailAsync(email);
        yield return new WaitUntil(() => resetTask.IsCompleted);

        if (resetTask.Exception != null)
        {
            StopLoading("Error: Correo no encontrado o invalido");
            Debug.LogError("Error en password reset: " + resetTask.Exception.ToString());
        }
        else
        {
            StopLoading("Correo de recuperacion enviado a: " + email);
            Debug.Log("Password reset email sent successfully");
        }

        isProcessing = false;
    }

    // =========================
    // LOGOUT
    // =========================

    public void Logout()
    {
        try
        {
            if (auth != null)
            {
                auth.SignOut();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error en SignOut: " + e.ToString());
        }

        currentUser = null;
        Username = null;
        isProcessing = false;

        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("UserId");

        if (panelLogin != null)
            panelLogin.SetActive(true);
        
        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (statusText != null)
            statusText.text = "Sesion cerrada";
            
        Debug.Log("Usuario deslogueado exitosamente");
    }

    // =========================
    // SEND SCORE
    // =========================

    public void SendScore(int score)
    {
        if (!firebaseReady)
        {
            if (statusText != null)
                statusText.text = "Firebase no esta listo";
            return;
        }

        if (!IsLoggedIn)
        {
            if (statusText != null)
                statusText.text = "Debes iniciar sesion";
            return;
        }

        if (isProcessing)
        {
            if (statusText != null)
                statusText.text = "Operacion en progreso...";
            return;
        }

        if (score < 0)
        {
            Debug.LogWarning("Score negativo no permitido: " + score);
            return;
        }

        StartCoroutine(SendScoreCoroutine(score));
    }

    IEnumerator SendScoreCoroutine(int score)
    {
        isProcessing = true;
        StartLoading();

        if (currentUser == null)
        {
            StopLoading("Error: Usuario no autenticado");
            Debug.LogError("currentUser es null en SendScoreCoroutine");
            isProcessing = false;
            yield break;
        }

        if (database == null)
        {
            StopLoading("Error: Base de datos no disponible");
            Debug.LogError("database es null en SendScoreCoroutine");
            isProcessing = false;
            yield break;
        }

        if (string.IsNullOrEmpty(currentUser.UserId))
        {
            StopLoading("Error: UserId invalido");
            Debug.LogError("UserId invalido en SendScoreCoroutine");
            isProcessing = false;
            yield break;
        }

        var scoreDict = new Dictionary<string, object>
        {
            { "userId", currentUser.UserId },
            { "username", Username ?? "Usuario" },
            { "score", score },
            { "timestamp", System.DateTime.Now.Ticks }
        };

        // Generar scoreId de forma segura
        string scoreId = database.RootReference.Child("scores").Push().Key;
        if (string.IsNullOrEmpty(scoreId))
        {
            StopLoading("Error: No se pudo generar ID de score");
            Debug.LogError("scoreId es null en SendScoreCoroutine");
            isProcessing = false;
            yield break;
        }

        var saveTask = database.RootReference
            .Child("scores")
            .Child(currentUser.UserId)
            .Child(scoreId)
            .SetValueAsync(scoreDict);

        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.Exception != null)
        {
            StopLoading("Error enviando score");
            Debug.LogError("Error guardando score: " + saveTask.Exception.ToString());
            isProcessing = false;
            yield break;
        }

        // Tambien actualizar el mejor score en el perfil del usuario
        var getBestTask = database.RootReference
            .Child("users")
            .Child(currentUser.UserId)
            .Child("score")
            .GetValueAsync();

        yield return new WaitUntil(() => getBestTask.IsCompleted);

        int existingBest = 0;
        if (getBestTask.Result != null && getBestTask.Result.Exists && getBestTask.Result.Value != null)
        {
            string bestStr = getBestTask.Result.Value.ToString();
            int.TryParse(bestStr, out existingBest);
        }

        if (score > existingBest)
        {
            var updateTask = database.RootReference
                .Child("users")
                .Child(currentUser.UserId)
                .Child("score")
                .SetValueAsync(score);

            yield return new WaitUntil(() => updateTask.IsCompleted);

            if (updateTask.Exception != null)
            {
                Debug.LogError("Error actualizando score en usuario: " + updateTask.Exception.ToString());
            }
            else
            {
                Debug.Log("Best score actualizado: " + score + " (user: " + currentUser.UserId + ")");
            }
        }
        else
        {
            Debug.Log("Nuevo score " + score + " no supera best existente " + existingBest + ". No se actualiza.");
        }

        StopLoading("Score procesado: " + score);
        Debug.Log("Score procesado. UserID: " + currentUser.UserId + ", Score: " + score);

        isProcessing = false;
    }

    // =========================
    // UI
    // =========================

    void SetUIForUserLogged()
    {
        if (panelLogin != null)
            panelLogin.SetActive(false);
        
        if (gamePanel != null)
            gamePanel.SetActive(true);

        if (usernameLabel != null)
            usernameLabel.text = "Jugador: " + (Username ?? "Desconocido");
            
        Debug.Log("UI actualizada para usuario logueado: " + Username);
    }
}

// =========================
// DATA CLASSES
// =========================

[System.Serializable]
public class PlayerScore
{
    public string name;
    public int score;
    public string userId;
}
