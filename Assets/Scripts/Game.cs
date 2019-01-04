using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct Position
{
    public int X { get; }
    public int Y { get; }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class Game : MonoBehaviour
{
    // References.
    public Canvas ScreenCanvas;
    public Text ScreenSymbolPrefab;
    public BoxCollider2D MapColliderPrefab;
    public Transform PlayerPrefab;

    // Screen.
    private readonly int screenWidth = 64;
    private readonly int screenHeight = 32;
    private readonly int screenOffset = 16;

    private Dictionary<Position, Text> screenSymbols = new Dictionary<Position, Text>();

    // Raycast.
    private readonly float raycastMaxDistance = 30f;
    private readonly float raycastSpread = 0.2f;
    private readonly float raycastLength = 10f;

    private Dictionary<int, float> raycastDistances = new Dictionary<int, float>();

    // Map.
    private readonly int mapWidth = 32;
    private readonly int mapHeight = 32;
    private readonly List<string> map = new List<string>()
    {
        "################################",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#......##..............##......#",
        "#......##..............##......#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "##############....##############",
        "##############....##############",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#.............####.............#",
        "#.............####.............#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "#..............................#",
        "################################",
    };

    private List<BoxCollider2D> mapColliders = new List<BoxCollider2D>();

    // Player.
    private readonly float playerSpeed = 5f;
    private readonly float playerTurnSpeed = 90f;
    private readonly float playerStartX = 16f;
    private readonly float playerStartY = 8f;

    private float playerAngle = 0f;
    private Transform playerTransform;

    private void Start()
    {
        GeneratePlayer();
        GenerateMapColliders();
        GenerateScreen();
    }

    private void Update()
    {
        UpdateInput();
        UpdateRaycasts();
        UpdateScreen();
    }

    private void GeneratePlayer()
    {
        playerTransform = Instantiate(PlayerPrefab, new Vector2(playerStartX - (1 / 2f), playerStartY - (1 / 2f)), Quaternion.identity, transform);
    }

    private void GenerateMapColliders()
    {
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                string symbol = map[j][i].ToString();

                if (symbol == ".")
                {
                    continue;
                }
                else if (symbol == "#")
                {
                    var boxCollider = Instantiate(MapColliderPrefab, new Vector2(i, j), Quaternion.identity, transform) as BoxCollider2D;

                    mapColliders.Add(boxCollider);
                }
            }
        }
    }

    private void GenerateScreen()
    {
        for (int i = 0; i < screenWidth; i++)
        {
            raycastDistances.Add(i, 0f);

            for (int j = 0; j < screenHeight; j++)
            {
                var text = Instantiate(ScreenSymbolPrefab, ScreenCanvas.transform) as Text;

                var textRect = text.GetComponent<RectTransform>();
                textRect.anchoredPosition = new Vector2(i * screenOffset - (screenWidth * screenOffset / 2) + (screenOffset / 2), j * screenOffset - (screenHeight * screenOffset / 2) + (screenOffset / 2));

                screenSymbols.Add(new Position(i, j), text);
            }
        }
    }

    private void UpdateInput()
    {
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            playerAngle += playerTurnSpeed * Time.deltaTime;

            if (playerAngle > 360f)
            {
                playerAngle = 0f;
            }

            playerTransform.localRotation = Quaternion.Euler(0f, 0f, playerAngle);
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            playerAngle -= playerTurnSpeed * Time.deltaTime;

            if (playerAngle < 0f)
            {
                playerAngle = 360f;
            }

            playerTransform.localRotation = Quaternion.Euler(0f, 0f, playerAngle);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            playerTransform.position += playerTransform.up * playerSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            playerTransform.position -= playerTransform.up * playerSpeed * Time.deltaTime;
        }
    }

    private void UpdateRaycasts()
    {
        for (int i = 0; i < screenWidth; i++)
        {
            var direction = playerTransform.TransformDirection(new Vector2((i - (screenWidth / 2)) * raycastSpread, raycastLength));

            var hit = Physics2D.Raycast(playerTransform.position, direction, raycastMaxDistance);

            if (hit.collider != null)
            {
                raycastDistances[i] = hit.distance;
            }
        }
    }

    private void UpdateScreen()
    {
        for (int i = 0; i < screenWidth; i++)
        {
            float distance = raycastDistances[i];

            string symbol = string.Empty;
            Color color = Color.clear;

            int height = Mathf.RoundToInt(screenHeight - distance);
            int verticalOffset = (screenHeight - height) / 2;

            if (distance < raycastMaxDistance)
            {
                symbol = "#";
                float depth = height / (float)screenHeight;
                color = new Color(depth, depth, depth);
            }

            for (int j = 0; j < screenHeight; j++)
            {
                var screenSymbol = screenSymbols[new Position(i, j)];

                if (j < verticalOffset)
                {
                    screenSymbol.text = string.Empty;
                    screenSymbol.color = Color.clear;
                }
                else if (j > screenHeight - verticalOffset)
                {
                    screenSymbol.text = string.Empty;
                    screenSymbol.color = Color.clear;
                }
                else
                {
                    screenSymbol.text = symbol;
                    screenSymbol.color = color;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && playerTransform != null)
        {
            Gizmos.DrawSphere(playerTransform.position, 1f);

            for (int i = 0; i < screenWidth; i++)
            {
                var direction = playerTransform.TransformDirection(new Vector2((i - (screenWidth / 2)) * raycastSpread, raycastLength));

                Gizmos.DrawRay(playerTransform.position, direction);
            }
        }
    }
}