using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

public class TileCreatorTool : EditorWindow
{
    // Safe Area 오브젝트 참조
    public GameObject safeAreaObject;
    private Rect safeArea;

    // 타일 크기와 그리드 설정
    private Vector2 tileSize = new Vector2(130, 130);
    private Dictionary<int, List<Rect>> floorGridRects = new Dictionary<int, List<Rect>>(); // 각 층별 그리드 저장
    private Dictionary<int, List<int>> selectedTileIndicesByFloor = new Dictionary<int, List<int>>(); // 각 층별 선택된 타일 인덱스 저장

    // Mahjong_GM에 있는 stageDatas 리스트에 저장할 수 있도록
    private Mahjong_GM mahjongGM;

    // 저장된 그리드 위치를 저장할 리스트
    private Dictionary<string, List<Vector2>> savedGridData = new Dictionary<string, List<Vector2>>();

    // 저장 파일 이름 및 스테이지 이름
    private string stageName = "stage1";
    private bool[] floorToggles = new bool[3]; // 1, 2, 3층 토글 상태
    private bool gridGenerated = false; // 그리드가 생성되었는지 여부

    [MenuItem("Tools/Tile Creator Tool")]
    public static void ShowWindow()
    {
        GetWindow<TileCreatorTool>("Tile Creator Tool");
    }

    private void OnGUI()
    {
        // Safe Area 오브젝트 지정
        GUILayout.Label("Safe Area Settings", EditorStyles.boldLabel);
        safeAreaObject = (GameObject)EditorGUILayout.ObjectField("Safe Area Object", safeAreaObject, typeof(GameObject), true);

        // 스테이지 이름 지정
        GUILayout.Label("Save File Settings", EditorStyles.boldLabel);
        stageName = EditorGUILayout.TextField("Stage Name", stageName);

        // Mahjong_GM 오브젝트 지정
        GUILayout.Label("Mahjong GM Reference", EditorStyles.boldLabel);
        mahjongGM = (Mahjong_GM)EditorGUILayout.ObjectField("Mahjong GM", mahjongGM, typeof(Mahjong_GM), true);

        // 층 선택 토글 버튼 (그리드 생성 후 보이기/숨기기만 가능)
        GUILayout.Label("Floor Selection", EditorStyles.boldLabel);
        for (int i = 0; i < floorToggles.Length; i++)
        {
            floorToggles[i] = EditorGUILayout.Toggle("Floor " + (i + 1), floorToggles[i]);
        }

        // 선택된 타일 개수 표시
        GUILayout.Label("Selected Tiles: " + GetTotalSelectedTilesCount(), EditorStyles.boldLabel);

        // 그리드 생성 버튼
        if (GUILayout.Button("Generate Grid") && safeAreaObject != null)
        {
            GenerateGrid();
            gridGenerated = true;
        }

        // Save 버튼
        if (GUILayout.Button("Save Grid") && mahjongGM != null)
        {
            SaveGrid();
        }
    }

    private void GenerateGrid()
    {
        floorGridRects.Clear();
        selectedTileIndicesByFloor.Clear();

        // Safe Area 오브젝트의 RectTransform을 사용하여 영역 설정
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
            Debug.LogError("Safe Area Object에 RectTransform이 없습니다.");
            return;
        }

        for (int floor = 0; floor < floorToggles.Length; floor++)
        {
            // 각 층의 그리드 저장 리스트 초기화
            floorGridRects[floor] = new List<Rect>();
            selectedTileIndicesByFloor[floor] = new List<int>();

            // 층에 따른 그리드 행과 열 설정
            int columns = 8;
            int rows = 8;
            if (floor == 1) // 2층은 7x7
            {
                columns = 7;
                rows = 7;
            }

            float totalGridWidth = columns * tileSize.x;
            float totalGridHeight = rows * tileSize.y;

            // 그리드를 Safe Area의 중심에 배치
            float startX = safeArea.x + (safeArea.width - totalGridWidth) / 2;
            float startY = safeArea.y + (safeArea.height - totalGridHeight) / 2;

            // 타일 모양으로 버튼 생성 (tileSize에 맞게 직사각형 버튼 생성)
            for (int y = 0; y < rows; y++) // y를 먼저 반복해서 위에서 아래로
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
            stage = mahjongGM.stageDatas.Count + 1, // 새 스테이지 번호 지정
            tileCount = GetTotalSelectedTilesCount() // 선택된 타일 개수 저장
        };

        // 층별로 타일 위치 저장
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
                    //Vector2 localPosition = new Vector2(rect.x - (safeArea.width / 2), rect.y - (safeArea.height / 2)); // Safe Area의 중심을 (0,0)으로 기준으로 변환
                    Vector2 centerPosition = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
                    // Safe Area의 로컬 좌표로 변환
                    Vector2 localPosition = safeAreaTransform.InverseTransformPoint(new Vector3(centerPosition.x, centerPosition.y, 0));
                    savedTilePositions.Add(localPosition);
                }
            }

            // y값 기준 내림차순 정렬 (위에서 아래로 저장)
            savedTilePositions.Sort((a, b) => b.y.CompareTo(a.y));

            // 각 층별로 저장된 위치 할당
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

        // 정렬된 층별 타일 위치를 StageData에 저장
        stageData.floor1_tilePosition = floor1Positions.ToArray();
        stageData.floor2_tilePosition = floor2Positions.ToArray();
        stageData.floor3_tilePosition = floor3Positions.ToArray();

        // Mahjong_GM에 StageData 추가
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

        // 층별 그리드를 그리기 및 상태 관리
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
