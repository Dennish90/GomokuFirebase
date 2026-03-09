using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

public class FirebaseBootstrap : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI matchIDText;
    [SerializeField] private bool debugOn;

    private FirebaseAuth auth;
    private DatabaseReference dbRoot;
    private DatabaseReference currentMatchRef;
    private string currentMatchId;

    public FirebaseUser CurrentUser
    {
        get;
        private set;
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus status = task.Result;

            if (status != DependencyStatus.Available)
            {
                Debug.LogError("Firebase dependency error: " + status);

                if (statusText != null)
                {
                    statusText.text = "Firebase init failed.";
                }

                return;
            }

            auth = FirebaseAuth.DefaultInstance;
            dbRoot = FirebaseDatabase.DefaultInstance.RootReference;

            SignInAnonymously();
        });
    }

    private void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Anonymous sign-in failed: " + task.Exception);

                if (statusText != null)
                {
                    statusText.text = "Anonymous sign-in failed.";
                }

                return;
            }

            AuthResult result = task.Result;
            CurrentUser = result.User;

            if (debugOn)
            {
                Debug.Log("Anonymous sign-in success. UID: " + CurrentUser.UserId);
            }

            if (statusText != null)
            {
                statusText.text = "Signed in as: " + CurrentUser.UserId;
            }

            CreateTestMatch();
        });
    }

    private void CreateTestMatch()
    {
        if (CurrentUser == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        DatabaseReference matchRef = dbRoot.Child("matches").Push();

        string json = JsonUtility.ToJson(new MatchData
        {
            playerXUid = CurrentUser.UserId,
            playerOUid = "",
            currentTurn = "X",
            winner = "",
            status = "waiting"
        });

        matchRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Failed to create match: " + task.Exception);

                if (statusText != null)
                {
                    statusText.text = "Failed to create match.";
                }

                return;
            }

            currentMatchId = matchRef.Key;
            currentMatchRef = matchRef;

            currentMatchRef.ValueChanged += OnMatchValueChanged;

            if (debugOn)
            {
                Debug.Log("Created match: " + matchRef.Key);
            }

            if (statusText != null)
            {
                statusText.text = "Connected. Waiting for opponent...";
            }

            if(matchIDText != null) 
            {
                matchIDText.text = "Match ID: " + matchRef.Key;
            }
        });
    }

    public void TryPlaceMark(int row, int col)
    {
        Debug.Log($"TryPlaceMark called for row {row}, col {col}");
    }

    public void RequestReplay()
    {
        Debug.Log("RequestReplay called");
    }

    private void OnMatchValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Database error: " + args.DatabaseError.Message);
            return;
        }

        if (!args.Snapshot.Exists)
        {
            Debug.Log("Match snapshot does not exist.");
            return;
        }

        if (debugOn)
        {
            Debug.Log("Match updated: " + args.Snapshot.GetRawJsonValue());
        }

        UpdateStatusText(args);
    }

    public void UpdateStatusText(ValueChangedEventArgs args)
    {
        object statusValue = args.Snapshot.Child("status").Value;
        string status = statusValue != null ? statusValue.ToString() : "";

        if(statusText != null)
        {
            if(status == "waiting")
            {
                statusText.text = "Connected. Waiting for opponent.";
            }
            else if(status == "playing")
            {
               statusText.text = "Playing";
            }
            else if(status == "finished")
            {
                statusText.text = "Game over!";
            }
            else
            {
                statusText.text = "Unknon match status";
            }
        }
    } 
}

[System.Serializable]
public class MatchData
{
    public string playerXUid;
    public string playerOUid;
    public string currentTurn;
    public string winner;
    public string status;
}