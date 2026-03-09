using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform boardParent;
    [SerializeField] private int boardSize = 15;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button replayButton;
    [SerializeField] private FirebaseBootstrap firebaseBootstrap;
    [SerializeField] private bool debugOn;

    private Cell[,] cells;
    private string[,] board;

    private string playerSymbol = "";
    private string currentTurn = "";
    private string winner = "";
    private string matchStatus = "";

    private bool matchLoaded = false;

    private void Start()
    {
        CreateBoard();

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(OnReplayClicked);
        }

        SetStatus("Connecting to Firebase...");
    }

    private void CreateBoard()
    {
        if(cellPrefab == null)
        {
            if(debugOn) Debug.LogError("cellPrefab is null on GameManager");
            return;
        }

        if(boardParent == null)
        {
            if(debugOn) Debug.LogError("boardParent is null on Gamemanager");
            return;
        } 

        cells = new Cell[boardSize, boardSize];
        board = new string[boardSize, boardSize];

        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                Cell newCell = Instantiate(cellPrefab, boardParent, false);
                newCell.Init(row, col, this);

                cells[row, col] = newCell;
                board[row, col] = "";

                if (debugOn)
                {
                    Debug.Log($"Created cell {row},{col}");
                }
            }
        }
    }

    public void SetPlayerSymbol(string symbol)
    {
        playerSymbol = symbol;

        if (debugOn)
        {
            Debug.Log("Player symbol set to: " + playerSymbol);
        }

        UpdateStatusText();
    }

    public void ApplyMatchState(MatchState matchState)
    {
        if (matchState == null)
        {
            return;
        }

        currentTurn = matchState.currentTurn ?? "";
        winner = matchState.winner ?? "";
        matchStatus = matchState.status ?? "";

        ClearBoardVisuals();

        if (matchState.board != null)
        {
            foreach (KeyValuePair<string, string> entry in matchState.board)
            {
                string[] parts = entry.Key.Split('_');

                if (parts.Length != 2)
                {
                    continue;
                }

                if (!int.TryParse(parts[0], out int row))
                {
                    continue;
                }

                if (!int.TryParse(parts[1], out int col))
                {
                    continue;
                }

                if (row < 0 || row >= boardSize || col < 0 || col >= boardSize)
                {
                    continue;
                }

                string mark = entry.Value ?? "";
                board[row, col] = mark;
                cells[row, col].SetMark(mark);
            }
        }

        matchLoaded = true;

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(matchStatus == "finished");
        }

        UpdateStatusText();
        UpdateBoardInteractable();
    }

    public void TryPlaceMark(int row, int col)
    {
        if (!matchLoaded)
        {
            if (debugOn)
            {
                Debug.Log("Match not loaded yet.");
            }

            return;
        }

        if (firebaseBootstrap == null)
        {
            Debug.LogError("FirebaseBootstrap reference missing.");
            return;
        }

        if (matchStatus != "playing")
        {
            if (debugOn)
            {
                Debug.Log("Match is not in playing state.");
            }

            return;
        }

        if (winner != "")
        {
            if (debugOn)
            {
                Debug.Log("Match already has a winner.");
            }

            return;
        }

        if (currentTurn != playerSymbol)
        {
            if (debugOn)
            {
                Debug.Log("Not your turn.");
            }

            return;
        }

        if (board[row, col] != "")
        {
            if (debugOn)
            {
                Debug.Log("Cell already occupied.");
            }

            return;
        }

        firebaseBootstrap.TryPlaceMark(row, col);
    }

    private void UpdateStatusText()
    {
        if (!matchLoaded)
        {
            SetStatus("Loading match...");
            return;
        }

        if (winner != "")
        {
            SetStatus(winner == playerSymbol ? "You won!" : "You lost to your opponent!");
            return;
        }

        if (matchStatus == "waiting")
        {
            SetStatus("Waiting for opponent...");
            return;
        }

        if (matchStatus == "finished")
        {
            SetStatus(winner == playerSymbol ? "You won!" : "You lost to your opponent!");
            return;
        }

        SetStatus(currentTurn == playerSymbol ? "Your turn!" : "Waiting for opponent's turn!");
    }

    private void UpdateBoardInteractable()
    {
        bool canPlay = matchLoaded &&
                       matchStatus == "playing" &&
                       winner == "" &&
                       currentTurn == playerSymbol;

        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                bool isEmpty = board[row, col] == "";
                cells[row, col].SetInteractable(canPlay && isEmpty);
            }
        }
    }

    private void ClearBoardVisuals()
    {
        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                board[row, col] = "";
                cells[row, col].SetMark("");
            }
        }
    }

    private void OnReplayClicked()
    {
        if (firebaseBootstrap == null)
        {
            Debug.LogError("FirebaseBootstrap reference missing.");
            return;
        }

        firebaseBootstrap.RequestReplay();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}