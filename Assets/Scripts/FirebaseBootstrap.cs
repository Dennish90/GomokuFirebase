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
    private string playerSymbol = "";

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

            TryJoinOrCreateMatch();
        });
    }

    private void TryJoinOrCreateMatch()
    {
        dbRoot.Child("matches").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Failed to read matches: " + task.Exception);
                CreateTestMatch();
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (!snapshot.Exists)
            {
                CreateTestMatch();
                return;
            }

            foreach (DataSnapshot matchSnapshot in snapshot.Children)
            {
                string status = matchSnapshot.Child("status").Value?.ToString() ?? "";
                string playerXUid = matchSnapshot.Child("playerXUid").Value?.ToString() ?? "";
                string playerOUid = matchSnapshot.Child("playerOUid").Value?.ToString() ?? "";

                if (status == "waiting" &&
                    playerXUid != CurrentUser.UserId &&
                    string.IsNullOrEmpty(playerOUid))
                {
                    JoinMatch(matchSnapshot.Key);
                    return;
                }
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
        playerSymbol = "X";

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

    private void JoinMatch(string matchId)
    {
        currentMatchId = matchId;
        currentMatchRef = dbRoot.Child("matches").Child(matchId);
        playerSymbol = "O";

        currentMatchRef.Child("playerOUid").SetValueAsync(CurrentUser.UserId).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Failed to join match: " + task.Exception);
                return;
            }

            currentMatchRef.Child("status").SetValueAsync("playing").ContinueWithOnMainThread(statusTask =>
            {
                if (statusTask.IsCanceled || statusTask.IsFaulted)
                {
                    Debug.LogError("Failed to set match to playing: " + statusTask.Exception);
                    return;
                }

                currentMatchRef.ValueChanged -= OnMatchValueChanged;
                currentMatchRef.ValueChanged += OnMatchValueChanged;

                if (debugOn)
                {
                    Debug.Log("Joined match: " + currentMatchId);
                }

                if (statusText != null)
                {
                    statusText.text = "Joined match. Waiting for sync...";
                }

                if (matchIDText != null)
                {
                    matchIDText.text = "Match ID: " + currentMatchId;
                }
            });
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

    private void UpdateStatusText(ValueChangedEventArgs args)
    {
        string status = args.Snapshot.Child("status").Value?.ToString() ?? "";
        string currentTurn = args.Snapshot.Child("currentTurn").Value?.ToString() ?? "";
        string playerXUid = args.Snapshot.Child("playerXUid").Value?.ToString() ?? "";
        string playerOUid = args.Snapshot.Child("playerOUid").Value?.ToString() ?? "";

        string myUid = CurrentUser != null ? CurrentUser.UserId : "";

        if (playerXUid == myUid)
        {
            playerSymbol = "X";
        }
        else if (playerOUid == myUid)
        {
            playerSymbol = "O";
        }
        else
        {
            playerSymbol = "";
        }

        if (statusText == null)
        {
            return;
        }

        if (status == "waiting")
        {
            statusText.text = "Connected. Waiting for opponent...";
        }
        else if (status == "playing")
        {
            if (currentTurn == playerSymbol)
            {
                statusText.text = "Your turn! [" + playerSymbol + "]";
            }
            else
            {
                statusText.text = "Opponent's turn.";
            }
        }
        else if (status == "finished")
        {
            statusText.text = "Game over!";
        }
        else
        {
            statusText.text = "Unknown match status.";
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