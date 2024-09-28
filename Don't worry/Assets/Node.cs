using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector2 pos { get; private set; }
    public List<Node> connectedNodes { get; private set; }
    public Node(Vector2 pos)
    {
        this.pos = pos;
        connectedNodes = new List<Node>();
    }
    public Node(Vector2 pos, Node parent)
    {
        this.pos = pos;
        connectedNodes = new List<Node>();
        Connect(this, parent);
    }
    public static void Connect(Node n1, Node n2)
    {
        n1.connectedNodes.Add(n2);
        n2.connectedNodes.Add(n1);
    }
    public bool isLeafNode()
    {
        return connectedNodes.Count == 1;
    }
    public Vector3 WorldPos(Terrain terrain)
    {
        return new Vector3(pos.x, terrain.terrainData.GetHeight((int)pos.x, (int)pos.y), pos.y);
    }
}
