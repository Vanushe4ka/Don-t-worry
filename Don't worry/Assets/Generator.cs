using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    public Terrain terrain;
    public int terrainWidth = 512;
    public int terrainHeight = 512;
    public float noiseScale = 20f;

    public int octaves;
    public float persistence;
    public float lacunarity;
    
    public float stoneHeightThreshold = 0.7f; // ������, �� ������� ���������� ������ (��������������� ��������)
    public float blendRange = 0.1f;

    public int numberOfTrees = 100;   // ���������� ��������
    public int numberOfGrass = 5000;  // ���������� �����
    public float minTreeHeight = 0.2f; // ����������� ������, �� ������� ����� ����� �������

    private const int grassTextureIndex = 1;
    private const int stoneTextureIndex = 5;

    void Start()
    {
        GenerateTerrain();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateTerrain();
            
        }
    }
    void GenerateTerrain()
    {
        TerrainData terrainData = terrain.terrainData;
        float[,] heights = new float[terrainWidth, terrainHeight];
        Vector2Int generationShift = new Vector2Int(Random.Range(-1000, 1000), Random.Range(-1000, 1000));
        for (int x = 0; x < terrainWidth; x++)
        {
            for (int y = 0; y < terrainHeight; y++)
            {
                //heights[x, y] = Mathf.PerlinNoise(x / noiseScale, y / noiseScale);
                 
                heights[x, y] = FractalNoise((x+generationShift.x) / noiseScale, (y + generationShift.y) / noiseScale,octaves,persistence,lacunarity);
            }
        }

        terrainData.heightmapResolution = terrainWidth + 1;
        terrainData.size = new Vector3(terrainWidth, 50, terrainHeight);
        terrainData.SetHeights(0, 0, heights);

        PaintTerrain();
        PlaceVegetation();
    }
    float FractalNoise(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;  // ������������ ��� ������������ ����������

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;

            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }
    void PaintTerrain()
    {
        TerrainData terrainData = terrain.terrainData;

        // �������� ������ ���������
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;

        // �������� ����� �����
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        // �������� ������� ���������
        float[,,] alphaMap = new float[alphaMapWidth, alphaMapHeight, 7]; 
        for (int z = 0; z < alphaMapHeight; z++)
        {
            for (int x = 0; x < alphaMapWidth; x++)
            {
                // ����������� ���������� ��������� � ���������� ����� �����
                int terrainX = Mathf.FloorToInt((float)x / alphaMapWidth * terrainData.heightmapResolution);
                int terrainZ = Mathf.FloorToInt((float)z / alphaMapHeight * terrainData.heightmapResolution);

                // �������� ������ � ��������������� ���� (�� 0 �� 1)
                float height = heights[terrainX, terrainZ];

                // ���������� ��� ������� ����� � ����� �� ������ ������ � blendRange
                float stoneWeight = Mathf.InverseLerp(stoneHeightThreshold - blendRange, stoneHeightThreshold + blendRange, height);
                float grassWeight = 1f - stoneWeight;

                // ��������� ���� � ����� ��������� (������ 1 - �����, ������ 6 - ������)
                alphaMap[z, x, grassTextureIndex] = grassWeight;
                alphaMap[z, x, stoneTextureIndex] = stoneWeight;
            }
        }

        // ��������� ��������� ���������
        terrainData.SetAlphamaps(0, 0, alphaMap);
    }
    void ClearTerrainVegetation()
    {
        // ������� �������
        terrain.terrainData.treeInstances = new TreeInstance[0];

        // ������� ��� ���� �����
        for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++)
        {
            int[,] emptyLayer = new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight];
            terrain.terrainData.SetDetailLayer(0, 0, i, emptyLayer);
        }
    }

    // ����� ��� ���������� �������� � �����
    void PlaceVegetation()
    {
        ClearTerrainVegetation();
        TerrainData terrainData = terrain.terrainData;

        // �������� ������ ���������
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;

        // �������� ���������
        float[,,] alphaMaps = terrainData.GetAlphamaps(0, 0, alphaMapWidth, alphaMapHeight);

        // ���������� ��������
        for (int i = 0; i < numberOfTrees; i++)
        {
            Vector3 treePosition = GetRandomPositionOnGrass(alphaMaps, terrainData);
            if (treePosition != Vector3.zero)
            {
                PlaceTree(treePosition);
            }
        }

        // ���������� �����
        for (int i = 0; i < numberOfGrass; i++)
        {
            Vector3 grassPosition = GetRandomPositionOnGrass(alphaMaps, terrainData);
            
            if (grassPosition != Vector3.zero)
            {
                PlaceGrass(grassPosition);
            }
        }
    }

    // ����� ��� ��������� ��������� ������� �� �������� �����
    Vector3 GetRandomPositionOnGrass(float[,,] alphaMaps, TerrainData terrainData)
    {
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;

        // ��������� ��������� �� ���������
        int x = Random.Range(0, alphaMapWidth);
        int z = Random.Range(0, alphaMapHeight);

        // ���������, ��� �������� � ���� ����� �� �������� (������ 1)
        float grassWeight = alphaMaps[z, x, grassTextureIndex];  // ������ 0 � �����
        float stoneWeight = alphaMaps[z, x, stoneTextureIndex];  // ������ 1 � ������

        // ���� �������� �������� � ����� (grassWeight ������, ��� stoneWeight)
        if (grassWeight > stoneWeight)
        {
            // ����������� ���������� ��������� � ������� ���������� ��������
            float worldPosX = (float)x / alphaMapWidth * terrainData.size.x;
            float worldPosZ = (float)z / alphaMapHeight * terrainData.size.z;

            // �������� ������ �������� � ���� �����
            float worldPosY = terrain.SampleHeight(new Vector3(worldPosX, 0, worldPosZ)) + terrain.transform.position.y;

            return new Vector3(worldPosX, worldPosY, worldPosZ);
        }

        return Vector3.zero; // ���� ����� �� ������� �� ��������� �������, ���������� (0,0,0)
    }

    // ����� ��� ���������� ������
    void PlaceTree(Vector3 position)
    {
        // �������� ��������� ��� ������, ������������������ � ��������
        int randomTreeIndex = Random.Range(0, terrain.terrainData.treePrototypes.Length);

        TreeInstance treeInstance = new TreeInstance
        {
            position = new Vector3(position.x / terrain.terrainData.size.x, position.y / terrain.terrainData.size.y, position.z / terrain.terrainData.size.z),
            prototypeIndex = randomTreeIndex,
            widthScale = 1,
            heightScale = 1,
            color = Color.white,
            lightmapColor = Color.white
        };

        // ��������� ������ � �������
        terrain.AddTreeInstance(treeInstance);
    }

    // ����� ��� ���������� �����
    void PlaceGrass(Vector3 position)
    {
        TerrainData terrainData = terrain.terrainData;

        // ����������� ������� ���������� � ���������� ��������
        int detailX = Mathf.FloorToInt((position.x / terrainData.size.x) * terrainData.detailWidth);
        int detailZ = Mathf.FloorToInt((position.z / terrainData.size.z) * terrainData.detailHeight);

        // �������� ������� �������� ���� ����� ��� ������� ������� �����
        int randomGrassIndex = Random.Range(0, terrainData.detailPrototypes.Length);
        int[,] details = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, randomGrassIndex);

        // ����������� ���������� ����� � ������ �������
        details[detailX, detailZ] = 1; // ������������� 1 ��������

        // ��������� ��������� ���� �����
        terrainData.SetDetailLayer(0, 0, randomGrassIndex, details);
    }
}

