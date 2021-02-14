using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Canvas screenCanvas;

    [Header("Prefab References")]
    [SerializeField] private Text screenSymbolPrefab;
    [SerializeField] private BoxCollider2D mapColliderPrefab;
    [SerializeField] private Transform playerPrefab;

    [Header("Map References")]
    [SerializeField] private TextAsset mapTextAsset;

    // Screen.
    private readonly int screenWidth = 64;
    private readonly int screenHeight = 32;
    private readonly int screenOffset = 8;
    private readonly string screenBlock = "■";
    private readonly string screenBlockTop = "▦";
    private readonly string screenBlockBottom = "▩";
    private readonly string screenEmpty = string.Empty;
    private readonly float distanceMultiplier = 1.3f;

    // Raycast.
    private readonly float raycastMaxDistance = 30f;
    private readonly float raycastSpread = 0.2f;
    private readonly float raycastLength = 10f;

    // Map.
    private readonly int mapWidth = 32;
    private readonly int mapHeight = 32;
    private readonly string mapFloorSymbol = ".";
    private readonly string mapWallSymbol = "#";

    // Player.
    private readonly float playerSpeed = 3f;
    private readonly float playerTurnSpeed = 90f;
    private readonly float playerStartX = 16f;
    private readonly float playerStartY = 8f;

    // Runtime.
    private Dictionary<Position, Text> screenSymbols = new Dictionary<Position, Text>();
    private Dictionary<int, RaycastHit2D> raycastHits = new Dictionary<int, RaycastHit2D>();
    private List<BoxCollider2D> mapColliders = new List<BoxCollider2D>();
    private Transform playerTransform;
    private float playerAngle = 0f;

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
        playerTransform = Instantiate(playerPrefab, new Vector2(playerStartX - 0.5f, playerStartY - 0.5f), Quaternion.identity, transform);
    }

    private void GenerateMapColliders()
    {
        var map = MapTextAssetToList(mapTextAsset);

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                string symbol = map[j][i].ToString();

                if (symbol == mapFloorSymbol)
                {
                    continue;
                }
                else if (symbol == mapWallSymbol)
                {
                    var boxCollider = Instantiate(mapColliderPrefab, new Vector2(i, j), Quaternion.identity, transform) as BoxCollider2D;

                    mapColliders.Add(boxCollider);
                }
            }
        }
    }

    private void GenerateScreen()
    {
        for (int i = 0; i < screenWidth; i++)
        {
            raycastHits.Add(i, new RaycastHit2D());

            for (int j = 0; j < screenHeight; j++)
            {
                var symbol = Instantiate(screenSymbolPrefab, screenCanvas.transform) as Text;

                var textRect = symbol.GetComponent<RectTransform>();
                textRect.anchoredPosition = new Vector2(i * screenOffset - (screenWidth * screenOffset / 2) + (screenOffset / 2), j * screenOffset - (screenHeight * screenOffset / 2) + (screenOffset / 2));

                screenSymbols.Add(new Position(i, j), symbol);
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

            if (GetRaycastDistance(true) < 0.5f)
            {
                playerTransform.position -= playerTransform.up * playerSpeed * Time.deltaTime;
            }
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            playerTransform.position -= playerTransform.up * playerSpeed * Time.deltaTime;

            if (GetRaycastDistance(false) < 0.5f)
            {
                playerTransform.position += playerTransform.up * playerSpeed * Time.deltaTime;
            }
        }
    }

    private void UpdateRaycasts()
    {
        for (int i = 0; i < screenWidth; i++)
        {
            var direction = playerTransform.TransformDirection(new Vector2((i - (screenWidth / 2f)) * raycastSpread, raycastLength));

            var hit = Physics2D.Raycast(playerTransform.position, direction, raycastMaxDistance);

            if (hit.collider != null)
            {
                raycastHits[i] = hit;
            }
        }
    }

    private void UpdateScreen()
    {
        for (int i = 0; i < screenWidth; i++)
        {
            float distance = raycastHits[i].distance;

            float height = screenHeight - (distance * distanceMultiplier);
            int verticalOffset = Mathf.RoundToInt((screenHeight - height) / 2f);

            string symbol = screenBlock;
            float depth = height / screenHeight;
            var color = new Color(depth, depth, depth);

            if (raycastHits[i].normal.x >= 1f || raycastHits[i].normal.x <= -1f)
            {
                color = new Color(depth * 0.75f, depth * 0.75f, depth * 0.75f);
            }

            for (int j = 0; j < screenHeight; j++)
            {
                var screenSymbol = screenSymbols[new Position(i, j)];

                if (j < verticalOffset)
                {
                    float floorColor = (verticalOffset - j) / (float)verticalOffset;

                    screenSymbol.text = symbol;
                    screenSymbol.color = new Color(floorColor * 0.4f, floorColor * 0.4f, floorColor * 0.4f);

                    if (!screenSymbol.enabled)
                    {
                        screenSymbol.enabled = true;
                    }
                }
                else if (j > screenHeight - verticalOffset)
                {
                    if (screenSymbol.enabled)
                    {
                        screenSymbol.enabled = false;
                    }
                }
                else
                {
                    if (j == screenHeight - verticalOffset)
                    {
                        symbol = screenBlockTop;
                    }
                    else if (j == verticalOffset)
                    {
                        symbol = screenBlockBottom;
                    }
                    else
                    {
                        symbol = screenBlock;
                    }

                    screenSymbol.text = symbol;
                    screenSymbol.color = color;

                    if (!screenSymbol.enabled)
                    {
                        screenSymbol.enabled = true;
                    }
                }
            }
        }
    }

    private float GetRaycastDistance(bool isForward = true)
    {
        float distance = raycastMaxDistance;

        var direction = isForward ? playerTransform.up : -playerTransform.up;

        var hit = Physics2D.Raycast(playerTransform.position, direction, raycastMaxDistance);

        if (hit.collider != null)
        {
            distance = hit.distance;
        }

        return distance;
    }

    private List<string> MapTextAssetToList(TextAsset mapTextAsset)
    {
        var list = new List<string>();
        var lineArray = mapTextAsset.text.Split('\n');
        foreach (var line in lineArray)
        {
            list.Add(line);
        }
        return list;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && playerTransform != null)
        {
            Gizmos.DrawSphere(playerTransform.position, 0.5f);

            for (int i = 0; i < screenWidth; i++)
            {
                var direction = playerTransform.TransformDirection(new Vector2((i - (screenWidth / 2f)) * raycastSpread, raycastLength));

                Gizmos.DrawRay(playerTransform.position, direction);
            }
        }
    }
}