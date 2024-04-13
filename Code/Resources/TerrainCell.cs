using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TerrainCell : Resource
{
    // global position in grid
    public readonly short x, y, z, scale;
    public bool collapsed;
    public List<TerrainNode> nodes;
    public short propagationDepth = short.MaxValue;
    private Node3D scene;

    public TerrainCell(short x, short y, short z, short scale, IEnumerable<TerrainNode> nodes, Node3D scene)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.scale = scale;
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

        CollapsedTo(node);
    }

    public void CollapseTo(IEnumerable<TerrainNode> collapsionNodes)
    {
        collapsionNodes = collapsionNodes.Where(n => nodes.Contains(n)).ToList();

        int nodeCount = collapsionNodes.Count();

        if (nodeCount == 0)
        {
            GD.PrintErr($"No valid node for ({x};{y};{z}) in list of collapsion nodes");
        }

        Random random = new Random();
        int randomIndex = random.Next(nodeCount);

        CollapsedTo(collapsionNodes.ElementAt(randomIndex));
    }

    private void CollapsedTo(TerrainNode node)
    {
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
                //GD.Print($"mesh: {nodes[0].mesh.ResourcePath}");
                MeshInstance3D meshInstance = new MeshInstance3D();
                meshInstance.Mesh = nodes[0].mesh;
                meshInstance.Position = new Vector3(z * scale, y * scale, -x * scale);
                meshInstance.Name = $"({z};{y};{-x}){nodes[0].corners}";
                meshInstance.CreateTrimeshCollision();
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
