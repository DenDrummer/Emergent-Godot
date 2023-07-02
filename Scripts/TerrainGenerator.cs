using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class TerrainGenerator : Node
{
    #region --- classes ---
    public enum NodeSides
	{
		TOP, BOTTOM, NORTH, SOUTH, EAST, WEST
	}

	public class TerrainNode
    {
        // 8 characters indicating the style of each corner
        // from East to West, North to South, Top to Bottom:
        //      TNE=0, TNW=1, TSE=2, TSW=3, BNE=4, BNW=5, BSE=6, BSW=7
        // Styles:
        //      '0' = air
        //      '1' = low poly aka testmesh
        public string corners;
        public Mesh mesh;

        public TerrainNode(string corners, Mesh mesh)
        {
            this.corners = corners;
            this.mesh = mesh;
            //GD.Print($"TerrainNode '{corners}' created");
        }

        //flipped values:
        //  vertical:
        //		0 <-> 2
        //		1 <-> 3
        //  horizontal:
        //		0 <-> 1
        //		2 <-> 3
        public static string FlipSide(string connection, NodeSides side)
        {
            bool isVerticalConnection = (side == NodeSides.TOP || side == NodeSides.BOTTOM);
            //GD.Print($"Flipping side '{connection}' " + (isVerticalConnection ? "vertically" : "horizontally") + "...");
            const string HORIZONTAL_FLIP = "2301";
            const string VERTICAL_FLIP = "1032";
            StringBuilder flippedSide = new StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                flippedSide.Append(connection[Int16.Parse((isVerticalConnection ? VERTICAL_FLIP : HORIZONTAL_FLIP)[i] + "")]);
            }
            //GD.Print($"-> Flipped side: '{flippedSide}'");
            return flippedSide.ToString();
        }

        public string GetSide(NodeSides side)
        {
            //GD.Print($"Getting the {side.ToString()} side");
            StringBuilder requestedSide = new StringBuilder();
            string buildString;
            switch (side)
            {
                case NodeSides.TOP:
                    buildString = "0123";
                    break;

                case NodeSides.BOTTOM:
                    buildString = "6745";
                    break;

                case NodeSides.NORTH:
                    buildString = "1054";
                    break;

                case NodeSides.SOUTH:
                    buildString = "2367";
                    break;

                case NodeSides.EAST:
                    buildString = "3175";
                    break;

                case NodeSides.WEST:
                    buildString = "0246";
                    break;

                default:
                    GD.PrintErr($"{side} is not a known side");
                    return null;
                    //throw new ArgumentException($"{side} is not a known side");
            }

            for (int i = 0; i< buildString.Length; i++)
            {
                requestedSide.Append(corners[Int16.Parse($"{buildString[i]}")]);
            }

            return requestedSide.ToString();
        }

        public bool Connectable(NodeSides mySide, string connection)
        {
            GD.Print($"Checking if {connection} can connect to this terrain node's {mySide.ToString()}");
            string connector = GetSide(mySide);
            return FlipSide(connection, mySide) == connector;
        }
    }

    public class Cell
    {
        // global position in grid
        public readonly short x, y, z;
        public bool collapsed;
        public List<TerrainNode> nodes;
        public short propagationDepth = short.MaxValue;
        private readonly short scale = 2;

        public Cell(short x, short y, short z, List<TerrainNode> nodes)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.nodes = new List<TerrainNode>(nodes);
            //GD.Print($"Terrain cell created at x:{x} y:{y} z:{z}");
        }

        public bool Collapse(List<string> validConnections, NodeSides side, short propagationDepth)
        {
            //GD.Print($"Collapsing cell from the {side.ToString()} with {validConnections.Count} valid connections");
            bool sidesRemoved = false;
            for (int i = nodes.Count - 1; i >= 0; i--)
            //foreach (TerrainNode node in nodes)
            {
                string connection = nodes[i].GetSide(side);

                // check if the node can connect to any of the valid connections
                if (!validConnections.Contains(connection))
                {
                    sidesRemoved = true;
                    nodes.Remove(nodes[i]);
                }
            }

            this.propagationDepth = Math.Min(this.propagationDepth, propagationDepth);

            TestCollapsed();
            return sidesRemoved;
        }

        public void CollapseTo(TerrainNode node)
        {
            GD.Print($"Collapsing ({x},{y},{z}) to \"{node.corners}\"");
            if (!nodes.Contains(node))
            {
                GD.PrintErr($"The \"{node.corners}\" node is not valid for this location");
                return;
            }

            nodes.RemoveAll(n =>
            {
                return n.corners != node.corners;
            });

            collapsed = true;
            propagationDepth = 0;
            PlaceMesh();
        }

        public void TestCollapsed()
        {
            //GD.Print("Testing if cell has been collapsed");
            if (nodes.Count == 1)
            {
                propagationDepth = 0;
                collapsed = true;
                PlaceMesh();
            }
            //GD.Print("-> " + (collapsed ? "collapsed" : "not collapsed"));
        }

        public void PlaceMesh()
        {
            // TODO: verify if mesh is placed
            if (collapsed & nodes[0].mesh != null)
            {
                GD.Print($"placing {nodes[0].corners} at ({x},{y},{z})");
                MeshInstance3D meshInstance = new MeshInstance3D();
                meshInstance.Mesh = nodes[0].mesh;
                meshInstance.Position = new Vector3(x*scale, y*scale, z*scale);
            }
        }
    }
    #endregion

    [Export]
    public short MAX_PROPAGATIONS = 4;
    // TODO: if possible, export to editor as a single list/array/...
    [Export]
    public string[] cornerValues;
    [Export]
    public Mesh[] meshes;

    List<TerrainNode> nodes = new List<TerrainNode>();
    List<Cell> cells = new List<Cell>();

    public TerrainGenerator()
    {
        //GD.Print("TerrainGenerator Created.");
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        //GD.Print("TerrainGenerator is ready.");
        LoadNodes();
        GenerateTerrain();
        //GD.Print("Terrain generated");
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
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

    private void GenerateTerrain()
    {
        //GD.Print("Generating the initial terrain");
        Cell spawnCell = GetCell(0, 0, 0);
        // x0 y0 z0 is always flat ground as it functions as the spawn
        bool validSpawn = false;
        //bool nodesRemoved = false;
        TerrainNode spawnNode = null;
        Random random = new Random();

        List<TerrainNode> spawnNodes = new List<TerrainNode>(nodes);
        // TODO: filter on spawnNodes and then choose random, making while obsolete
        // .where ? (LINQ)

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
                //nodesRemoved = true;
            }
        }

        if (!validSpawn)
        {
            GD.PrintErr("No valid spawn found");
            return;
            //throw new Exception("No valid spawn found!");
        }

        spawnCell.CollapseTo(spawnNode);
        PropagateChanges(spawnCell);
    }

    private void GenerateTerrain(short x, short y, short z)
    {
        // TODO: Generate specific coords, usually because a player is moving in this direction
    }

    public Cell GetCell(short x, short y, short z)
    {
        //GD.Print($"Finding cell at x:{x} y:{y} z:{z}");
        // find if cell exists
        Cell cell = cells.Find(c =>
        {
            return c.x == x && c.y == y && c.z == z;
        });
        if (cell != null)
        {
            //GD.Print("-> Returning existing cell");
            return cell;
        }
        //GD.Print("-> Creating new cell");
        cell = new Cell(x, y, z, nodes);
        cells.Add(cell);
        return cell;
    }

    public void PropagateChanges(Cell rootCell, short propagationDepth = 1)
    {
        //GD.Print($"Propagating changes from x:{rootCell.x} y:{rootCell.y} z:{rootCell.z} depth:{rootCell.propagationDepth}");
        Cell targetCell;
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

    public void PropagateSide(Cell rootCell, NodeSides rootSide,
        Cell targetCell, NodeSides targetSide,
        short propagationDepth)
    {
        //GD.Print($"Propagating changes to x:{targetCell.x} y:{targetCell.y} z:{targetCell.z}");
        List<string> sides = new List<string>();
        string side;
        foreach (TerrainNode node in rootCell.nodes)
        {
            side = node.GetSide(rootSide);
            side = TerrainNode.FlipSide(side, rootSide);
            sides.Add(side);
        }
        sides = sides.Distinct().ToList();
        bool nodesRemoved = targetCell.Collapse(sides, targetSide, propagationDepth);
        if (nodesRemoved && propagationDepth<MAX_PROPAGATIONS)
        {
            propagationDepth++;
            PropagateChanges(targetCell, propagationDepth);
        }
    }
}
