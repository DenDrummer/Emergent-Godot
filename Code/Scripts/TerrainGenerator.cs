using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

public partial class TerrainGenerator : Node3D
{
    private static Array<TerrainNode> DefaultNodes()
    {
        Array<TerrainNode> defaultNodes = new Array<TerrainNode>();

        int helper;
        string corners;
        char[] reversed;

        for (int i = 0; i < 256; i++)
        {
            helper = i;
            corners = "";

            for (int j = 0; j < 8; j++)
            {
                corners += helper % 2;
                helper /= 2;
            }
            reversed = corners.ToCharArray();

            System.Array.Reverse(reversed);

            corners = new string(reversed);

            defaultNodes.Add(new TerrainNode(corners, new PlaceholderMesh()));
        }

        return defaultNodes;
    }

    [Export]
    public short MAX_PROPAGATIONS = 4;
    // TODO: if possible, export to editor as a single list/array/...
    [Export]
    public string[] cornerValues;
    [Export]
    public Mesh[] meshes;
    [Export]
    public short initialCells = 10;
    [Export]
    public short cellsPerTick = 10;
    [Export]
    public short gridScale = 2;

    [Export]
    private Array<TerrainNode> newNodeList = DefaultNodes();

    List<TerrainNode> nodes = new List<TerrainNode>();
    List<TerrainCell> cells = new List<TerrainCell>();

    public TerrainGenerator()
    {
        //GD.Print("TerrainGenerator Created.");
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        LoadNodes();
        GenerateInitialTerrain();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        for (int i = 0; i < Mathf.Min(initialCells, cellsPerTick); i++)
        {
            initialCells--;
            if (i % 3 == 0)
            {
                GenerateTerrain();
            } else
            {
                GenerateTerrain(0, 0, 0);
            }
        }
    }

    private void LoadNodes()
    {
        if (cornerValues.Length != meshes.Length)
        {
            throw new Exception("Different ammount of cornervalues and meshes (null values allowed)");
        }
        for (int i = 0; i < cornerValues.Length; i++)
        {
            nodes.Add(new TerrainNode(cornerValues[i], meshes[i]));
        }
        GD.Print($"{cornerValues.Length} nodes loaded.");
    }

    private void GenerateInitialTerrain()
    {
        TerrainCell spawnCell = GetCell(0, 0, 0);
        // x0 y0 z0 is always flat ground as it functions as the spawn
        bool validSpawn = false;
        TerrainNode spawnNode = null;
        Random random = new Random();

        List<TerrainNode> spawnNodes = new List<TerrainNode>(nodes);
        // TODO: filter on spawnNodes and then choose random, making while obsolete
        // .where ? (LINQ)

        spawnNodes.RemoveAll(sn =>
        {
            return !sn.corners.StartsWith("0000")
            || sn.corners[4] == '0'
            || sn.corners[5] == '0'
            || sn.corners[6] == '0'
            || sn.corners[7] == '0';
        });

        while (!validSpawn && spawnNodes.Count > 0)
        {
            validSpawn = true;
            spawnNode = spawnNodes[random.Next(spawnNodes.Count)];

            if (!spawnNode.corners.StartsWith("0000") ||
                spawnNode.corners[4] == '0' ||
                spawnNode.corners[5] == '0' ||
                spawnNode.corners[6] == '0' ||
                spawnNode.corners[7] == '0')
            {
                validSpawn = false;
            }

            if (!validSpawn)
            {
                // remove invalid node so we don't have to check for it again
                spawnNodes.Remove(spawnNode);
            }
        }

        if (!validSpawn)
        {
            GD.PrintErr("No valid spawn found");
            return;
        }

        spawnCell.CollapseTo(spawnNode);
        PropagateChanges(spawnCell);
    }

    public void GenerateTerrain()
    {
        List<TerrainCell> filteredCells = cells.FindAll(c => !c.collapsed);

        GenerateTerrain(filteredCells);
    }

    public void GenerateTerrain(short x, short y, short z)
    {
        float scaledX = -z / gridScale;
        float scaledY = y / gridScale;
        float scaledZ = x / gridScale;
        List<TerrainCell> filteredCells = cells.FindAll(c => !c.collapsed);
        filteredCells.Sort((a, b) =>
        {
            float aDelta = Mathf.Sqrt(Mathf.Pow(a.x - scaledX, 2) + Mathf.Pow(a.y - scaledY, 2) + Mathf.Pow(a.z - scaledZ, 2));
            float bDelta = Mathf.Sqrt(Mathf.Pow(b.x - scaledX, 2) + Mathf.Pow(b.y - scaledY, 2) + Mathf.Pow(b.z - scaledZ, 2));
            float distanceDelta = aDelta - bDelta;

            if (distanceDelta > 0) return 1;
            if (distanceDelta < 0) return -1;
            return 0;
        });

        float delta = Mathf.Sqrt(Mathf.Pow(filteredCells[0].x - scaledX, 2) + Mathf.Pow(filteredCells[0].y - scaledY, 2) + Mathf.Pow(filteredCells[0].z - scaledZ, 2));
        filteredCells.RemoveAll(c =>
        {
            float cDelta = Mathf.Sqrt(Mathf.Pow(c.x - scaledX, 2) + Mathf.Pow(c.y - scaledY, 2) + Mathf.Pow(c.z - scaledZ, 2));
            return cDelta > delta;
        });

        GenerateTerrain(filteredCells);
    }

    private void GenerateTerrain(List<TerrainCell> filteredCells)
    {
        filteredCells.Sort((a, b) => a.nodes.Count - b.nodes.Count);
        short minStates = (short)filteredCells[0].nodes.Count;
        filteredCells.RemoveAll(c => c.nodes.Count > minStates);

        filteredCells.Sort((a, b) => a.propagationDepth - b.propagationDepth);
        short minPropagationDepth = filteredCells[0].propagationDepth;
        filteredCells.RemoveAll(c => c.propagationDepth > minPropagationDepth);

        int cellIndex = new Random().Next(0, filteredCells.Count);
        TerrainCell fc = filteredCells[cellIndex];

        fc.CollapseRandomly();
        PropagateChanges(fc);
    }

    public TerrainCell GetCell(short x, short y, short z)
    {
        TerrainCell cell = cells.Find(c =>
        {
            return c.x == x && c.y == y && c.z == z;
        });
        if (cell != null)
        {
            return cell;
        }
        cell = new TerrainCell(x, y, z, gridScale, nodes, this);
        cells.Add(cell);
        return cell;
    }

    public void PropagateChanges(TerrainCell rootCell, short propagationDepth = 1)
    {
        TerrainCell targetCell;
        NodeSides rootSide, targetSide;

        // propagate top
        targetCell = GetCell(rootCell.x, (short)(rootCell.y + 1), rootCell.z);
        rootSide = NodeSides.TOP;
        targetSide = NodeSides.BOTTOM;
        PropagateSide(rootCell, rootSide, targetCell, targetSide, propagationDepth);

        // propagate bottom
        targetCell = GetCell(rootCell.x, (short)(rootCell.y - 1), rootCell.z);
        rootSide = NodeSides.BOTTOM;
        targetSide = NodeSides.TOP;
        PropagateSide(rootCell, rootSide, targetCell, targetSide, propagationDepth);

        // propagate north
        targetCell = GetCell(rootCell.x, rootCell.y, (short)(rootCell.z - 1));
        rootSide = NodeSides.NORTH;
        targetSide = NodeSides.SOUTH;
        PropagateSide(rootCell, rootSide, targetCell, targetSide, propagationDepth);

        // propagate south
        targetCell = GetCell(rootCell.x, rootCell.y, (short)(rootCell.z + 1));
        rootSide = NodeSides.SOUTH;
        targetSide = NodeSides.NORTH;
        PropagateSide(rootCell, rootSide, targetCell, targetSide, propagationDepth);

        // propagate east
        targetCell = GetCell((short)(rootCell.x + 1), rootCell.y, rootCell.z);
        rootSide = NodeSides.EAST;
        targetSide = NodeSides.WEST;
        PropagateSide(rootCell, rootSide, targetCell, targetSide, propagationDepth);

        // propagate west
        targetCell = GetCell((short)(rootCell.x - 1), rootCell.y, rootCell.z);
        rootSide = NodeSides.WEST;
        targetSide = NodeSides.EAST;
        PropagateSide(rootCell, rootSide, targetCell, targetSide, propagationDepth);
    }

    public void PropagateSide(TerrainCell rootCell, NodeSides rootSide,
        TerrainCell targetCell, NodeSides targetSide,
        short propagationDepth)
    {
        List<string> sides = new List<string>();
        string side;
        foreach (TerrainNode node in rootCell.nodes)
        {
            side = node.GetSide(rootSide);
            side = TerrainNode.FlipSide(side, rootSide);
            sides.Add(side);
        }
        sides = sides.Distinct().ToList();
        bool nodesRemoved = false;
        if (!targetCell.collapsed)
        {
            nodesRemoved = targetCell.Collapse(sides, targetSide, propagationDepth);
            if (targetCell.collapsed)
            {
                propagationDepth = 0;
            }
        }
        if (nodesRemoved && propagationDepth < MAX_PROPAGATIONS)
        {
            propagationDepth++;
            PropagateChanges(targetCell, propagationDepth);
        }
    }
}
