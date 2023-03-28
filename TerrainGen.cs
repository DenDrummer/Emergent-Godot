using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public enum Sides
{
	TOP, BOTTOM, NORTH, SOUTH, EAST, WEST
}

public class TerrainNode
{
    // 8 characters indicating the style of each corner
    // from East to West, North to South, Top to Bottom:
    //      TNE=0, TNW=1, TSE=2, TSW=3, BNE=4, BNW=5, BSE=6, BSW=7
    // Styles:
    //      0 = air
    //      1 = low poly
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
    public static string FlipSide(string connection, bool isVertical)
	{
        const string HORIZONTAL_FLIP= "2301"; 
        const string VERTICAL_FLIP  = "1032";
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < 4; i++)
        {
            stringBuilder.Append(connection[Int16.Parse((isVertical ? VERTICAL_FLIP : HORIZONTAL_FLIP)[i]+"")]);
        }
        return stringBuilder.ToString();
    }

    public string GetSide(Sides side)
    {
        StringBuilder stringBuilder = new StringBuilder();
        string buildString;
        switch (side)
        {
            case Sides.TOP:
                buildString = "0123";
                break;

            case Sides.BOTTOM:
                buildString = "6745";
                break;

            case Sides.NORTH:
                buildString = "1054";
                break;

            case Sides.SOUTH:
                buildString = "2367";
                break;

            case Sides.EAST:
                buildString = "3175";
                break;

            case Sides.WEST:
                buildString = "0246";
                break;

            default:
                throw new ArgumentException($"{side} is not a known side");
        }

        for (int i = 0; i < 4; i++)
        {
            stringBuilder.Append(corners[Int16.Parse(buildString[i]+"")]);
        }

        return stringBuilder.ToString();
    }

	public bool Connectable(Sides side, string connection)
	{
		string connector = GetSide(side);
		bool isVertical = (side == Sides.TOP || side == Sides.BOTTOM);
		return FlipSide(connection, isVertical) == connector;
	}
}

public class Cell
{
    // global position in grid
    public readonly short x;
    public readonly short y;
    public readonly short z;

    bool collapsed;

    public List<TerrainNode> possibleNodes;

    public Cell(short x, short y, short z, List<TerrainNode> nodes)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        possibleNodes = nodes;
        collapsed = false;
    }

    public bool Collapse(List<string> validConnections, Sides side)
    { 
        bool sidesRemoved = false;
        validConnections.Sort();
        foreach (TerrainNode node in possibleNodes)
        {
            string connection = node.GetSide(side);

            if (validConnections.BinarySearch(connection)<0)
            {
                sidesRemoved = true;
                possibleNodes.Remove(node);
            }
        }

        TestCollapsed();
        return sidesRemoved;
    }

    public void CollapseTo(TerrainNode node)
    {
        possibleNodes.Sort();
        if (possibleNodes.BinarySearch(node)<0)
        {
            throw new Exception($"The \"{node.corners}\" node is not valid for this location");
        }

        possibleNodes.RemoveAll(n =>
        {
            return n.corners != node.corners;
        });

        collapsed = true;
    }

    public void TestCollapsed()
    {
        collapsed = possibleNodes.Count == 1;
    }
}

public class TerrainGen
{
    List<TerrainNode> nodes = new List<TerrainNode>();
    List<Cell> cells = new List<Cell>();

    public TerrainGen()
    {
        CreateNodes();
        Generate();
    }

    // TODO: make list assignable from within Godot if possible
    public void CreateNodes()
    {
        nodes.Add(new TerrainNode("00000000", null)); // air
        nodes.Add(new TerrainNode("00001111", null)); // low poly floor

        // TODO: add more possible nodes
    }

    public void Generate()
    {
        Cell spawnCell = GetCell(0, 0, 0);
        // x0 y0 z0 is always flat ground as it functions as the spawn
        bool validSpawn = false;
        bool nodesRemoved = false;
        TerrainNode spawnNode = null;
        Random random = new Random();

        List<TerrainNode> spawnNodes = new List<TerrainNode>(spawnCell.possibleNodes);

        while (!validSpawn && spawnNodes.Count>0)
        {
            validSpawn = true;
            spawnNode = spawnNodes[random.Next(spawnNodes.Count)];

            if (!spawnNode.corners.StartsWith("0000"))
                validSpawn = false;
            else if (spawnNode.corners[4] == 0)
                validSpawn = false;
            else if (spawnNode.corners[5] == 0)
                validSpawn = false;
            else if (spawnNode.corners[6] == 0)
                validSpawn = false;
            else if (spawnNode.corners[7] == 0)
                validSpawn = false;

            if (!validSpawn)
            {
                //remove invalid node so we don't have to check for it again
                spawnNodes.Remove(spawnNode);
                nodesRemoved = true;
            }
        }

        if (!validSpawn)
        {
            throw new Exception("No valid spawn found!");
        }

        spawnCell.CollapseTo(spawnNode);
        if (nodesRemoved)
            Propagate(spawnCell);
    }

    public void Generate(short x, short y, short z)
    {
        // TODO: Generate specific coords, usually because a player is moving in this direction
    }

    public Cell GetCell(short x, short y, short z)
    {
        // find if cell exists
        Cell cell = cells.Find(c =>
        {
            return c.x == x && c.y == y && c.z == z;
        });
        if (cell != null)
        {
            // return existing cell
            return cell;
        }
        // create new cell
        cell = new Cell(x, y, z, nodes);
        cells.Add(cell);
        return cell;
    }

    public void Propagate(Cell cell)
    {
        Cell targetCell;
        Sides targetSide;

        //todo: propagate top
        targetCell = GetCell(cell.x, (short)(cell.y + 1), cell.z);
        targetSide = Sides.BOTTOM;
        PropagateSide(cell, targetCell, targetSide);

        //todo: propagate bottom
        targetCell = GetCell(cell.x, (short)(cell.y - 1), cell.z);
        targetSide = Sides.TOP;
        PropagateSide(cell, targetCell, targetSide);

        //todo: propagate north
        targetCell = GetCell(cell.x, cell.y, (short)(cell.z - 1));
        targetSide = Sides.SOUTH;
        PropagateSide(cell, targetCell, targetSide);

        //todo: propagate south
        targetCell = GetCell(cell.x, cell.y, (short)(cell.z + 1));
        targetSide = Sides.NORTH;
        PropagateSide(cell, targetCell, targetSide);

        //todo: propagate east
        targetCell = GetCell((short)(cell.x + 1), cell.y, cell.z);
        targetSide = Sides.WEST;
        PropagateSide(cell, targetCell, targetSide);

        //todo: propagate west
        targetCell = GetCell((short)(cell.x - 1), cell.y, cell.z);
        targetSide = Sides.EAST;
        PropagateSide(cell, targetCell, targetSide);

    }

    public void PropagateSide(Cell rootCell, Cell targetCell, Sides targetSide)
    {
        List<String> sides = new List<String>();
        foreach (TerrainNode node in rootCell.possibleNodes)
        {
            sides.Add(node.GetSide(Sides.TOP));
        }
        bool nodesRemoved = targetCell.Collapse(sides, targetSide);
        Propagate(targetCell);
    }
}