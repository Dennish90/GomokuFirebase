using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    private int row;
    private int col;
    private GameManager gameManager;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void Init(int row, int col, GameManager gameManager)
    {
        this.row = row;
        this.col = col;
        this.gameManager = gameManager;

        if (button == null)
        {
            Debug.LogError("Cell button is null.");
            return;
        }

        if (label == null)
        {
            Debug.LogError("Cell label is null.");
            return;
        }

        if (this.gameManager == null)
        {
            Debug.LogError("Cell gameManager is null.");
            return;
        }

        label.text = "";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        gameManager.TryPlaceMark(row, col);
    }

    public void SetMark(string mark)
    {
        label.text = mark;
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }
}