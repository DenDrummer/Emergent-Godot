using Godot;
using System;

public partial class Observer : Node3D
{
    [Export]
    float moveSpeed = 1;
    [Export]
    float xRotationSpeed = 1;
    [Export]
    float yRotationSpeed = 1;
    [Export]
    Node3D player;
    [Export]
    Node3D camera;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
    }

    public override void _Input(InputEvent @event)
    {
        InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
        if (mouseMotion != null)
        {
            // TODO something something mouseMotion.Relative
            camera.RotateX(-mouseMotion.Relative.Y * xRotationSpeed * (float)GetProcessDeltaTime());
            player.RotateY(mouseMotion.Relative.X * yRotationSpeed * (float)GetProcessDeltaTime());
        }
    }
}
