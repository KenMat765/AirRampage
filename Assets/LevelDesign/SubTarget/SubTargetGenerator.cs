using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SubTargetGenerator : MonoBehaviour
{
    [Header("Terrain Information")]
    [SerializeField] Terrain terrain;
    [SerializeField] Texture2D heightMap;
    bool NoHeightMap() { return !heightMap; }
    float terrainWidth { get { return terrain.terrainData.size.x; } }
    float terrainLength { get { return terrain.terrainData.size.z; } }
    float terrainHeight { get { return terrain.terrainData.size.y; } }
    Vector3 terrainPosition { get { return terrain.transform.position; } }


    [Header("Generate Settings")]
    [SerializeField] float minHeight;
    [SerializeField] float maxHeight;
    [SerializeField] float width, length;
    [SerializeField] float widthStep, lengthStep;


    [Header("Offset Range")]
    [SerializeField] float minOffset;
    [SerializeField] float maxOffset;


    [Header("Sub Target")]
    [SerializeField] GameObject subTargetPrefab;
    [SerializeField] GameObject subtargetParent;


    [Header("RayCast")]
    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] QueryTriggerInteraction queryTriggerInteraction;


    void OnDrawGizmos()
    {
        // Draw generate area.
        Vector3 generator_pos = transform.position;
        Vector3 center = new Vector3(generator_pos.x, (maxHeight + minHeight) / 2, generator_pos.z);
        Vector3 size = new Vector3(width, maxHeight - minHeight, length);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);

        // Draw max & min point of terrain.
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(min_point, 50);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(max_point, 50);
    }


    // Sub-Target Generation ///////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Generate sub-targets by raycast. (Used when terrain & structures exists)
    /// </summary>
    [Button("Generate Sub-targets by Raycast")]
    void GenerateSubTargetsRaycast()
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

        // Start & End position of grid.
        Vector3 generator_pos = transform.position;
        Vector2 start = new Vector2(generator_pos.x - width / 2, generator_pos.z - length / 2);
        Vector2 end = new Vector2(generator_pos.x + width / 2, generator_pos.z + length / 2);

        // Check height by raycast and create sub-target for each grid.
        for (float l = start.y; l <= end.y; l += lengthStep)
            for (float w = start.x; w <= end.x; w += widthStep)
            {
                // Cast a ray downward from maxHeight at each grid.
                Vector3 ray_origin = new Vector3(w, maxHeight, l);
                Ray ray = new Ray(ray_origin, Vector3.down);
                RaycastHit hit;
                float max_ray_dist = maxHeight - minHeight;
                if (Physics.Raycast(ray, out hit, max_ray_dist, obstacleLayer, queryTriggerInteraction))
                {
                    // Get height of hit point.
                    float height = hit.point.y;

                    // Add random offset to height.
                    float offset = Random.Range(minOffset, maxOffset);
                    float h = height + offset;
                    if (h > maxHeight) h = maxHeight;
                    else if (h < minHeight) h = minHeight;

                    // Create sub-target.
                    Vector3 position = new Vector3(w, h, l);
                    Instantiate(subTargetPrefab, position, Quaternion.identity, subtargetParent.transform);
                }
            }
    }

    /// <summary>
    /// Generate sub-targets inside generate area (Can be used when there are no terrains & structures)
    /// </summary>
    [Button("Generate Sub-targets Inside Area")]
    void GenerateSubTargetsInsideArea()
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

        // Start & End position of grid.
        Vector3 generator_pos = transform.position;
        Vector2 start = new Vector2(generator_pos.x - width / 2, generator_pos.z - length / 2);
        Vector2 end = new Vector2(generator_pos.x + width / 2, generator_pos.z + length / 2);

        // Check height by raycast and create sub-target for each grid.
        for (float l = start.y; l <= end.y; l += lengthStep)
            for (float w = start.x; w <= end.x; w += widthStep)
            {
                // Get middle height of generate area.
                float height = (maxHeight + minHeight) / 2;

                // Add random offset to height.
                float offset = Random.Range(minOffset, maxOffset);
                float h = height + offset;
                if (h > maxHeight) h = maxHeight;
                else if (h < minHeight) h = minHeight;

                // Create sub-target.
                Vector3 position = new Vector3(w, h, l);
                Instantiate(subTargetPrefab, position, Quaternion.identity, subtargetParent.transform);
            }
    }


    // Terrain Analyze /////////////////////////////////////////////////////////////////////////////////////////////////////
    Vector3 max_point, min_point;

    [Button("Analyze Terrain")]
    void AnalyzeTerrain()
    {
        if (widthStep == 0 || lengthStep == 0)
        {
            Debug.LogWarning("Stepが0になっています!!");
            return;
        }

        // Max & Min height of the terrain.
        float max_height = 0;
        float min_height = float.PositiveInfinity;

        // Start & End position of grid.
        Vector2 start = new Vector2(terrainPosition.x - terrainWidth / 2, terrainPosition.z - terrainLength / 2);
        Vector2 end = new Vector2(terrainPosition.x + terrainWidth / 2, terrainPosition.z + terrainLength / 2);

        for (float l = start.y; l <= end.y; l += lengthStep)
            for (float w = start.x; w <= end.x; w += widthStep)
            {
                // Cast a ray downward from maxHeight at each grid.
                Vector3 ray_origin = new Vector3(w, terrainPosition.y + terrainHeight, l);
                Ray ray = new Ray(ray_origin, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, terrainHeight * 2, GameInfo.terrainMask))
                {
                    // Get height of hit point and compare with current min & max height.
                    float height = hit.point.y;
                    if (height < min_height)
                    {
                        min_height = height;
                        min_point = new Vector3(w, height, l);
                    }
                    else if (max_height < height)
                    {
                        max_height = height;
                        max_point = new Vector3(w, height, l);
                    }
                }
            }

        Debug.Log($"Max : {max_height}");
        Debug.Log($"Min : {min_height}");
    }


    /// <summary>
    /// Get pixel coord from terrain coord.
    /// </summary>
    Vector2 Terrain2Pixel(Vector2 terrain)
    {
        Vector2 pixel;
        pixel.x = terrain.x * heightMap.width / terrainWidth;
        pixel.y = terrain.y * heightMap.height / terrainLength;
        return pixel;
    }


    /// <summary>
    /// Decode height from pixel color of heightmap.
    /// </summary>
    float HeightDecoder(Color pixel_color)
    {
        float gray = pixel_color.grayscale;
        float height = terrainHeight * gray;
        return height;
    }
}
