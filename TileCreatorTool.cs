using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

public class TileCreatorTool : EditorWindow
{
    // Safe Area ������Ʈ ����
    public GameObject safeAreaObject;
    private Rect safeArea;

    // Ÿ�� ũ��� �׸��� ����
    private Vector2 tileSize = new Vector2(130, 130);
    private Dictionary<int, List<Rect>> floorGridRects = new Dictionary<int, List<Rect>>(); // �� ���� �׸��� ����
    private Dictionary<int, List<int>> selectedTileIndicesByFloor = new Dictionary<int, List<int>>(); // �� ���� ���õ� Ÿ�� �ε��� ����

    // Mahjong_GM�� �ִ� stageDatas ����Ʈ�� ������ �� �ֵ���
    private Mahjong_GM mahjongGM;

    // ����� �׸��� ��ġ�� ������ ����Ʈ
    private Dictionary<string, List<Vector2>> savedGridData = new Dictionary<string, List<Vector2>>();

    // ���� ���� �̸� �� �������� �̸�
    private string stageName = "stage1";
    private bool[] floorToggles = new bool[3]; // 1, 2, 3�� ��� ����
    private bool gridGenerated = false; // �׸��尡 �����Ǿ����� ����

    [MenuItem("Tools/Tile Creator Tool")]
    public static void ShowWindow()
    {
        GetWindow<TileCreatorTool>("Tile Creator Tool");
    }

    private void OnGUI()
    {
        // Safe Area ������Ʈ ����
        GUILayout.Label("Safe Area Settings", EditorStyles.boldLabel);
        safeAreaObject = (GameObject)EditorGUILayout.ObjectField("Safe Area Object", safeAreaObject, typeof(GameObject), true);

        // �������� �̸� ����
        GUILayout.Label("Save File Settings", EditorStyles.boldLabel);
        stageName = EditorGUILayout.TextField("Stage Name", stageName);

        // Mahjong_GM ������Ʈ ����
        GUILayout.Label("Mahjong GM Reference", EditorStyles.boldLabel);
        mahjongGM = (Mahjong_GM)EditorGUILayout.ObjectField("Mahjong GM", mahjongGM, typeof(Mahjong_GM), true);

        // �� ���� ��� ��ư (�׸��� ���� �� ���̱�/����⸸ ����)
        GUILayout.Label("Floor Selection", EditorStyles.boldLabel);
        for (int i = 0; i < floorToggles.Length; i++)
        {
            floorToggles[i] = EditorGUILayout.Toggle("Floor " + (i + 1), floorToggles[i]);
        }

        // ���õ� Ÿ�� ���� ǥ��
        GUILayout.Label("Selected Tiles: " + GetTotalSelectedTilesCount(), EditorStyles.boldLabel);

        // �׸��� ���� ��ư
        if (GUILayout.Button("Generate Grid") && safeAreaObject != null)
        {
            GenerateGrid();
            gridGenerated = true;
        }

        // Save ��ư
        if (GUILayout.Button("Save Grid") && mahjongGM != null)
        {
            SaveGrid();
        }
    }

    private void GenerateGrid()
    {
        floorGridRects.Clear();
        selectedTileIndicesByFloor.Clear();

        // Safe Area ������Ʈ�� RectTransform�� ����Ͽ� ���� ����
        if (safeAreaObject == null)
        {
            Debug.LogError("Safe Area Object is not assigned.");
            return;
        }
        RectTransform safeAreaTransform = safeAreaObject.GetComponent<RectTransform>();
        if (safeAreaTransform != null)
        {
            Vector3[] corners = new Vector3[4];
            safeAreaTransform.GetWorldCorners(corners);
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];
            safeArea = new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
        }
        else
        {
            Debug.LogError("Safe Area Object�� RectTransform�� �����ϴ�.");
            return;
        }

        for (int floor = 0; floor < floorToggles.Length; floor++)
        {
            // �� ���� �׸��� ���� ����Ʈ �ʱ�ȭ
            floorGridRects[floor] = new List<Rect>();
            selectedTileIndicesByFloor[floor] = new List<int>();

            // ���� ���� �׸��� ��� �� ����
            int columns = 8;
            int rows = 8;
            if (floor == 1) // 2���� 7x7
            {
                columns = 7;
                rows = 7;
            }

            float totalGridWidth = columns * tileSize.x;
            float totalGridHeight = rows * tileSize.y;

            // �׸��带 Safe Area�� �߽ɿ� ��ġ
            float startX = safeArea.x + (safeArea.width - totalGridWidth) / 2;
            float startY = safeArea.y + (safeArea.height - totalGridHeight) / 2;

            // Ÿ�� ������� ��ư ���� (tileSize�� �°� ���簢�� ��ư ����)
            for (int y = 0; y < rows; y++) // y�� ���� �ݺ��ؼ� ������ �Ʒ���
            {
                for (int x = 0; x < columns; x++)
                {
                    float xPos = startX + x * tileSize.x;
                    float yPos = startY + y * tileSize.y;
                    Rect tileRect = new Rect(xPos, yPos, tileSize.x, tileSize.y);
                    floorGridRects[floor].Add(tileRect);
                }
            }
        }

        SceneView.RepaintAll();
    }

    private void SaveGrid()
    {
        Mahjong_GM.StageData stageData = new Mahjong_GM.StageData
        {
            stageName = stageName,
            stage = mahjongGM.stageDatas.Count + 1, // �� �������� ��ȣ ����
            tileCount = GetTotalSelectedTilesCount() // ���õ� Ÿ�� ���� ����
        };

        // ������ Ÿ�� ��ġ ����
        List<Vector2> floor1Positions = new List<Vector2>();
        List<Vector2> floor2Positions = new List<Vector2>();
        List<Vector2> floor3Positions = new List<Vector2>();

        RectTransform safeAreaTransform = safeAreaObject.GetComponent<RectTransform>();

        foreach (var floorIndices in selectedTileIndicesByFloor)
        {
            int floor = floorIndices.Key;
            List<int> selectedIndices = floorIndices.Value;
            List<Rect> floorRects = floorGridRects[floor];
            List<Vector2> savedTilePositions = new List<Vector2>();

            foreach (int index in selectedIndices)
            {
                if (index < floorRects.Count)
                {
                    Rect rect = floorRects[index];
                    //Vector2 localPosition = new Vector2(rect.x - (safeArea.width / 2), rect.y - (safeArea.height / 2)); // Safe Area�� �߽��� (0,0)���� �������� ��ȯ
                    Vector2 centerPosition = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
                    // Safe Area�� ���� ��ǥ�� ��ȯ
                    Vector2 localPosition = safeAreaTransform.InverseTransformPoint(new Vector3(centerPosition.x, centerPosition.y, 0));
                    savedTilePositions.Add(localPosition);
                }
            }

            // y�� ���� �������� ���� (������ �Ʒ��� ����)
            savedTilePositions.Sort((a, b) => b.y.CompareTo(a.y));

            // �� ������ ����� ��ġ �Ҵ�
            switch (floor)
            {
                case 0:
                    floor1Positions.AddRange(savedTilePositions);
                    break;
                case 1:
                    floor2Positions.AddRange(savedTilePositions);
                    break;
                case 2:
                    floor3Positions.AddRange(savedTilePositions);
                    break;
            }
        }

        // ���ĵ� ���� Ÿ�� ��ġ�� StageData�� ����
        stageData.floor1_tilePosition = floor1Positions.ToArray();
        stageData.floor2_tilePosition = floor2Positions.ToArray();
        stageData.floor3_tilePosition = floor3Positions.ToArray();

        // Mahjong_GM�� StageData �߰�
        mahjongGM.stageDatas.Add(stageData);

        Debug.Log("Grid saved to Mahjong_GM for stage: " + stageName);
    }

    private int GetTotalSelectedTilesCount()
    {
        int count = 0;
        foreach (var selectedIndices in selectedTileIndicesByFloor.Values)
        {
            count += selectedIndices.Count;
        }
        return count;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (floorGridRects.Count == 0)
        {
            return;
        }

        // ���� �׸��带 �׸��� �� ���� ����
        for (int floor = 0; floor < floorToggles.Length; floor++)
        {
            if (floorToggles[floor])
            {
                List<Rect> gridRects = floorGridRects[floor];
                for (int i = 0; i < gridRects.Count; i++)
                {
                    Handles.color = selectedTileIndicesByFloor[floor].Contains(i) ? Color.blue : Color.white;
                    if (Handles.Button(new Vector3(gridRects[i].x + tileSize.x / 2, gridRects[i].y + tileSize.y / 2, 0), Quaternion.identity, tileSize.x / 2, tileSize.y / 2, Handles.RectangleHandleCap))
                    {
                        if (selectedTileIndicesByFloor[floor].Contains(i))
                        {
                            selectedTileIndicesByFloor[floor].Remove(i);
                        }
                        else
                        {
                            selectedTileIndicesByFloor[floor].Add(i);
                        }
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}
