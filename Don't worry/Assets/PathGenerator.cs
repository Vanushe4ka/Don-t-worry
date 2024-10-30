using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
   
    // Start is called before the first frame update
    [SerializeField] Terrain terrain;
    [SerializeField] TerrainData terrainData;
    [SerializeField] Generator generator;
    [SerializeField] int minPathLength;
    float pathLength;
    [SerializeField] int minLeafNodes;
    [SerializeField] int maxLeafNodes;
    [SerializeField] int maxDistanceBetweenNodes;
    [SerializeField] int minDistanceBetweenNodes;
    [SerializeField] float maxHeightInTerrain;
    [SerializeField] float minAngleBetweenConnections = 45;
    public List<Node> nodes = new List<Node>();
    

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 nodePos = nodes[i].WorldPos(terrain);
            Gizmos.DrawSphere(nodePos, 1);
            for (int j = 0; j < nodes[i].connectedNodes.Count; j++)
            {
                Vector3 dautherNodePos = nodes[i].connectedNodes[j].WorldPos(terrain);
                Gizmos.DrawLine(nodePos, dautherNodePos);
            }
        }
        
    }
    int CalcLeafNodes()
    {
        int res = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].isLeafNode()) { res++; }
        }
        return res;
    }
    List<Node> LeafNodesList()
    {
        List<Node> resLIst = new List<Node>();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].isLeafNode()) { resLIst.Add(nodes[i]); }
        }
        return resLIst;
    }
    bool CreateStartNode()
    {
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        if (heights[Mathf.RoundToInt(terrainData.size.z / 2),Mathf.RoundToInt(terrainData.size.x / 2)] > generator.stoneHeightThreshold - generator.blendRange)
        {
            return false;
        }
        nodes.Add(new Node(new Vector2(terrainData.size.x/2,terrainData.size.z/2)));
        return true;
    }
    void CreateNode(Node parent)
    {
        
        float? randomAngle = GenerateRandomAngle(parent);
        if (randomAngle == null) { return; }
        float angle = (float)randomAngle;
        float distance = Random.Range(minDistanceBetweenNodes, maxDistanceBetweenNodes);

        // Вычисляем координаты новой точки
        float offsetX = parent.pos.x + Mathf.Cos(angle) * distance;
        float offsetY = parent.pos.y + Mathf.Sin(angle) * distance;
        float circleShiftX = terrainData.size.x - generator.minDistanceToStartCircle*2;
        float circleShiftY = terrainData.size.z - generator.minDistanceToStartCircle*2;
        if (offsetX > terrainData.size.x - circleShiftX || offsetY >terrainData.size.z - circleShiftY || 
            offsetX < circleShiftX || offsetY < circleShiftY) { return; }

        Vector2 nodePos = new Vector2(offsetX, offsetY);
        if (isHaveMontainsOnLine(parent.pos, nodePos)) { return; }
        List<Node> checkedNodes = new List<Node>();
        checkedNodes.Add(parent);
        for (int i = 0; i < nodes.Count; i++)
        {
            if (Vector2.Distance(nodePos,nodes[i].pos) < minDistanceBetweenNodes) { return; }
            for (int j = 0; j < nodes[i].connectedNodes.Count; j++)
            {
                if (checkedNodes.Contains(nodes[i])) { continue; }
                if (!checkedNodes.Contains(nodes[i].connectedNodes[j]))
                {
                    if (DoLinesWithThicknessIntersect(parent.pos,nodePos,nodes[i].pos, nodes[i].connectedNodes[j].pos, minDistanceBetweenNodes) 
                        || DistanceFromPointToLineSegment(nodePos, nodes[i].pos, nodes[i].connectedNodes[j].pos) < minDistanceBetweenNodes) 
                    { return; }

                }
                checkedNodes.Add(nodes[i]);
            }
        }

        Node newNode = new Node(nodePos, parent);
        nodes.Add(newNode);
        pathLength += Vector3.Distance(newNode.pos, parent.pos);

    }
    void ConnectNodes(Node first)
    {
        List<Node> nodeCanConnect = new List<Node>();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != first && !first.connectedNodes.Contains(nodes[i]) && Vector2.Distance(first.pos,nodes[i].pos) < maxDistanceBetweenNodes)
            {
                nodeCanConnect.Add(nodes[i]);
            }
        }
        if (nodeCanConnect.Count == 0) { return; }
        
        for (int n = 0; n < nodeCanConnect.Count; n++)
        {
            if (isHaveMontainsOnLine(first.pos, nodeCanConnect[n].pos)) { continue; }
            List<Node> checkedNodes = new List<Node>();
            checkedNodes.Add(first);
            checkedNodes.Add(nodeCanConnect[n]);
            Node second = nodeCanConnect[n];
            bool canConnect = true;
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes[i].connectedNodes.Count; j++)
                {
                    if (checkedNodes.Contains(nodes[i])) { continue; }
                    if (!checkedNodes.Contains(nodes[i].connectedNodes[j]))
                    {
                        if (DoLinesWithThicknessIntersect(first.pos, second.pos, nodes[i].pos, nodes[i].connectedNodes[j].pos, minDistanceBetweenNodes))
                        { canConnect = false; break; }

                    }
                }
            }
            if (canConnect) { Node.Connect(first, second); }
        }
    }
    public bool isHaveMontainsOnLine(Vector2 p1,Vector2 p2)
    {
        Vector2Int p1Int = new Vector2Int(Mathf.RoundToInt(p1.x), Mathf.RoundToInt(p1.y));
        Vector2Int p2Int = new Vector2Int(Mathf.RoundToInt(p2.x), Mathf.RoundToInt(p2.y));
        float distance = Vector2.Distance(p1,p2);
        
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        for (int i = 0; i < Mathf.RoundToInt(distance); i++)
        {
            float t = (float)i / distance;
            int x = Mathf.RoundToInt(Mathf.Lerp(p1.x, p2.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(p1.y, p2.y, t));

            if (heights[y,x] > generator.stoneHeightThreshold - generator.blendRange) {  return true; }
        }
        return false;
    }
    public float? GenerateRandomAngle(Node node)
    {
        List<float> angles = new List<float>();
        float minAngle = minAngleBetweenConnections * Mathf.Deg2Rad / Mathf.PI;
        // Вычисляем углы для каждой точки
        for (int i = 0; i < node.connectedNodes.Count; i++)
        {
            Vector2 direction = node.pos - node.connectedNodes[i].pos;
            float tempAngle = Mathf.Atan2(direction.y,direction.x) + Mathf.PI;
            tempAngle /= Mathf.PI;
            angles.Add(tempAngle);
        }
        if (angles.Count == 0) { return Random.Range(0, 2); }
        angles.Sort();
        List<Vector2> canNotRanges = new List<Vector2>();//x - min,y- max
        Vector2? firstRange = null;
        Vector2? lastRange = null;
        for (int i = 0; i < angles.Count; i++)
        {
            float min = angles[i] - minAngle;
            float max = angles[i] + minAngle;

            
            if (min >= 0 && max <= 2)
            {
                canNotRanges.Add(new Vector2(min, max));
            }
            else
            {
                if (min < 0)
                {
                    lastRange = (new Vector2(min + 2,  2));
                    firstRange = (new Vector2(0, max));
                }
                else if (max > 2)
                {
                    firstRange = (new Vector2(0, max - 2));
                    lastRange = (new Vector2(min, 2));
                }
            }
        }
        List<Vector2> angleRanges = new List<Vector2>();
        if (firstRange != null && lastRange != null)
        {
            canNotRanges.Insert(0, (Vector2)firstRange);
            canNotRanges.Add((Vector2)lastRange);
        }
        float angle = 0;
        int rangeIndex = 0;
        
        while(angle != 2)
        {
            if (angle < canNotRanges[rangeIndex].x) { angleRanges.Add(new Vector2(angle, canNotRanges[rangeIndex].x)); }
            angle = canNotRanges[rangeIndex].y;
            rangeIndex++;
            if (rangeIndex >= canNotRanges.Count && angle != 2)
            {
                angleRanges.Add(new Vector2(angle, 2));
                break;
            }
        }
        if (angleRanges.Count == 0)
        {
            Debug.Log("Can not search range");
            return null;
        }
        
        int angleRangeIndex = Random.Range(0, angleRanges.Count);
        float selecterAngle = Random.Range(angleRanges[angleRangeIndex].x, angleRanges[angleRangeIndex].y);
        
        selecterAngle *=  Mathf.PI;
        return selecterAngle;
    }
    public float DistanceFromPointToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        // Вектор от начала до конца отрезка
        Vector2 lineVector = lineEnd - lineStart;

        // Вектор от начала отрезка до точки
        Vector2 pointVector = point - lineStart;

        // Проекция вектора pointVector на lineVector
        float t = Vector2.Dot(pointVector, lineVector) / lineVector.sqrMagnitude;

        // Ограничение проекции на [0, 1], чтобы остаться в пределах отрезка
        t = Mathf.Clamp01(t);

        // Точка на отрезке, ближайшая к исходной точке
        Vector2 closestPoint = lineStart + t * lineVector;

        // Возвращаем расстояние от точки до ближайшей точки на отрезке
        return Vector2.Distance(point, closestPoint);
    }
    public bool DoLinesWithThicknessIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, float thickness)
    {
        // Построение прямоугольников для двух линий с учетом толщины
        Vector2[] rect1 = BuildRectangleAroundLine(p1, p2, thickness);
        Vector2[] rect2 = BuildRectangleAroundLine(p3, p4, thickness);

        // Проверка пересечения этих прямоугольников
        return DoRectanglesIntersect(rect1, rect2);
    }

    // Метод для построения прямоугольника вокруг линии с заданной толщиной
    private Vector2[] BuildRectangleAroundLine(Vector2 p1, Vector2 p2, float thickness)
    {
        Vector2 direction = (p2 - p1).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness / 2;

        Vector2[] rectangle = new Vector2[4];
        rectangle[0] = p1 + normal;
        rectangle[1] = p1 - normal;
        rectangle[2] = p2 - normal;
        rectangle[3] = p2 + normal;

        return rectangle;
    }

    // Метод для проверки пересечения двух прямоугольников (с использованием алгоритма SAT)
    private bool DoRectanglesIntersect(Vector2[] rect1, Vector2[] rect2)
    {
        return !IsSeparated(rect1, rect2) && !IsSeparated(rect2, rect1);
    }

    // Метод для проверки, отделены ли два прямоугольника с помощью алгоритма SAT (Separating Axis Theorem)
    private bool IsSeparated(Vector2[] rect1, Vector2[] rect2)
    {
        for (int i = 0; i < rect1.Length; i++)
        {
            // Получение нормали к краям прямоугольника
            Vector2 edge = rect1[(i + 1) % rect1.Length] - rect1[i];
            Vector2 axis = new Vector2(-edge.y, edge.x).normalized;

            // Проецирование всех точек на ось
            float min1, max1;
            ProjectPolygon(rect1, axis, out min1, out max1);

            float min2, max2;
            ProjectPolygon(rect2, axis, out min2, out max2);

            // Если есть промежуток между проекциями, значит прямоугольники не пересекаются
            if (max1 < min2 || max2 < min1)
                return true;
        }

        return false;
    }

    // Метод для проекции полигона на ось
    private void ProjectPolygon(Vector2[] polygon, Vector2 axis, out float min, out float max)
    {
        min = Vector2.Dot(polygon[0], axis);
        max = min;

        for (int i = 1; i < polygon.Length; i++)
        {
            float projection = Vector2.Dot(polygon[i], axis);
            if (projection < min)
            {
                min = projection;
            }
            if (projection > max)
            {
                max = projection;
            }
        }
    }

    public Vector2 RandomPointOnPath()
    {
        Node first = nodes[Random.Range(0, nodes.Count)];
        Node second = first.connectedNodes[Random.Range(0, first.connectedNodes.Count)];
        float t = Random.Range(0, 1f);
        return Vector2.Lerp(first.pos, second.pos, t);
    }
   
    public void GeneratePath(TerrainData TD)
    {
        terrainData = TD;
        nodes.Clear();
        pathLength = 0;
        if (!CreateStartNode())
        {
            if (generator.GenerateCorutine != null) { StopCoroutine(generator.GenerateCorutine); }
            generator.GenerateCorutine = StartCoroutine(generator.GenerateTerrain());
            Debug.LogError("CanNotPlaceStartPoint");
            return;
        }
        while(pathLength < minPathLength || CalcLeafNodes() < minLeafNodes)
        {
            List<Node> nodeList;
            if (CalcLeafNodes() >= maxLeafNodes)
            {
                nodeList = LeafNodesList();
            }
            else
            {
                nodeList = nodes;
            }
            int connectionChance = Random.Range(0, 11);
            if (connectionChance == 0)
            {
                ConnectNodes(nodeList[Random.Range(0, nodeList.Count)]);
            }
            else
            {
                CreateNode(nodeList[Random.Range(0, nodeList.Count)]);
            }
            
        }
    }
}
