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
    public bool[] floorVisibility; // �� ���� ������ ���� ������ ���θ� ����
    public Color[] floorColors; // �� ������ �ٸ� ������ ���

    float tile_width;
    float tile_height;
    Vector2 previousFirstTileBottomRight; // ���� ���� ù ��° Ÿ���� ������ �Ʒ� ��ǥ�� ����

    void OnValidate()
    {
        if (tile != null)
        {        
            tile_width = tile.GetComponent<RectTransform>().rect.width;
            tile_height = tile.GetComponent<RectTransform>().rect.height;
        }

        // floors�� floorVisibility �迭�� ���� ����
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

        // Safe Area�� ũ��� ��ġ�� �����ɴϴ�.
        Rect safeAreaRect = safeArea.rect;
        Vector2 safeAreaPosition = safeArea.position;

        // ù ��° �������� Ÿ�� ��ġ�� ����մϴ�.
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
                break; // Ÿ���� ���� 0 ���ϰ� �Ǹ� �� �̻� �׸��� �ʽ��ϴ�.
            }

            for (int row = 0; row < tilesPerColumn; row++)
            {
                for (int col = 0; col < tilesPerRow; col++)
                {
                    Vector2 tilePosition;
                    if (floorIndex == 0)
                    {
                        // ù ��° ���� Ÿ�� ��ġ ���
                        tilePosition = new Vector2(
                            safeAreaPosition.x - safeAreaRect.width / 2 + horizontalMargin + col * tile_width + tile_width / 2,
                            safeAreaPosition.y - safeAreaRect.height / 2 + verticalMargin + row * tile_height + tile_height / 2
                        );

                        // ù ��° Ÿ���� ������ �Ʒ� ��ǥ�� �����մϴ�.
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
                        // ���� ���� ù ��° Ÿ���� ������ �Ʒ� ��ǥ�� ���߱�
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
