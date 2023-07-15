using Godot;
using System;

public partial class Observer : Node3D
{
    // lattitudal, longitudal & vertical
    short lat, lng, vrt, yaw;
    [Export]
    float moveSpeed = 1;
    [Export]
    Vector3 rotationSpeed = new Vector3(1, 1, 1);
    [Export]
    Node3D player;
    [Export]
    Node3D camera;
    [Export]
    RichTextLabel coordLabel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        UpdateLabel();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        #region --- movement ---
        #region --- longitudal movement ---
        if (Input.IsActionJustPressed("forward"))
        {
            lng--;
        }
        if (Input.IsActionJustReleased("forward"))
        {
            lng++;
        }

        if (Input.IsActionJustPressed("backward"))
        {
            lng++;
        }
        if (Input.IsActionJustReleased("backward"))
        {
            lng--;
        }
        #endregion

        #region --- latitudal movement ---
        if (Input.IsActionJustPressed("left"))
        {
            lat--;
        }
        if (Input.IsActionJustReleased("left"))
        {
            lat++;
        }

        if (Input.IsActionJustPressed("right"))
        {
            lat++;
        }
        if (Input.IsActionJustReleased("right"))
        {
            lat--;
        }
        #endregion

        #region --- latitudal movement ---
        if (Input.IsActionJustPressed("down"))
        {
            vrt--;
        }
        if (Input.IsActionJustReleased("down"))
        {
            vrt++;
        }

        if (Input.IsActionJustPressed("up"))
        {
            vrt++;
        }
        if (Input.IsActionJustReleased("up"))
        {
            vrt--;
        }
        #endregion

        Vector3 moveDirection = new Vector3(lat, vrt, lng);

        if (!moveDirection.IsZeroApprox())
        {
            player.Translate(moveDirection.Normalized() * moveSpeed * (float)GetProcessDeltaTime());
            UpdateLabel();
        }
        #endregion

        #region --- rotation ---
        if (Input.IsActionJustPressed("clockwise"))
        {
            yaw++;
        }
        if (Input.IsActionJustReleased("clockwise"))
        {
            yaw--;
        }

        if (Input.IsActionJustPressed("counterclockwise"))
        {
            yaw--;
        }
        if (Input.IsActionJustReleased("counterclockwise"))
        {
            yaw++;
        }

        if (yaw!=0)
        {
            // rotate camera around player's rotation
            camera.RotateZ(yaw * rotationSpeed.Z * (float)GetProcessDeltaTime());
            // move rotation from camera to player
            player.GlobalRotation = camera.GlobalRotation;
            camera.Rotation = new Vector3();
        }
        #endregion

        #region --- scene ---
        if (Input.IsActionJustPressed("escape"))
        {
            GetTree().Quit();
        }

        if (Input.IsActionJustPressed("restart"))
        {
            GetTree().ReloadCurrentScene();
        }
        #endregion
    }

    public override void _Input(InputEvent @event)
    {
        InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
        if (mouseMotion != null)
        {
            // rotate camera around player's rotation
            camera.RotateX(-mouseMotion.Relative.Y * rotationSpeed.X * (float)GetProcessDeltaTime());
            camera.RotateY(-mouseMotion.Relative.X * rotationSpeed.Y * (float)GetProcessDeltaTime());
            // move rotation from camera to player
            player.GlobalRotation = camera.GlobalRotation;
            camera.Rotation = new Vector3();
        }
    }

    public void UpdateLabel()
    {
        
        coordLabel.Text = $"x: {MathF.Round(Position.X, 2)}\ny: {MathF.Round(Position.Y, 2)}\nz: {MathF.Round(Position.Z, 2)}";
    }
}
