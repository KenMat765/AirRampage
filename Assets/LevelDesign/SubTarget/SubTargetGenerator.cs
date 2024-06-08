using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SubTargetGenerator : MonoBehaviour
{
    [Header("Terrain Information")]
    [SerializeField] Texture2D heightMap;
    [SerializeField, ShowIf("NoHeightMap")] float heightOffset;
    bool NoHeightMap() { return !heightMap; }
    [SerializeField] float terrainWidth;
    [SerializeField] float terrainLength;
    [SerializeField] float terrainHeight;


    [Header("Generate Settings")]
    [SerializeField] float widthStep, lengthStep;
    [SerializeField] float minHeight, maxHeight;
    [SerializeField] float left, right, up, down;


    [Header("Offset Range")]
    [SerializeField] float minOffset;
    [SerializeField] float maxOffset;


    [Header("Sub Target")]
    [SerializeField] GameObject subTargetPrefab;
    [SerializeField] GameObject subtargetParent;


    void OnDrawGizmos()
    {
        Vector3 center = new Vector3(terrainWidth / 2f + (left - right), (maxHeight + minHeight) / 2, terrainLength / 2f + (down - up));
        Vector3 size = new Vector3(terrainWidth - left - right, maxHeight - minHeight, terrainLength - up - down);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }


    [ContextMenu("Generate Sub Targets")]
    void GenerateSubTargets()
    {
        if (widthStep == 0 || lengthStep == 0)
        {
            Debug.LogWarning("Stepが0になっています!!");
            return;
        }

        if (!subtargetParent)
        {
            Debug.LogWarning("SubtargetParentが指定されていません!!");
            return;
        }

        Vector2 start = new Vector2(left, down);
        Vector2 end = new Vector2(terrainWidth, terrainLength) - new Vector2(right, up);

        for (float l = start.y; l <= end.y; l += lengthStep)
            for (float w = start.x; w <= end.x; w += widthStep)
            {
                float height;

                if (heightMap == null)
                {
                    height = heightOffset;
                }

                else
                {
                    Vector2 terrain_coord = new Vector2(w, l);
                    Vector2 pixel_coord = Terrain2Pixel(terrain_coord);

                    int pixel_x = Mathf.FloorToInt(pixel_coord.x);
                    int pixel_y = Mathf.FloorToInt(pixel_coord.y);
                    Color pixel_color = heightMap.GetPixel(pixel_x, pixel_y);

                    height = HeightDecoder(pixel_color);
                }

                float offset = Random.Range(minOffset, maxOffset);
                float h = height + offset;
                if (h > maxHeight) h = maxHeight;
                else if (h < minHeight) h = minHeight;

                Vector3 position = new Vector3(w, h, l);
                Instantiate(subTargetPrefab, position, Quaternion.identity, subtargetParent.transform);
            }
    }


    [ContextMenu("Analyze Terrain")]
    void AnalyzeTerrain()
    {
        if (heightMap == null)
        {
            Debug.LogWarning("HeightMapを指定してください!!");
            return;
        }

        if (widthStep == 0 || lengthStep == 0)
        {
            Debug.LogWarning("Stepが0になっています!!");
            return;
        }

        float min = 1000;
        float max = 0;
        for (float l = 0; l <= terrainLength; l += lengthStep)
            for (float w = 0; w <= terrainWidth; w += widthStep)
            {
                Vector2 terrain_coord = new Vector2(w, l);
                Vector2 pixel_coord = Terrain2Pixel(terrain_coord);

                int pixel_x = Mathf.FloorToInt(pixel_coord.x);
                int pixel_y = Mathf.FloorToInt(pixel_coord.y);
                Color pixel_color = heightMap.GetPixel(pixel_x, pixel_y);

                float height = HeightDecoder(pixel_color);

                if (height < min) min = height;
                if (max < height) max = height;
            }

        Debug.Log($"Max : {max}");
        Debug.Log($"Min : {min}");
    }


    Vector2 Terrain2Pixel(Vector2 terrain)
    {
        Vector2 pixel;
        pixel.x = terrain.x * heightMap.width / terrainWidth;
        pixel.y = terrain.y * heightMap.height / terrainLength;
        return pixel;
    }


    float HeightDecoder(Color pixel_color)
    {
        float gray = pixel_color.grayscale;
        float height = terrainHeight * gray;
        return height;
    }
}
