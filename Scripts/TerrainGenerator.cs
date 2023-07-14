using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

public partial class TerrainGenerator : Node3D
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
        }

        //flipped values:
        //  vertical:
        //		0 <-> 2
        //		1 <-> 3
        //  horizontal:
        //		0 <-> 1
        //		2 <-> 3
        //corner positions when seen from visible side:
        // 0=TOPLEFT 1=TOPRIGHT 2=BOTTOMLEFT 3=BOTTOMRIGHT
        public static string FlipSide(string connection, [StringLength(4,MinimumLength =4)] NodeSides side)
        {
            bool isVerticalConnection = (side == NodeSides.TOP || side == NodeSides.BOTTOM);
            const string X_FLIP = "1032";
            const string Y_FLIP = "2301";
            const string Z_FLIP = "1032";
            StringBuilder flippedSide = new StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                switch (side)
                {
                    case NodeSides.TOP:
                    case NodeSides.BOTTOM:
                        flippedSide.Append(connection[Int16.Parse($"{Y_FLIP[i]}")]);
                        break;
                    case NodeSides.NORTH:
                    case NodeSides.SOUTH:
                        flippedSide.Append(connection[Int16.Parse($"{X_FLIP[i]}")]);
                        break;
                    case NodeSides.EAST:
                    case NodeSides.WEST:
                        flippedSide.Append(connection[Int16.Parse($"{Z_FLIP[i]}")]);
                        break;
                };
            }
            return flippedSide.ToString();
        }

        public string GetSide(NodeSides side)
        {
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
            }

            for (int i = 0; i < buildString.Length; i++)
            {
                requestedSide.Append(corners[Int16.Parse($"{buildString[i]}")]);
            }

            return requestedSide.ToString();
        }

        public bool Connectable(NodeSides mySide, string connection)
        {
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
        private readonly short scale = 1;
        private Node3D scene;

        public Cell(short x, short y, short z, List<TerrainNode> nodes, Node3D scene)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.nodes = new List<TerrainNode>(nodes);
            this.scene = scene;
            //GD.Print($"Terrain cell created at x:{x} y:{y} z:{z}");
        }

        public bool Collapse(List<string> validConnections, NodeSides side, short propagationDepth)
        {
            //GD.Print($"Collapsing cell from the {side.ToString()} with {validConnections.Count} valid connections");
            bool sidesRemoved = false;
            for (int i = nodes.Count - 1; i >= 0; i--)
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
            if (!nodes.Contains(node))
            {
                GD.PrintErr($"The \"{node.corners}\" node is not valid for ({x};{y};{z})");
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

        public void CollapseRandomly()
        {
            int nodeIndex = new Random().Next(0, nodes.Count);
            nodes.RemoveAll(n =>
            {
                return n.corners != nodes[nodeIndex].corners;
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
            if (collapsed)
            {
                if (nodes[0].mesh != null)
                {
                    GD.Print($"placing {nodes[0].corners} at ({z * scale},{y * scale},{-x * scale})");
                    GD.Print($"mesh: {nodes[0].mesh.ResourcePath}");
                    MeshInstance3D meshInstance = new MeshInstance3D();
                    meshInstance.Mesh = nodes[0].mesh;
                    meshInstance.Position = new Vector3(z * scale, y * scale, -x * scale);
                    meshInstance.Name = $"({z};{y};{-x}){nodes[0].corners}";
                    scene.AddChild(meshInstance);
                }
                else
                {
                    GD.PrintErr($"No mesh or placeholder assigned for {nodes[0].corners} ({z * scale},{y * scale},{-x * scale})!");
                }
            }
            else
            {
                GD.PrintErr("Trying to place mesh before collapsing");
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
    [Export]
    public short initialCells = 10;

    List<TerrainNode> nodes = new List<TerrainNode>();
    List<Cell> cells = new List<Cell>();

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
        Cell spawnCell = GetCell(0, 0, 0);
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

        for (short i = 1; i < initialCells; i++)
        {
            GenerateTerrain();
        }
    }

    private void GenerateTerrain()
    {
        cells.Sort((a, b) => { return a.nodes.Count - b.nodes.Count; });
        List<Cell> filteredCells = cells.FindAll(c =>
        {
            return !c.collapsed;
        });

        short minStates = (short)filteredCells[0].nodes.Count;
        filteredCells.RemoveAll(c =>
        {
            return  c.propagationDepth != 1;
        });

        int cellIndex = new Random().Next(0, filteredCells.Count);
        Cell fc = filteredCells[cellIndex];

        fc.CollapseRandomly();
        PropagateChanges(fc);
    }

    private void GenerateTerrain(short x, short y, short z)
    {
        Cell cell = GetCell(x, y, z);
        cell.CollapseRandomly();
        PropagateChanges(cell);
    }

    public Cell GetCell(short x, short y, short z)
    {
        Cell cell = cells.Find(c =>
        {
            return c.x == x && c.y == y && c.z == z;
        });
        if (cell != null)
        {
            return cell;
        }
        cell = new Cell(x, y, z, nodes, this);
        cells.Add(cell);
        return cell;
    }

    public void PropagateChanges(Cell rootCell, short propagationDepth = 1)
    {
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
