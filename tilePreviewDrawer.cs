using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class tilePreviewDrawer : MonoBehaviour
{
    public RectTransform safeArea;
    public GameObject tile;
    public Transform[] floors;
    public bool[] floorVisibility; // 각 층의 예측을 보고 싶은지 여부를 선택
    public Color[] floorColors; // 각 층마다 다른 색상을 사용

    float tile_width;
    float tile_height;
    Vector2 previousFirstTileBottomRight; // 이전 층의 첫 번째 타일의 오른쪽 아래 좌표를 저장

    void OnValidate()
    {
        if (tile != null)
        {        
            tile_width = tile.GetComponent<RectTransform>().rect.width;
            tile_height = tile.GetComponent<RectTransform>().rect.height;
        }

        // floors와 floorVisibility 배열의 길이 맟춤
        if (floors != null)
        {
            if (floorVisibility == null || floorVisibility.Length != floors.Length)
            {
                floorVisibility = new bool[floors.Length];
                for (int i = 0; i < floorVisibility.Length; i++)
                {
                    floorVisibility[i] = true; 
                }
            }

            if (floorColors == null || floorColors.Length != floors.Length)
            {
                floorColors = new Color[floors.Length];
                for (int i = 0; i < floorColors.Length; i++)
                {
                    floorColors[i] = Color.cyan; 
                }
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (safeArea == null || tile == null || floors == null)
        {
            return;
        }

        // Safe Area의 크기와 위치를 가져옵니다.
        Rect safeAreaRect = safeArea.rect;
        Vector2 safeAreaPosition = safeArea.position;

        // 첫 번째 층에서의 타일 배치를 계산합니다.
        int initialTilesPerRow = Mathf.FloorToInt(safeAreaRect.width / tile_width);
        int initialTilesPerColumn = Mathf.FloorToInt(safeAreaRect.height / tile_height);

        float horizontalMargin = (safeAreaRect.width - (initialTilesPerRow * tile_width)) / 2.0f;
        float verticalMargin = (safeAreaRect.height - (initialTilesPerColumn * tile_height)) / 2.0f;

        for (int floorIndex = 0; floorIndex < floors.Length; floorIndex++)
        {
            if (!floorVisibility[floorIndex])
            {
                continue;
            }

            Transform floor = floors[floorIndex];
            Gizmos.color = floorColors[floorIndex];

            int tilesPerRow = initialTilesPerRow;
            int tilesPerColumn = initialTilesPerColumn;

            if (floorIndex > 0)
            {
                tilesPerRow -= floorIndex;
                tilesPerColumn -= floorIndex;
            }

            if (tilesPerRow <= 0 || tilesPerColumn <= 0)
            {
                break; // 타일의 수가 0 이하가 되면 더 이상 그리지 않습니다.
            }

            for (int row = 0; row < tilesPerColumn; row++)
            {
                for (int col = 0; col < tilesPerRow; col++)
                {
                    Vector2 tilePosition;
                    if (floorIndex == 0)
                    {
                        // 첫 번째 층의 타일 위치 계산
                        tilePosition = new Vector2(
                            safeAreaPosition.x - safeAreaRect.width / 2 + horizontalMargin + col * tile_width + tile_width / 2,
                            safeAreaPosition.y - safeAreaRect.height / 2 + verticalMargin + row * tile_height + tile_height / 2
                        );

                        // 첫 번째 타일의 오른쪽 아래 좌표를 저장합니다.
                        if (row == 0 && col == 0)
                        {
                            previousFirstTileBottomRight = new Vector2(
                                tilePosition.x + tile_width / 2,
                                tilePosition.y - tile_height / 2
                            );
                        }
                    }
                    else
                    {
                        // 이전 층의 첫 번째 타일의 오른쪽 아래 좌표에 맞추기
                        if (row == 0 && col == 0)
                        {
                            tilePosition = previousFirstTileBottomRight;
                        }
                        else
                        {
                            tilePosition = new Vector2(
                                previousFirstTileBottomRight.x + col * tile_width,
                                previousFirstTileBottomRight.y - row * tile_height
                            );
                        }
                    }

                    Gizmos.DrawWireCube(tilePosition, new Vector3(tile_width, tile_height, 1));
                }
            }
        }
    }
#endif
}
