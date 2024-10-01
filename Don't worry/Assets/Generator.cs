using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    [SerializeField] Player player;
    public Terrain terrain;
    TerrainCollider terrainCollider;
    [SerializeField] PathGenerator pathGenerator;
    public int terrainWidth = 512;
    public int terrainHeight = 512;
    public float noiseScale = 20f;

    public int octaves;
    public float persistence;
    public float lacunarity;
    
    public float stoneHeightThreshold = 0.7f; // Высота, на которой начинается камень (нормализованное значение)
    public float blendRange = 0.1f;

    public int numberOfTrees = 100;   // Количество деревьев
    public int numberOfGrass = 5000;  // Количество травы
    public float minTreeHeight = 0.2f; // Минимальная высота, на которой могут расти деревья

    private const int grassTextureIndex = 1;
    private const int stoneTextureIndex = 5;
    private const int pathTextureIndex = 4;

    public Coroutine GenerateCorutine;
    public float lineWidth;

    public Grib[] gribPrefabs;
    List<GameObject> spawnedGribs = new List<GameObject>();
    public int[] gribsToGenerateQuantity;
    void Start()
    {
        terrainCollider = terrain.gameObject.GetComponent<TerrainCollider>();
        //GenerateCorutine = StartCoroutine(GenerateTerrain());

    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void Regenerate()
    {
        for (int i = 0; i < spawnedGribs.Count; i++)
        {
            if (spawnedGribs[i] != null) { Destroy(spawnedGribs[i]); }
        }
        spawnedGribs.Clear();
        for (int i = 0; i < gribsToGenerateQuantity.Length; i++)
        {
            gribsToGenerateQuantity[i] = Random.Range(gribsToGenerateQuantity[i] / 2, gribsToGenerateQuantity[i]);
        }
        GenerateCorutine = StartCoroutine(GenerateTerrain());
        player.StartGame();
    }
    public IEnumerator GenerateTerrain()
    {
        player.transform.position = new Vector3(terrain.terrainData.size.x / 2, 50, terrain.terrainData.size.z / 2);
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
        pathGenerator.GeneratePath(terrainData);
        PaintTerrain();
        yield return StartCoroutine(PlaceVegetationAsync());
        if (terrainCollider.enabled) { terrainCollider.enabled = false; }
        terrainCollider.enabled = true;

    }
    float FractalNoise(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;  // используется для нормализации результата

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

        // Получаем размер альфамапы
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;

        // Получаем карту высот
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        // Получаем текущие альфамапы
        float[,,] alphaMap = new float[alphaMapWidth, alphaMapHeight, 7]; 
        for (int z = 0; z < alphaMapHeight; z++)
        {
            for (int x = 0; x < alphaMapWidth; x++)
            {
                // Преобразуем координаты альфамапы в координаты карты высот
                int terrainX = Mathf.FloorToInt((float)x / alphaMapWidth * terrainData.heightmapResolution);
                int terrainZ = Mathf.FloorToInt((float)z / alphaMapHeight * terrainData.heightmapResolution);

                // Получаем высоту в нормализованном виде (от 0 до 1)
                float height = heights[terrainZ, terrainX];

                // Определяем вес текстур травы и камня на основе высоты и blendRange
                float stoneWeight = Mathf.InverseLerp(stoneHeightThreshold - blendRange, stoneHeightThreshold + blendRange, height);
                float grassWeight = 1f - stoneWeight;

                // Применяем веса к слоям альфамапы (индекс 1 - трава, индекс 6 - камень)
                alphaMap[z, x, grassTextureIndex] = grassWeight;
                alphaMap[z, x, stoneTextureIndex] = stoneWeight;
            }
        }
        List<Node> nodes = pathGenerator.nodes;
        List<Node> checkedNodes = new List<Node>();
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes[i].connectedNodes.Count; j++)
            {
                if (!checkedNodes.Contains(nodes[i].connectedNodes[j]))
                {
                    DrawLineOnTerrain(nodes[i].pos, nodes[i].connectedNodes[j].pos, alphaMap, alphaMapWidth, alphaMapHeight);

                }
                checkedNodes.Add(nodes[i]);
            }
        }

        // Применяем изменённые альфамапы
        terrainData.SetAlphamaps(0, 0, alphaMap);
    }
    public void DrawLineOnTerrain(Vector2 p1, Vector2 p2, float[,,] alphaMap, int alphaMapWidth, int alphaMapHeight)
    {
        Vector2Int p1Int = new Vector2Int(Mathf.RoundToInt(p1.x), Mathf.RoundToInt(p1.y));
        Vector2Int p2Int = new Vector2Int(Mathf.RoundToInt(p2.x), Mathf.RoundToInt(p2.y));
        float distance = Vector2.Distance(p1, p2);
        TerrainData terrainData = terrain.terrainData;
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        for (int i = 0; i < Mathf.RoundToInt(distance); i++)
        {
            float t = (float)i / distance;
            int x = Mathf.RoundToInt(Mathf.Lerp(p1.x, p2.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(p1.y, p2.y, t));

            DrawPointOnTerrain(new Vector2Int(x, y), lineWidth, alphaMap, alphaMapWidth, alphaMapHeight);
        }
    }
    private void DrawPointOnTerrain(Vector2Int point, float lineWidth, float[,,] alphaMap, int alphaMapWidth, int alphaMapHeight)
    {
        // Преобразуем мировые координаты в координаты альфамапы
        int x = Mathf.FloorToInt(point.x / terrain.terrainData.size.x * alphaMapWidth);
        int z = Mathf.FloorToInt(point.y / terrain.terrainData.size.z * alphaMapHeight);

        // Ограничиваем координаты альфамапы
        x = Mathf.Clamp(x, 0, alphaMapWidth - 1);
        z = Mathf.Clamp(z, 0, alphaMapHeight - 1);

        // Устанавливаем максимальное альфа-значение для текстур в пределах ширины линии
        int halfWidth = Mathf.FloorToInt(lineWidth / 2);

        for (int i = -halfWidth; i <= halfWidth; i++)
        {
            // Проверяем границы
            int currentX = Mathf.Clamp(x + i, 0, alphaMapWidth - 1);
            int currentZ = Mathf.Clamp(z, 0, alphaMapHeight - 1);

            // Устанавливаем альфа-значение для выбранного текстурного слоя (трава и камень)
            alphaMap[currentZ, currentX, grassTextureIndex] = 0; // Устанавливаем траву на 0 для линии
            alphaMap[currentZ, currentX, stoneTextureIndex] = 0; // Устанавливаем камень на 0 для линии
            alphaMap[currentZ, currentX, pathTextureIndex] = 1; // Устанавливаем камень на 0 для линии
        }
    }
    void ClearTerrainVegetation()
    {
        // Очищаем деревья
        terrain.terrainData.treeInstances = new TreeInstance[0];

        // Очищаем все слои травы
        for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++)
        {
            int[,] emptyLayer = new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight];
            terrain.terrainData.SetDetailLayer(0, 0, i, emptyLayer);
        }
    }

    // Метод для размещения деревьев и травы
    IEnumerator PlaceVegetationAsync()
    {
        ClearTerrainVegetation();
        TerrainData terrainData = terrain.terrainData;

        float[,,] alphaMaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        List<TreeInstance> trees = new List<TreeInstance>();
        int[][,] grassLayers = new int[terrainData.detailPrototypes.Length][,];
        for (int i = 0; i < grassLayers.Length; i++)
        {
            grassLayers[i] = new int[terrainData.detailWidth, terrainData.detailHeight];
        }
        //int[,] grassLayer =

        // Генерация деревьев
        for (int i = 0; i < numberOfTrees; i++)
        {
            Vector3 treePosition = GetRandomPositionOnGrass(alphaMaps, terrainData);
            if (treePosition != Vector3.zero)
            {
                TreeInstance treeInstance = CreateTreeInstance(treePosition);
                trees.Add(treeInstance);
            }

            if (i % 100 == 0)
            {
                yield return null;  // Ждём следующий кадр
            }
        }

        // Применение деревьев разом
        terrainData.treeInstances = trees.ToArray();
        
        // Генерация травы
        for (int i = 0; i < numberOfGrass; i++)
        {
            Vector3 grassPosition = GetRandomPositionOnGrass(alphaMaps, terrainData);
            if (grassPosition != Vector3.zero)
            {
                int detailX = Mathf.FloorToInt((grassPosition.x / terrainData.size.x) * terrainData.detailWidth);
                int detailZ = Mathf.FloorToInt((grassPosition.z / terrainData.size.z) * terrainData.detailHeight);
                int layer = Random.Range(0, grassLayers.Length);
                grassLayers[layer][detailZ, detailX] = 1;
            }

            if (i % 1000 == 0)
            {
                yield return null;  // Ждём следующий кадр
            }
        }
        for (int i = 0; i < grassLayers.Length; i++)
        {
            terrainData.SetDetailLayer(0, 0, i, grassLayers[i]);
        }

        for (int i = 0; i < gribsToGenerateQuantity.Length; i++)
        {
            for (int j = 0; j < gribsToGenerateQuantity[i]; j++)
            {
                Vector2 randomization = new Vector2(Random.Range(-1, 1f), Random.Range(-1, 1f));
                Vector2 posInTerrain = randomization + pathGenerator.RandomPointOnPath();
                spawnedGribs.Add(Instantiate(gribPrefabs[i].gameObject, new Vector3(posInTerrain.x, terrain.terrainData.GetHeight((int)posInTerrain.x, (int)posInTerrain.y), posInTerrain.y), Quaternion.Euler(0, 0, 0)));
            }
        }
        SetTargetInventoryToPlayer();

    }
    void SetTargetInventoryToPlayer()
    {
        List<(string, int)> targetInventory = new List<(string, int)>();
        for (int i = 0; i < gribsToGenerateQuantity.Length; i++)
        {
            if (!gribPrefabs[i].isPoisonous) targetInventory.Add((gribPrefabs[i].Name,gribsToGenerateQuantity[i]));
        }
        player.SetTargetInventory(targetInventory.ToArray());
    }
    Vector3 GetRandomPositionOnGrass(float[,,] alphaMaps, TerrainData terrainData)
    {
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;

        // Случайное положение на альфамапе
        int x = Random.Range(0, alphaMapWidth);
        int z = Random.Range(0, alphaMapHeight);

        // Получаем вес текстур травы и камня
        float grassWeight = alphaMaps[z, x, grassTextureIndex];  // Индекс 1 — трава
        float stoneWeight = alphaMaps[z, x, stoneTextureIndex];  // Индекс 5 — камень

        // Устанавливаем пороги для текстур
        float grassThreshold = 0.7f;  // Минимальный вес для травы
        float stoneThreshold = 0.1f;   // Максимальный вес для камня

        // Проверяем, что вес травы достаточно высок, а вес камня достаточно низок
        if (grassWeight > grassThreshold && stoneWeight < stoneThreshold)
        {
            // Преобразуем координаты альфамапы в мировые координаты террейна
            float worldPosX = (float)x / alphaMapWidth * terrainData.size.x;
            float worldPosZ = (float)z / alphaMapHeight * terrainData.size.z;

            // Получаем высоту террейна в этой точке
            float worldPosY = terrain.SampleHeight(new Vector3(worldPosX, 0, worldPosZ)) + terrain.transform.position.y;

            return new Vector3(worldPosX, worldPosY, worldPosZ);
        }

        // Если траву не удалось разместить, возвращаем (0,0,0)
        return GetRandomPositionOnGrass(alphaMaps, terrainData);
    }


    TreeInstance CreateTreeInstance(Vector3 position)
    {
        int randomTreeIndex = Random.Range(0, terrain.terrainData.treePrototypes.Length);

        return new TreeInstance
        {
            position = new Vector3(position.x / terrain.terrainData.size.x, position.y / terrain.terrainData.size.y, position.z / terrain.terrainData.size.z),
            prototypeIndex = randomTreeIndex,
            widthScale = 1,
            heightScale = 1,
            color = Color.white,
            lightmapColor = Color.white
        };
    }
}

