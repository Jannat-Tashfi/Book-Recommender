
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TicTacToe : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject gamePanel;

    [Header("Start Menu")]
    public Button startButton;
    public TMP_Dropdown difficultyDropdown;
    public TMP_Text titleText;

    [Header("Game Buttons")]
    public Button restartButton;
    public Button backButton;

    [Header("Result Window")]
    public GameObject resultPanel;
    public TMP_Text resultMessage;
    public Button resultRestartButton;
    public Button resultBackButton;

    [Header("Game Text")]
    public TMP_Text turnText;
    public TMP_Text scoreText;

    [Header("Board Cells - Left to Right, Top to Bottom")]
    public Button[] buttons = new Button[9];
    public TMP_Text[] cellTexts = new TMP_Text[9];

    [Header("Winning Line")]
    public RectTransform winningLine;

    [Header("Settings")]
    public float aiDelay = 0.5f;
    public int winningScore = 3;

    private string[] cells = new string[9];

    private readonly int[,] winPatterns =
    {
        { 0, 1, 2 },
        { 3, 4, 5 },
        { 6, 7, 8 },
        { 0, 3, 6 },
        { 1, 4, 7 },
        { 2, 5, 8 },
        { 0, 4, 8 },
        { 2, 4, 6 }
    };

    private int xScore;
    private int oScore;

    // Human plays O; AI plays X.
    private string currentPlayer = "O";

    private bool gameOver = true;
    private bool matchOver = false;
    private bool aiThinking = false;

    private Coroutine aiRoutine;

    // -----------------------------------------
    // INITIAL SETUP
    // -----------------------------------------

    private void Start()
    {
        if (!ValidateBoard())
        {
            enabled = false;
            return;
        }

        // Connect menu buttons.
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartRound);
            restartButton.onClick.AddListener(RestartRound);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackToMenu);
            backButton.onClick.AddListener(BackToMenu);
        }

        // Connect result-window buttons.
        if (resultRestartButton != null)
        {
            resultRestartButton.onClick.RemoveListener(
                RestartFromResult);

            resultRestartButton.onClick.AddListener(
                RestartFromResult);
        }

        if (resultBackButton != null)
        {
            resultBackButton.onClick.RemoveListener(
                BackFromResult);

            resultBackButton.onClick.AddListener(
                BackFromResult);
        }

        // Connect all nine board buttons.
        for (int i = 0; i < 9; i++)
        {
            int index = i;

            buttons[i].onClick.RemoveListener(
                () => PlayerMove(index));

            buttons[i].onClick.AddListener(
                () => PlayerMove(index));
        }

        xScore = 0;
        oScore = 0;

        UpdateScore();

        if (winningLine != null)
            winningLine.gameObject.SetActive(false);

        // Show Start Menu at launch.
        if (startPanel != null)
            startPanel.SetActive(true);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        Debug.Log("Tic Tac Toe initialized.");
    }

    private bool ValidateBoard()
    {
        if (buttons == null || buttons.Length != 9 ||
            cellTexts == null || cellTexts.Length != 9)
        {
            Debug.LogError(
                "Assign exactly 9 Buttons and 9 TMP cell texts.");
            return false;
        }

        for (int i = 0; i < 9; i++)
        {
            if (buttons[i] == null || cellTexts[i] == null)
            {
                Debug.LogError(
                    "Missing Button or TMP Text at board cell " + i);
                return false;
            }
        }

        return true;
    }

    // -----------------------------------------
    // START GAME
    // -----------------------------------------

    public void StartGame()
    {
        StopAITurn();

        xScore = 0;
        oScore = 0;
        matchOver = false;

        UpdateScore();

        if (startPanel != null)
            startPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(true);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        ResetBoard();

        Debug.Log("Game started.");
    }

    // -----------------------------------------
    // BACK TO START MENU
    // -----------------------------------------

    public void BackToMenu()
    {
        StopAITurn();

        gameOver = true;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (startPanel != null)
            startPanel.SetActive(true);
    }

    public void BackFromResult()
    {
        BackToMenu();
    }

    // -----------------------------------------
    // RESTART
    // -----------------------------------------

    public void RestartRound()
    {
        StopAITurn();

        // Start a new match if the previous match ended.
        if (matchOver)
        {
            xScore = 0;
            oScore = 0;
        }

        matchOver = false;

        UpdateScore();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(true);

        ResetBoard();
    }

    public void RestartFromResult()
    {
        RestartRound();
    }

    private void ResetBoard()
    {
        StopAITurn();

        gameOver = false;
        matchOver = false;
        aiThinking = false;
        currentPlayer = "O";

        for (int i = 0; i < 9; i++)
        {
            cells[i] = "";

            cellTexts[i].text = "";
            buttons[i].interactable = true;
        }

        if (winningLine != null)
            winningLine.gameObject.SetActive(false);

        if (resultMessage != null)
            resultMessage.text = "";

        UpdateTurn();

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // -----------------------------------------
    // HUMAN PLAYER: O
    // -----------------------------------------

    private void PlayerMove(int index)
    {
        if (gameOver || matchOver || aiThinking)
            return;

        if (index < 0 || index >= 9)
            return;

        if (!string.IsNullOrEmpty(cells[index]))
            return;

        PlaceMove(index, "O");

        if (CheckGameEnd("O"))
            return;

        currentPlayer = "X";
        UpdateTurn();

        aiRoutine = StartCoroutine(AITurn());
    }

    // -----------------------------------------
    // AI TURN
    // -----------------------------------------

    private IEnumerator AITurn()
    {
        aiThinking = true;

        yield return new WaitForSeconds(aiDelay);

        if (!gameOver && !matchOver)
        {
            int level = difficultyDropdown != null
                ? difficultyDropdown.value
                : 0;

            int move;

            switch (level)
            {
                case 0:
                    // Beginner: random legal move.
                    move = FindRandomMove();
                    break;

                case 1:
                    // Middle: win, block, and prefer good cells.
                    move = FindMiddleMove();
                    break;

                case 2:
                    // Advanced: optimal minimax strategy.
                    move = FindAdvancedMove();
                    break;

                default:
                    move = FindRandomMove();
                    break;
            }

            if (move >= 0)
            {
                PlaceMove(move, "X");

                if (!CheckGameEnd("X"))
                {
                    currentPlayer = "O";
                    UpdateTurn();
                }
            }
        }

        aiThinking = false;
        aiRoutine = null;
    }

    // -----------------------------------------
    // BEGINNER AI
    // -----------------------------------------

    private int FindRandomMove()
    {
        List<int> available = new List<int>();

        for (int i = 0; i < 9; i++)
        {
            if (string.IsNullOrEmpty(cells[i]))
                available.Add(i);
        }

        if (available.Count == 0)
            return -1;

        return available[Random.Range(0, available.Count)];
    }

    // -----------------------------------------
    // MIDDLE AI
    // -----------------------------------------

    private int FindMiddleMove()
    {
        // Win if possible.
        int move = FindWinningMove("X");

        if (move >= 0)
            return move;

        // Block O from winning.
        move = FindWinningMove("O");

        if (move >= 0)
            return move;

        // Prefer center.
        if (string.IsNullOrEmpty(cells[4]))
            return 4;

        // Prefer corners.
        int[] corners = { 0, 2, 6, 8 };

        foreach (int i in corners)
        {
            if (string.IsNullOrEmpty(cells[i]))
                return i;
        }

        // Then prefer sides.
        int[] sides = { 1, 3, 5, 7 };

        foreach (int i in sides)
        {
            if (string.IsNullOrEmpty(cells[i]))
                return i;
        }

        return -1;
    }

    private int FindWinningMove(string player)
    {
        for (int i = 0; i < 9; i++)
        {
            if (!string.IsNullOrEmpty(cells[i]))
                continue;

            cells[i] = player;

            bool wins = HasWinner(player);

            cells[i] = "";

            if (wins)
                return i;
        }

        return -1;
    }

    // -----------------------------------------
    // ADVANCED AI: MINIMAX
    // -----------------------------------------

    private int FindAdvancedMove()
    {
        int bestMove = -1;
        int bestScore = int.MinValue;

        // Center first helps make equally good moves consistent.
        int[] order = { 4, 0, 2, 6, 8, 1, 3, 5, 7 };

        foreach (int i in order)
        {
            if (!string.IsNullOrEmpty(cells[i]))
                continue;

            cells[i] = "X";

            int score = Minimax(false, 1);

            cells[i] = "";

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = i;
            }
        }

        return bestMove;
    }

    private int Minimax(bool maximizing, int depth)
    {
        if (HasWinner("X"))
            return 10 - depth;

        if (HasWinner("O"))
            return depth - 10;

        bool full = true;

        foreach (string cell in cells)
        {
            if (string.IsNullOrEmpty(cell))
            {
                full = false;
                break;
            }
        }

        if (full)
            return 0;

        if (maximizing)
        {
            int bestScore = int.MinValue;

            for (int i = 0; i < 9; i++)
            {
                if (!string.IsNullOrEmpty(cells[i]))
                    continue;

                cells[i] = "X";

                int score = Minimax(false, depth + 1);

                cells[i] = "";

                bestScore = Mathf.Max(bestScore, score);
            }

            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;

            for (int i = 0; i < 9; i++)
            {
                if (!string.IsNullOrEmpty(cells[i]))
                    continue;

                cells[i] = "O";

                int score = Minimax(true, depth + 1);

                cells[i] = "";

                bestScore = Mathf.Min(bestScore, score);
            }

            return bestScore;
        }
    }

    // -----------------------------------------
    // PLACE A MOVE
    // -----------------------------------------

    private void PlaceMove(int index, string player)
    {
        cells[index] = player;

        cellTexts[index].text = player;

        cellTexts[index].color = player == "O"
            ? new Color(0.25f, 0.65f, 1f)
            : new Color(1f, 0.25f, 0.3f);
    }

    // -----------------------------------------
    // CHECK WIN OR DRAW
    // -----------------------------------------

    private bool CheckGameEnd(string player)
    {
        int[] winningCells = GetWinningLine(player);

        if (winningCells != null)
        {
            gameOver = true;

            DrawWinningLine(
                winningCells[0],
                winningCells[2]);

            if (player == "O")
                oScore++;
            else
                xScore++;

            UpdateScore();
            DisableBoard();

            bool wonMatch =
                xScore >= winningScore ||
                oScore >= winningScore;

            matchOver = wonMatch;

            if (resultMessage != null)
            {
                if (wonMatch)
                {
                    resultMessage.text =
                        player + " WINS THE MATCH!";
                }
                else
                {
                    resultMessage.text =
                        player + " WINS THE ROUND!";
                }
            }

            ShowResultPanel();

            Debug.Log(player + " wins!");

            return true;
        }

        bool full = true;

        foreach (string cell in cells)
        {
            if (string.IsNullOrEmpty(cell))
            {
                full = false;
                break;
            }
        }

        if (full)
        {
            gameOver = true;

            DisableBoard();

            if (resultMessage != null)
                resultMessage.text = "IT'S A DRAW!";

            ShowResultPanel();

            Debug.Log("Draw!");

            return true;
        }

        return false;
    }

    private void ShowResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            // Keep the result window above the board.
            resultPanel.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning(
                "Assign ResultRestartPanel to Result Panel in the Inspector.");
        }
    }

    private void DisableBoard()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.interactable = false;
        }
    }

    private bool HasWinner(string player)
    {
        return GetWinningLine(player) != null;
    }

    private int[] GetWinningLine(string player)
    {
        for (int i = 0; i < winPatterns.GetLength(0); i++)
        {
            int a = winPatterns[i, 0];
            int b = winPatterns[i, 1];
            int c = winPatterns[i, 2];

            if (cells[a] == player &&
                cells[b] == player &&
                cells[c] == player)
            {
                return new int[] { a, b, c };
            }
        }

        return null;
    }

    // -----------------------------------------
    // DRAW THE WINNING LINE
    // -----------------------------------------

    private void DrawWinningLine(int first, int last)
    {
        if (winningLine == null)
            return;

        RectTransform firstRect =
            buttons[first].GetComponent<RectTransform>();

        RectTransform lastRect =
            buttons[last].GetComponent<RectTransform>();

        Transform lineParent = winningLine.parent;

        if (lineParent == null)
            return;

        Vector3 firstWorld =
            firstRect.TransformPoint(firstRect.rect.center);

        Vector3 lastWorld =
            lastRect.TransformPoint(lastRect.rect.center);

        Vector2 start =
            lineParent.InverseTransformPoint(firstWorld);

        Vector2 end =
            lineParent.InverseTransformPoint(lastWorld);

        Vector2 direction = end - start;

        winningLine.anchoredPosition = (start + end) / 2f;

        winningLine.sizeDelta =
            new Vector2(direction.magnitude, 12f);

        winningLine.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg);

        winningLine.gameObject.SetActive(true);
        winningLine.SetAsLastSibling();
    }

    // -----------------------------------------
    // UPDATE SCORE AND TURN
    // -----------------------------------------

    private void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "O: " + oScore + "     X: " + xScore;
        }
    }

    private void UpdateTurn()
    {
        if (turnText != null)
            turnText.text = currentPlayer + "'s TURN";
    }

    // -----------------------------------------
    // STOP AI COROUTINE
    // -----------------------------------------

    private void StopAITurn()
    {
        if (aiRoutine != null)
        {
            StopCoroutine(aiRoutine);
            aiRoutine = null;
        }

        aiThinking = false;
    }
}
