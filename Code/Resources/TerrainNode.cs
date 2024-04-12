using Godot;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

public partial class TerrainNode : Resource
{
    // 8 characters indicating the style of each corner
    // from East to West, North to South, Top to Bottom:
    //      TNE=0, TNW=1, TSE=2, TSW=3, BNE=4, BNW=5, BSE=6, BSW=7
    // Styles:
    //      '0' = air
    //      '1' = low poly aka testmesh
    [Export]
    public string corners;
    [Export]
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
    public static string FlipSide(string connection, [StringLength(4, MinimumLength = 4)] NodeSides side)
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
